using System;
using System.IO;
using System.Text.RegularExpressions;
using StellarFramework.HybridCLR;
using UnityEditor;
using YooAsset.Editor;

namespace StellarFramework.Editor.HotUpdatePublisher
{
    /// <summary>Creates a separate business package while preserving all existing YooAsset packages.</summary>
    internal static class YooAssetPublisherFirstUseSetup
    {
        private const string PackageNamePrefsSuffix = ".packageName";
        private const string AssetOutputRootPrefsSuffix = ".assetOutputRoot";
        private const string PackageGroupName = "HotUpdateRuntimePayload";
        private const string GeneratedRoot = "Assets/HotUpdatePublisherConsumerE2E/Generated";
        private const string ConsumerBehaviorPath = "Assets/HotUpdatePublisherConsumerE2E/Content/HotUpdateBehavior.txt";

        private static readonly Regex SafePackageName = new Regex(
            @"\A[A-Za-z0-9][A-Za-z0-9_-]{0,63}\z",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [MenuItem("Tools/StellarFramework/HotUpdate Publisher/Configure Recommended YooAsset Collector")]
        private static void ConfigureRecommendedCollector()
        {
            string packageName = ReadSelectedPackageName();
            if (!IsSafeBusinessPackageName(packageName))
            {
                UnityEngine.Debug.LogError(
                    "[HotUpdatePublisher] Enter a valid business Package name first. Verification-only package names are rejected.");
                return;
            }

            string generatedRoot = ReadHotUpdateAssetOutputRoot();
            if (!IsSafeAssetRoot(generatedRoot))
            {
                UnityEngine.Debug.LogError(
                    $"[HotUpdatePublisher] HotUpdate asset output root '{generatedRoot}' must be a safe folder inside Assets/. No Collector changes were made.");
                return;
            }

            string[] metadataPaths;
            try
            {
                HotUpdateSettings settings = HotUpdateSettings.LoadOrCreateDefault();
                metadataPaths = HotUpdateAotMetadataSelection.GetGeneratedAssetPaths(
                    generatedRoot, settings.AotMetadataKeys);
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is ArgumentException)
            {
                UnityEngine.Debug.LogError(
                    "[HotUpdatePublisher] HotUpdateSettings AOT metadata selection is invalid: " + exception.Message);
                return;
            }

            string consumerBehaviorAbsolutePath = ToAbsoluteAssetPath(ConsumerBehaviorPath);
            Directory.CreateDirectory(Path.GetDirectoryName(consumerBehaviorAbsolutePath));
            if (!File.Exists(consumerBehaviorAbsolutePath))
            {
                File.WriteAllText(consumerBehaviorAbsolutePath,
                    "publisher-consumer-behavior=v1\n");
                AssetDatabase.ImportAsset(ConsumerBehaviorPath, ImportAssetOptions.ForceUpdate);
            }

            AssetBundleCollectorSetting setting = AssetBundleCollectorSettingData.Setting;
            AssetBundleCollectorPackage package = null;
            for (int index = 0; index < setting.Packages.Count; index++)
            {
                if (string.Equals(setting.Packages[index].PackageName, packageName, StringComparison.Ordinal))
                {
                    package = setting.Packages[index];
                    break;
                }
            }

            if (package != null)
            {
                if (!package.EnableAddressable)
                {
                    // The runtime consumer loads resources by address. YooAsset emits empty
                    // addresses when this package option is disabled, even when collectors
                    // use AddressByFileName.
                    package.EnableAddressable = true;
                    AssetBundleCollectorSettingData.SaveFile();
                    UnityEngine.Debug.Log(
                        $"[HotUpdatePublisher] Enabled Addressable on existing business package '{packageName}' so AddressByFileName collectors produce runtime-loadable asset addresses.");
                }

                UnityEngine.Debug.Log(
                    $"[HotUpdatePublisher] Business package '{packageName}' already exists. Existing groups and collectors were preserved; Addressable was enabled if needed. Review the package in the Collector window.");
                EditorApplication.ExecuteMenuItem("YooAsset/AssetBundle Collector");
                return;
            }

            package = new AssetBundleCollectorPackage
            {
                PackageName = packageName,
                PackageDesc = "HotUpdate Publisher business package. Not the Verification package.",
                EnableAddressable = true,
                SupportExtensionless = true,
                LocationToLower = false,
                IncludeAssetGUID = false,
                AutoCollectShaders = false,
                IgnoreRuleName = nameof(NormalIgnoreRule)
            };

            var group = new AssetBundleCollectorGroup
            {
                GroupName = PackageGroupName,
                GroupDesc = "Publisher-generated HybridCLR payload and consumer content",
                ActiveRuleName = nameof(EnableGroup)
            };
            package.Groups.Add(group);

            AddCollector(group, ConsumerBehaviorPath);
            AddCollector(group, generatedRoot + "/Manifest/HotUpdateManifest.json");
            AddCollector(group, generatedRoot + "/Code/HotUpdate.dll.bytes");
            foreach (string metadataPath in metadataPaths)
                AddCollector(group, metadataPath);

            setting.Packages.Add(package);
            AssetBundleCollectorSettingData.SaveFile();

            UnityEngine.Debug.Log(
                $"[HotUpdatePublisher] Created independent business package '{packageName}' with one consumer content asset and six Publisher output paths. Existing Verification configuration was preserved and is not used by this package.");
            EditorApplication.ExecuteMenuItem("YooAsset/AssetBundle Collector");
        }

        private static void AddCollector(AssetBundleCollectorGroup group, string assetPath)
        {
            group.Collectors.Add(new AssetBundleCollector
            {
                CollectPath = assetPath,
                CollectorType = ECollectorType.MainAssetCollector,
                AddressRuleName = nameof(AddressByFileName),
                PackRuleName = nameof(PackSeparately),
                FilterRuleName = nameof(CollectAll)
            });
        }

        private static string ReadSelectedPackageName()
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string suffix = projectRoot.Replace('\\', '/');
            string packageName = EditorPrefs.GetString(
                "StellarFramework.HotUpdatePublisher." + suffix + PackageNamePrefsSuffix,
                "HotUpdatePublisherConsumerE2E").Trim();
            return string.IsNullOrWhiteSpace(packageName)
                ? "HotUpdatePublisherConsumerE2E"
                : packageName;
        }

        private static string ReadHotUpdateAssetOutputRoot()
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string suffix = projectRoot.Replace('\\', '/');
            string root = UnityEditor.EditorPrefs.GetString(
                "StellarFramework.HotUpdatePublisher." + suffix + AssetOutputRootPrefsSuffix,
                GeneratedRoot);
            return (root ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        }

        private static bool IsSafeAssetRoot(string assetRoot)
        {
            if (string.IsNullOrWhiteSpace(assetRoot) ||
                !assetRoot.StartsWith("Assets/", StringComparison.Ordinal) ||
                assetRoot.Contains(":") || assetRoot.Contains("%"))
                return false;

            string[] segments = assetRoot.Split('/');
            for (int index = 0; index < segments.Length; index++)
                if (string.IsNullOrWhiteSpace(segments[index]) || segments[index] == "." || segments[index] == "..")
                    return false;

            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string assetsRoot = Path.GetFullPath(UnityEngine.Application.dataPath)
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(Path.Combine(projectRoot,
                assetRoot.Replace('/', Path.DirectorySeparatorChar)))
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSafeBusinessPackageName(string packageName)
        {
            return !string.IsNullOrWhiteSpace(packageName) &&
                   SafePackageName.IsMatch(packageName) &&
                   packageName.IndexOf("verification", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
