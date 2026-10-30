using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StellarFramework.Editor.ResourcePublisher
{
    /// <summary>单个待上传的不可变发布文件及其创建时的长度/SHA256 快照。</summary>
    public interface IResourceRemoteFile
    {
        string RelativePath { get; }
        long Length { get; }
    }

    public sealed class ResourcePublishFile : IResourceRemoteFile
    {
        private ResourcePublishFile(string relativePath, string sourcePath, long length, string sha256)
        {
            RelativePath = relativePath;
            SourcePath = sourcePath;
            Length = length;
            Sha256 = sha256;
        }

        public string RelativePath { get; }
        public string SourcePath { get; }
        public long Length { get; }
        public string Sha256 { get; }

        /// <summary>读取源文件并生成不可变校验快照。</summary>
        public static ResourcePublishFile FromFile(string relativePath, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path is required.", nameof(sourcePath));
            string absoluteSourcePath = Path.GetFullPath(sourcePath);
            var info = new FileInfo(absoluteSourcePath);
            if (!info.Exists) throw new FileNotFoundException("Publish source file was not found.", absoluteSourcePath);
            return new ResourcePublishFile(
                relativePath,
                absoluteSourcePath,
                info.Length,
                ComputeSha256(absoluteSourcePath));
        }

        internal static string ComputeSha256(string path)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return ToHex(sha256.ComputeHash(stream));
            }
        }

        internal static string ComputeSha256(byte[] content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            using (SHA256 sha256 = SHA256.Create())
                return ToHex(sha256.ComputeHash(content));
        }

        private static string ToHex(byte[] hash)
        {
            var builder = new StringBuilder(hash.Length * 2);
            for (int index = 0; index < hash.Length; index++) builder.Append(hash[index].ToString("x2"));
            return builder.ToString();
        }
    }

    /// <summary>目标端单个文件的实际长度和 SHA256。</summary>
    public sealed class ResourcePublishTargetFileInfo
    {
        public ResourcePublishTargetFileInfo(string relativePath, long length, string sha256)
        {
            RelativePath = relativePath ?? string.Empty;
            Length = length;
            Sha256 = sha256 ?? string.Empty;
        }

        public string RelativePath { get; }
        public long Length { get; }
        public string Sha256 { get; }
    }

    /// <summary>一次明确要求更新的可变版本指针。不可变 Bundle/Manifest 文件不使用此请求。</summary>
    public sealed class ResourceVersionPublishRequest
    {
        public string PointerRelativePath { get; set; } = "PackageVersion";
        public string PackageVersion { get; set; } = string.Empty;
        public string ExpectedCurrentPackageVersion { get; set; } = string.Empty;
    }

    /// <summary>上传、查询、校验和最后切换版本指针的发布目标抽象。</summary>
    public interface IResourcePublishTarget
    {
        Task<ResourcePublishTargetFileInfo> UploadAsync(ResourcePublishFile file, CancellationToken cancellationToken);
        Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken);
        Task<ResourcePublishTargetFileInfo> GetInfoAsync(string relativePath, CancellationToken cancellationToken);
        Task VerifyAsync(IReadOnlyList<ResourcePublishFile> files, CancellationToken cancellationToken);
        Task PublishVersionAsync(ResourceVersionPublishRequest request, CancellationToken cancellationToken);
        Task RollbackAsync(ResourceVersionPublishRequest request, CancellationToken cancellationToken);
    }
}
