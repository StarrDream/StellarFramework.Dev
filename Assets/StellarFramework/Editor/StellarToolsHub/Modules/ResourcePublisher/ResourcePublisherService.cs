// 【2026-10-30｜RESOURCE-PUBLISHER-01】YooAsset 资源发布服务，不依赖 HybridCLR。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StellarFramework.Editor.ResourcePublisher
{
    public sealed class ResourcePublishPlan
    {
        public readonly List<ResourcePublishFile> Files = new List<ResourcePublishFile>();
        public readonly List<ResourcePublishFile> UploadFiles = new List<ResourcePublishFile>();
        public readonly List<ResourcePublishFile> ReusedFiles = new List<ResourcePublishFile>();
        public long UploadBytes => UploadFiles.Sum(item => item.Length);
    }

    public static class ResourcePublisherService
    {
        // .version 是可变指针，仅在不可变文件全部验证后才能切换。
        public static List<ResourcePublishFile> Collect(string directory, string packageName, string packageVersion)
        {
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
            if (!SafeSegment(packageName) || !SafeSegment(packageVersion))
                throw new ArgumentException("资源包名或版本不合法。");
            foreach (string required in new[] {
                packageName + ".version",
                packageName + "_" + packageVersion + ".bytes",
                packageName + "_" + packageVersion + ".hash" })
                if (!File.Exists(Path.Combine(directory, required)))
                    throw new FileNotFoundException("YooAsset 正式产物缺少文件：" + required);
            string[] paths = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".ldtmp", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(Path.GetFileName(path), "OutputCache", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(Path.GetFileName(path), "OutputCache.manifest", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(Path.GetFileName(path), packageName + ".version", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (!paths.Any(path => path.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("资源包没有 .bundle 产物。");
            var files = paths.Select(path => ResourcePublishFile.FromFile(Path.GetFileName(path), path)).ToList();
            if (files.Select(f => f.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count)
                throw new InvalidDataException("存在大小写冲突的发布文件名。");
            return files;
        }

        public static async Task<ResourcePublishPlan> PreviewAsync(IReadOnlyList<ResourcePublishFile> files,
            IResourcePublishTarget target, CancellationToken cancellationToken)
        {
            if (files == null || files.Count == 0) throw new ArgumentException("没有待发布文件。", nameof(files));
            if (target == null) throw new ArgumentNullException(nameof(target));
            var plan = new ResourcePublishPlan();
            foreach (ResourcePublishFile file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                plan.Files.Add(file);
                ResourcePublishTargetFileInfo remote = await target.GetInfoAsync(file.RelativePath, cancellationToken);
                if (remote == null) plan.UploadFiles.Add(file);
                else if (remote.Length == file.Length &&
                         string.Equals(remote.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
                    plan.ReusedFiles.Add(file);
                else throw new IOException("远端已存在不同内容的不可变文件，禁止覆盖：" + file.RelativePath);
            }
            return plan;
        }

        public static async Task PublishAsync(ResourcePublishPlan plan, IResourcePublishTarget target,
            string packageName, string nextVersion, string expectedCurrentVersion,
            CancellationToken cancellationToken)
        {
            if (plan == null || plan.Files.Count == 0) throw new ArgumentException("发布计划为空。", nameof(plan));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!SafeSegment(packageName) || !SafeSegment(nextVersion)) throw new ArgumentException("非法发布包版本。");
            // 上传操作本身有不可变文件冲突检查，防止预览后目标被其他发布者修改。
            foreach (ResourcePublishFile file in plan.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await target.UploadAsync(file, cancellationToken);
            }
            await target.VerifyAsync(plan.Files, cancellationToken);
            // CAS 指针；失败时不会错误地宣称发布成功。
            await target.PublishVersionAsync(new ResourceVersionPublishRequest {
                PointerRelativePath = packageName + ".version",
                PackageVersion = nextVersion,
                ExpectedCurrentPackageVersion = expectedCurrentVersion ?? string.Empty
            }, cancellationToken);
        }

        public static bool SafeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value == "." || value == ".." ||
                value.Length > 128 || value.Contains("/") || value.Contains("\\") ||
                value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Contains(":")) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_') return false;
            }
            return true;
        }

        public static string CombineRemoteRoot(string root, string platform, string package)
        {
            if (!SafeSegment(root) || !SafeSegment(platform) || !SafeSegment(package))
                throw new ArgumentException("发布路径必须是安全的单层目录名称。");
            return root + "/" + platform + "/" + package;
        }

        public static string PackageHost(string baseUrl, string relativeRoot)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new ArgumentException("CDN 基础 URL 必须为 HTTP(S) 目录地址。");
            string[] segments = relativeRoot.Split('/');
            if (segments.Any(segment => !SafeSegment(segment))) throw new ArgumentException("相对路径不合法。");
            return new Uri(uri, string.Join("/", segments) + "/").AbsoluteUri;
        }
    }
}
