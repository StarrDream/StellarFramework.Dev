using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StellarFramework.Editor.ResourceBuild;
using UnityEditor;
using UnityEngine.UIElements;
using YooAsset;

namespace StellarFramework.Tests
{
    public sealed class YooAssetResourceBuildTests
    {
        [TestCase("../escape")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        [TestCase("..")]
        [TestCase("CON")]
        [TestCase("LPT1.txt")]
        [TestCase("1.0.0.")]
        [TestCase("OutputCache")]
        public void OutputDirectory_RejectsInvalidVersion(string version)
        {
            Assert.Throws<ArgumentException>(() => YooAssetResourceBuilder.GetOutputDirectory(
                new YooAssetResourceBuildRequest { PackageName = "GameContent", Version = version }));
        }

        [Test]
        public void OutputDirectory_IncludesPlatformPackageAndVersion()
        {
            string output = YooAssetResourceBuilder.GetOutputDirectory(new YooAssetResourceBuildRequest
            { PackageName = "GameContent", Version = "1.0.1", Target = BuildTarget.StandaloneWindows64, OutputRoot = "Temp/ResourceBuild" });
            Assert.That(output, Does.EndWith(Path.Combine("StandaloneWindows64", "GameContent", "1.0.1")));
            Assert.That(Path.IsPathRooted(output), Is.True);
        }

        [Test]
        public void BuildAssembly_HasNoHybridCLRPublisherOrResKitRuntimeDependency()
        {
            string[] references = typeof(YooAssetResourceBuilder).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();
            Assert.That(references, Does.Not.Contain("StellarFramework.ToolsHub.HotUpdatePublisher.Editor"));
            Assert.That(references.Any(name => name.Contains("HybridCLR")), Is.False);
            Assert.That(references, Does.Not.Contain("StellarFramework.ResKit"));
        }

        [Test]
        public void OutputInspection_RequiresAllManifestsAndBundles()
        {
            string directory = Path.Combine(Path.GetTempPath(), "StellarFrameworkResourceBuildTests", Guid.NewGuid().ToString("N"));
            var request = new YooAssetResourceBuildRequest { PackageName = "GameContent", Version = "1.0.0" };
            Directory.CreateDirectory(directory);
            try
            {
                Assert.That(YooAssetResourceBuilder.InspectOutput(request, directory).Success, Is.False);
                string[] names =
                {
                    YooAssetSettingsData.GetManifestBinaryFileName(request.PackageName, request.Version),
                    YooAssetSettingsData.GetManifestJsonFileName(request.PackageName, request.Version),
                    YooAssetSettingsData.GetPackageHashFileName(request.PackageName, request.Version),
                    YooAssetSettingsData.GetPackageVersionFileName(request.PackageName)
                };
                foreach (string name in names) File.WriteAllText(Path.Combine(directory, name), "test");
                Assert.That(YooAssetResourceBuilder.InspectOutput(request, directory).Success, Is.False);
                File.WriteAllText(Path.Combine(directory, "content.bundle"), "test");
                YooAssetResourceBuildResult result = YooAssetResourceBuilder.InspectOutput(request, directory);
                Assert.That(result.Success, Is.True);
                Assert.That(result.BundleCount, Is.EqualTo(1));
                Assert.That(result.TotalBytes, Is.GreaterThan(0));
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void View_ExposesChineseBuildControlsWithoutCodeUpdateSetup()
        {
            var module = new YooAssetBuildHubModule();
            VisualElement view = module.CreateView();
            Assert.That(view.Q<DropdownField>("resource-package"), Is.Not.Null);
            Assert.That(view.Q<DropdownField>("resource-platform"), Is.Not.Null);
            Assert.That(view.Q<TextField>("resource-version"), Is.Not.Null);
            Assert.That(view.Q<Button>("resource-build-button").text, Is.EqualTo("开始构建资源"));
            Assert.That(view.Q<TextField>("resource-result-path").isReadOnly, Is.True);
        }
    }
}
