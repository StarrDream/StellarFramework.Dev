// 【2026-10-30｜RESOURCE-PUBLISHER-01】CDN 下载端逐文件校验；快速模式不冒充全量校验。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StellarFramework.Editor.ResourcePublisher
{
    public sealed class ResourceRemoteVerificationResult
    {
        public int CheckedFiles;
        public long CheckedBytes;
        public bool FullSha256;
    }

    public static class ResourceRemoteVerifier
    {
        public static async Task<ResourceRemoteVerificationResult> VerifyAsync(
            string packageHost, string packageName, string expectedVersion,
            IReadOnlyList<ResourcePublishFile> files, bool strict, CancellationToken token)
        {
            if (!Uri.TryCreate(packageHost, UriKind.Absolute, out Uri root) ||
                (root.Scheme != Uri.UriSchemeHttps && root.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException("CDN 地址必须为 HTTP(S) URL。", nameof(packageHost));
            if (!ResourcePublisherService.SafeSegment(packageName) ||
                !ResourcePublisherService.SafeSegment(expectedVersion))
                throw new ArgumentException("资源包版本非法。");
            if (files == null || files.Count == 0) throw new ArgumentException("没有待检查的资源。", nameof(files));
            string version = await ReadSmallTextAsync(new Uri(root, packageName + ".version"), token);
            if (!string.Equals(version.Trim(), expectedVersion, StringComparison.Ordinal))
                throw new IOException("CDN 版本不一致：期望 " + expectedVersion + "，实际 " + version);
            List<ResourcePublishFile> chosen;
            if (strict) chosen = files.ToList();
            else
            {
                chosen = files.Where(file => file.RelativePath.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase) ||
                    file.RelativePath.EndsWith(".hash", StringComparison.OrdinalIgnoreCase)).ToList();
                ResourcePublishFile bundle = files.FirstOrDefault(file => file.RelativePath.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase));
                if (bundle != null) chosen.Add(bundle);
            }
            var result = new ResourceRemoteVerificationResult { FullSha256 = strict };
            foreach (ResourcePublishFile file in chosen)
            {
                token.ThrowIfCancellationRequested();
                await VerifyFileAsync(new Uri(root, Uri.EscapeDataString(file.RelativePath)), file, token);
                result.CheckedFiles++;
                result.CheckedBytes += file.Length;
            }
            return result;
        }

        private static async Task<string> ReadSmallTextAsync(Uri url, CancellationToken token)
        {
            HttpWebRequest request = CreateRequest(url);
            try
            {
                using (token.Register(request.Abort))
                using (WebResponse response = await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (var memory = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, token)) != 0)
                    {
                        if (memory.Length + read > 4096) throw new IOException("远端版本文件过大：" + url);
                        memory.Write(buffer, 0, read);
                    }
                    return Encoding.UTF8.GetString(memory.ToArray()).Trim('\uFEFF', ' ', '\r', '\n');
                }
            }
            catch (WebException exception) { throw new IOException("访问远端版本失败：" + url + "；" + exception.Message, exception); }
        }

        private static async Task VerifyFileAsync(Uri url, ResourcePublishFile file, CancellationToken token)
        {
            HttpWebRequest request = CreateRequest(url);
            try
            {
                using (token.Register(request.Abort))
                using (WebResponse response = await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (SHA256 hash = SHA256.Create())
                {
                    if (response.ContentLength >= 0 && response.ContentLength != file.Length)
                        throw new IOException("远端文件长度不匹配：" + file.RelativePath);
                    byte[] buffer = new byte[81920];
                    long bytes = 0;
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, token)) != 0)
                    {
                        bytes += read;
                        if (bytes > file.Length) throw new IOException("远端文件长度超出预期：" + file.RelativePath);
                        hash.TransformBlock(buffer, 0, read, buffer, 0);
                    }
                    hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    string actual = BitConverter.ToString(hash.Hash).Replace("-", "").ToLowerInvariant();
                    if (bytes != file.Length || !string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("远端文件 SHA-256 或长度不匹配：" + file.RelativePath);
                }
            }
            catch (WebException exception) { throw new IOException("CDN 文件不可访问：" + url + "；" + exception.Message, exception); }
        }

        private static HttpWebRequest CreateRequest(Uri url)
        {
            if ((url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Query) ||
                !string.IsNullOrEmpty(url.Fragment)) throw new ArgumentException("不安全的 CDN 地址：" + url);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;
            request.AllowAutoRedirect = false;
            return request;
        }
    }
}
