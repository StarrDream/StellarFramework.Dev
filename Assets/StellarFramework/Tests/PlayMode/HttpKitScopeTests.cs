using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace StellarFramework.Tests.PlayMode
{
    [UnityPlatform(RuntimePlatform.WindowsEditor)]
    public sealed class HttpKitScopeTests
    {
        private HttpListener _server;
        private string _url;

        [SetUp]
        public void StartServer()
        {
            var reservation = new TcpListener(IPAddress.Loopback, 0);
            reservation.Start();
            int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
            reservation.Stop();
            _url = $"http://127.0.0.1:{port}/scope/";
            _server = new HttpListener();
            _server.Prefixes.Add(_url);
            _server.Start();
            HttpKit.SetRequestScope(string.Empty);
            HttpKit.ClearAuthToken();
        }

        [TearDown]
        public void StopServer()
        {
            _server?.Close();
            HttpImageDownload.ClearCache();
            HttpKit.Instance.CancelAllRequests();
            UnityEngine.Object.DestroyImmediate(HttpKit.Instance.gameObject);
        }

        [UnityTest]
        public IEnumerator ChangedTokenDoesNotBlockAnotherAccountAndSameTokenIsDeduplicated()
        {
            Task<HttpListenerContext> accept = _server.GetContextAsync();
            var config = new RequestConfig { preventDuplicate = true };
            HttpKit.SetAuthToken("account-A");
            Task<HttpResponse> a = HttpKit.SendAsync("GET", _url, config: config).AsTask();
            HttpKit.SetAuthToken("account-B");
            Task<HttpResponse> b = HttpKit.SendAsync("GET", _url, config: config).AsTask();
            Task<HttpResponse> duplicate = HttpKit.SendAsync("GET", _url, config: config).AsTask();
            yield return WaitFor(() => duplicate.IsCompleted);
            Assert.That(duplicate.Result.error, Does.Contain("Duplicate"));
            yield return WaitFor(() => accept.IsCompleted);
            HttpListenerContext first = accept.Result;
            accept = _server.GetContextAsync();
            yield return WaitFor(() => accept.IsCompleted);
            HttpListenerContext second = accept.Result;
            Assert.That(new[] { first.Request.Headers["Authorization"], second.Request.Headers["Authorization"] },
                Is.EquivalentTo(new[] { "Bearer account-A", "Bearer account-B" }));
            Reply(first, first.Request.Headers["Authorization"]);
            Reply(second, second.Request.Headers["Authorization"]);
            yield return WaitFor(() => a.IsCompleted && b.IsCompleted);
            Assert.That(a.Result.isSuccess, Is.True, a.Result.error);
            Assert.That(b.Result.isSuccess, Is.True, b.Result.error);
            Assert.That(a.Result.responseText, Is.EqualTo("Bearer account-A"));
            Assert.That(b.Result.responseText, Is.EqualTo("Bearer account-B"));
        }

        [UnityTest]
        public IEnumerator CookieAccountsUseExplicitScopesAndSwitchCancelsThePreviousSession()
        {
            HttpKit.SetRequestScope("account:A");
            var config = new RequestConfig { autoInjectToken = false, preventDuplicate = true };
            Task<HttpListenerContext> accept = _server.GetContextAsync();
            Task<HttpResponse> a = HttpKit.SendAsync("GET", _url, config: config).AsTask();
            yield return WaitFor(() => accept.IsCompleted);
            HttpListenerContext old = accept.Result;
            HttpKit.SetRequestScope("account:B");
            accept = _server.GetContextAsync();
            Task<HttpResponse> b = HttpKit.SendAsync("GET", _url, config: config).AsTask();
            yield return WaitFor(() => a.IsCompleted && accept.IsCompleted);
            Assert.That(a.Result.isSuccess, Is.False);
            Assert.That(a.Result.error, Is.EqualTo("Request Cancelled"));
            Reply(accept.Result, "account-B");
            old.Response.Close();
            yield return WaitFor(() => b.IsCompleted);
            Assert.That(b.Result.isSuccess, Is.True, b.Result.error);
            Assert.That(b.Result.responseText, Is.EqualTo("account-B"));
        }

        [UnityTest]
        public IEnumerator AtomicAuthContextSwitchCancelsTheOldAccountAndSendsOnlyTheNewToken()
        {
            var config = new RequestConfig { preventDuplicate = true };
            HttpKit.SetAuthContext("account:A", "token-A");
            Task<HttpListenerContext> accept = _server.GetContextAsync();
            Task<HttpResponse> a = HttpKit.SendAsync("GET", _url, config: config).AsTask();
            yield return WaitFor(() => accept.IsCompleted);
            HttpListenerContext old = accept.Result;
            Assert.That(old.Request.Headers["Authorization"], Is.EqualTo("Bearer token-A"));
            HttpKit.SetAuthContext("account:B", "token-B");
            accept = _server.GetContextAsync();
            Task<HttpResponse> b = HttpKit.SendAsync("GET", _url, config: config).AsTask();
            yield return WaitFor(() => a.IsCompleted && accept.IsCompleted);
            Assert.That(a.Result.error, Is.EqualTo("Request Cancelled"));
            Assert.That(accept.Result.Request.Headers["Authorization"], Is.EqualTo("Bearer token-B"));
            Reply(accept.Result, "account-B");
            old.Response.Close();
            yield return WaitFor(() => b.IsCompleted);
            Assert.That(b.Result.isSuccess, Is.True, b.Result.error);
        }

        [UnityTest]
        public IEnumerator HeaderCaseOrderAndCallerMutationDoNotChangeTheRequestIdentity()
        {
            HttpKit.SetAuthToken("global-token");
            var config = new RequestConfig
            {
                preventDuplicate = true,
                requestScope = "account:A",
                headers = new Dictionary<string, string>
                {
                    ["authorization"] = "Bearer explicit-token",
                    ["Accept-Language"] = "zh-CN"
                }
            };
            Task<HttpListenerContext> accept = _server.GetContextAsync();
            Task<HttpResponse> a = HttpKit.SendAsync("get", _url, config: config).AsTask();
            var equivalent = new RequestConfig
            {
                preventDuplicate = true,
                requestScope = "account:A",
                headers = new Dictionary<string, string>
                {
                    ["accept-language"] = "zh-CN",
                    ["Authorization"] = "Bearer explicit-token"
                }
            };
            Task<HttpResponse> duplicate = HttpKit.SendAsync("GET", _url, config: equivalent).AsTask();
            yield return WaitFor(() => duplicate.IsCompleted);
            Assert.That(duplicate.Result.error, Does.Contain("Duplicate"));
            config.headers["authorization"] = "Bearer changed-token";
            yield return WaitFor(() => accept.IsCompleted);
            HttpListenerContext context = accept.Result;
            Assert.That(context.Request.Headers["Authorization"], Is.EqualTo("Bearer explicit-token"));
            Assert.That(context.Request.Headers["Accept-Language"], Is.EqualTo("zh-CN"));
            HttpKit.Instance.CancelRequest(_url, config: equivalent);
            yield return WaitFor(() => a.IsCompleted);
            Assert.That(a.Result.error, Is.EqualTo("Request Cancelled"));
            context.Response.Close();
        }

        [UnityTest]
        public IEnumerator ImageDownloadsAndCacheAreIsolatedByAccountScope()
        {
            var accountA = new RequestConfig { autoInjectToken = false, requestScope = "account:A" };
            var accountB = new RequestConfig { autoInjectToken = false, requestScope = "account:B" };
            Task<HttpListenerContext> accept = _server.GetContextAsync();
            Task<Texture2D> a = HttpImageDownload.DownloadTextureAsync(_url, default, accountA).AsTask();
            yield return WaitFor(() => accept.IsCompleted);
            HttpListenerContext first = accept.Result;
            accept = _server.GetContextAsync();
            Task<Texture2D> b = HttpImageDownload.DownloadTextureAsync(_url, default, accountB).AsTask();
            yield return WaitFor(() => accept.IsCompleted);
            ReplyImage(first, Color.red);
            ReplyImage(accept.Result, Color.green);
            yield return WaitFor(() => a.IsCompleted && b.IsCompleted);
            Assert.That(a.Result, Is.Not.Null);
            Assert.That(b.Result, Is.Not.Null);
            Assert.That(a.Result, Is.Not.SameAs(b.Result));
            Assert.That(a.Result.GetPixel(0, 0).r, Is.GreaterThan(0.9f));
            Assert.That(b.Result.GetPixel(0, 0).g, Is.GreaterThan(0.9f));
            Task<Texture2D> cachedA = HttpImageDownload.DownloadTextureAsync(_url, default, accountA).AsTask();
            Task<Texture2D> cachedB = HttpImageDownload.DownloadTextureAsync(_url, default, accountB).AsTask();
            yield return WaitFor(() => cachedA.IsCompleted && cachedB.IsCompleted);
            Assert.That(cachedA.Result, Is.SameAs(a.Result));
            Assert.That(cachedB.Result, Is.SameAs(b.Result));
            HttpImageDownload.ClearCache(_url, accountA);
            Task<Texture2D> stillB = HttpImageDownload.DownloadTextureAsync(_url, default, accountB).AsTask();
            yield return WaitFor(() => stillB.IsCompleted);
            Assert.That(stillB.Result, Is.SameAs(b.Result));
        }

        [UnityTest]
        public IEnumerator ChangingAccountCancelsAnOldSharedImageDownload()
        {
            HttpKit.SetRequestScope("account:A");
            Task<HttpListenerContext> accept = _server.GetContextAsync();
            Task<Texture2D> a = HttpImageDownload.DownloadTextureAsync(_url).AsTask();
            yield return WaitFor(() => accept.IsCompleted);
            HttpListenerContext old = accept.Result;
            HttpKit.SetRequestScope("account:B");
            yield return WaitFor(() => a.IsCompleted);
            Assert.That(a.Result, Is.Null);
            accept = _server.GetContextAsync();
            Task<Texture2D> b = HttpImageDownload.DownloadTextureAsync(_url).AsTask();
            yield return WaitFor(() => accept.IsCompleted);
            ReplyImage(accept.Result, Color.green);
            old.Response.Close();
            yield return WaitFor(() => b.IsCompleted);
            Assert.That(b.Result, Is.Not.Null);
            Assert.That(b.Result.GetPixel(0, 0).g, Is.GreaterThan(0.9f));
        }

        private static void ReplyImage(HttpListenerContext context, Color color)
        {
            var image = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            image.SetPixel(0, 0, color);
            image.Apply();
            byte[] bytes = image.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(image);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "image/png";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        private static IEnumerator WaitFor(Func<bool> complete)
        {
            var timeout = Stopwatch.StartNew();
            while (!complete() && timeout.Elapsed.TotalSeconds < 10) yield return null;
            Assert.That(complete(), Is.True, "Local HTTP test did not finish within 10 seconds.");
        }

        private static void Reply(HttpListenerContext context, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
