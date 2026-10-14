// Copy to Assets/Editor only after installing reskit.yooasset.tools in an isolated consumer project.
using System;
using System.IO;
using System.Linq;
using StellarFramework.Editor.ResourceBuild;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

public static class ConsumerResourceBuildGate
{
    public static void Run()
    {
        var report = new Report();
        try
        {
            if (File.ReadAllText("Packages/manifest.json").Contains("hybridclr")) throw new Exception("Unexpected HybridCLR dependency.");
            if (YooAssetResourceBuilder.FindCollectorSetting() != null) throw new Exception("This gate requires an empty consumer project.");
            Directory.CreateDirectory("Assets/ResourceBuildConsumer");
            Directory.CreateDirectory("Assets/Config");
            const string expected = "StellarFramework standalone YooAsset resource build";
            File.WriteAllText("Assets/ResourceBuildConsumer/Message.txt", expected);
            AssetDatabase.Refresh();
            var setting = ScriptableObject.CreateInstance<AssetBundleCollectorSetting>();
            var package = new AssetBundleCollectorPackage { PackageName = "ConsumerResources", EnableAddressable = true };
            var group = new AssetBundleCollectorGroup { GroupName = "Content" };
            group.Collectors.Add(new AssetBundleCollector { CollectPath = "Assets/ResourceBuildConsumer" });
            package.Groups.Add(group);
            setting.Packages.Add(package);
            const string settingPath = "Assets/Config/AssetBundleCollectorSetting.asset";
            AssetDatabase.CreateAsset(setting, settingPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            byte[] originalSetting = File.ReadAllBytes(settingPath);
            var request = new YooAssetResourceBuildRequest
            {
                Target = BuildTarget.StandaloneWindows64, PackageName = package.PackageName,
                Version = "1.0.0", OutputRoot = "BuildArtifacts/ConsumerResources"
            };
            report.first = YooAssetResourceBuilder.Build(request);
            if (!report.first.Success) throw new Exception(report.first.Error);
            string bundlePath = Directory.GetFiles(report.first.OutputDirectory, "*.bundle").First();
            byte[] originalBundle = File.ReadAllBytes(bundlePath);
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null) throw new Exception("Produced Bundle could not be loaded.");
            try
            {
                report.resourceLoaded = bundle.LoadAllAssets<TextAsset>().Any(asset => asset.text == expected);
                if (!report.resourceLoaded) throw new Exception("Produced Bundle did not contain the configured TextAsset.");
            }
            finally { bundle.Unload(true); }
            var duplicate = YooAssetResourceBuilder.Build(request);
            report.existingVersionRejected = !duplicate.Success && originalBundle.SequenceEqual(File.ReadAllBytes(bundlePath));
            if (!report.existingVersionRejected) throw new Exception("Existing version output was overwritten.");
            request.Version = "1.0.1";
            report.next = YooAssetResourceBuilder.Build(request);
            if (!report.next.Success) throw new Exception(report.next.Error);
            report.collectorUnchanged = originalSetting.SequenceEqual(File.ReadAllBytes(settingPath));
            report.streamingAssetsUnchanged = !Directory.Exists("Assets/StreamingAssets");
            if (!report.collectorUnchanged || !report.streamingAssetsUnchanged) throw new Exception("Build changed collector settings or built-in content.");
            report.status = "PASS";
        }
        catch (Exception exception) { report.status = "FAIL"; report.error = exception.ToString(); }
        string[] args = Environment.GetCommandLineArgs();
        string output = args.SkipWhile(value => value != "-consumerReport").Skip(1).First();
        File.WriteAllText(output, JsonUtility.ToJson(report, true));
        Debug.Log("[ConsumerResourceBuildGate] " + report.status + " " + report.error);
        EditorApplication.Exit(report.status == "PASS" ? 0 : 1);
    }

    [Serializable] private sealed class Report
    {
        public string status;
        public string error;
        public bool resourceLoaded;
        public bool existingVersionRejected;
        public bool collectorUnchanged;
        public bool streamingAssetsUnchanged;
        public YooAssetResourceBuildResult first;
        public YooAssetResourceBuildResult next;
    }
}
