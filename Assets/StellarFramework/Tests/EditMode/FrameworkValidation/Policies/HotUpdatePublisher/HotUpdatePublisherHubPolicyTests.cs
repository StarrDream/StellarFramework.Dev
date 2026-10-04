using System.IO;
using System.Reflection;
using NUnit.Framework;
using StellarFramework.Editor;
using StellarFramework.Editor.HotUpdatePublisher;
using StellarFramework.Editor.Modules;
using UnityEngine;

namespace StellarFramework.Tests.FrameworkValidation
{
    public sealed class HotUpdatePublisherHubPolicyTests
    {
        [Test]
        public void PublisherModuleIsRegisteredInHotUpdateToolsHubGroup()
        {
            var attribute = (StellarToolAttribute)typeof(HotUpdatePublisherHubModule)
                .GetCustomAttributes(typeof(StellarToolAttribute), false)[0];

            Assert.That(typeof(ToolModule).IsAssignableFrom(typeof(HotUpdatePublisherHubModule)), Is.True);
            Assert.That(attribute.Title, Is.EqualTo("热更发布器"));
            Assert.That(attribute.Group, Is.EqualTo("热更新"));
            Assert.That(attribute.RequiredAssemblyNames, Does.Contain("StellarFramework.ToolsHub.HotUpdatePublisher.Editor"));
        }

        [Test]
        public void PublisherHubExposesRequiredSectionsAndOnlyThreePrimaryActions()
        {
            string source = ReadModuleSource();

            Assert.That(source, Does.Contain("概览").And.Contain("变更检查").And.Contain("构建与发布")
                .And.Contain("发布目标").And.Contain("发布记录").And.Contain("高级设置"));
            Assert.That(source, Does.Contain("模拟发布（不上传）").And.Contain("构建并发布"));
            Assert.That(source, Does.Contain("本机或已挂载目录").And.Contain("OpenFolderPanel"));
            Assert.That(CountOccurrences(source, "GUILayout.Button(new GUIContent(\"模拟发布（不上传）\"") , Is.EqualTo(1));
            Assert.That(CountOccurrences(source, "GUILayout.Button(new GUIContent(\"仅构建与检查\"") , Is.EqualTo(1));
            Assert.That(CountOccurrences(source, "GUILayout.Button(new GUIContent(\"构建并发布\"") , Is.EqualTo(1));
        }

        [Test]
        public void PublisherHubShowsTruthfulReadinessAndAdvancedTools()
        {
            string source = ReadModuleSource();

            Assert.That(source, Does.Contain("检查改动是否适合热更").And.Contain("客户端基包版本")
                .And.Contain("YooAsset 资源包名").And.Contain("远端检查")
                .And.Contain("代码热更").And.Contain("AOT 元数据").And.Contain("主下载地址"));
            Assert.That(source, Does.Contain("本机构建").And.Contain("文件检查")
                .And.Contain("发布前验证").And.Contain("打开构建文件夹")
                .And.Contain("查看热更清单").And.Contain("查看客户端基包记录"));
            Assert.That(source, Does.Contain("HotUpdatePublisherBuildAdapters.Create(repository)")
                .And.Contain("workflow.DryRun.RunAsync").And.Contain("workflow.Pipeline.RunAsync")
                .And.Contain("RunRollbackAsync"));
            Assert.That(source, Does.Not.Contain("尚未接入"));
            Assert.That(source, Does.Contain("EditorPrefs"));
        }

        [Test]
        public void PublisherHubDisplaysGitProvenanceAndBlocksDirtyProduction()
        {
            string source = ReadModuleSource();

            Assert.That(source, Does.Contain("_gitSnapshot.Branch").And.Contain("_gitSnapshot.Commit")
                .And.Contain("_gitSnapshot.IsDirty"));
            Assert.That(source, Does.Contain("正式环境发布已阻止"));
            Assert.That(source, Does.Contain("开发或预发布仍可继续"));
        }

        [Test]
        public void OptionalSdkAdaptersRegisterWithThePublisherHubAtEditorLoad()
        {
            Assert.That(HotUpdatePublisherBuildAdapters.GetReadinessError(), Is.Empty);
            Assert.That(HotUpdatePublisherBuildAdapters.GetHybridCLRPackageVersion(), Is.Not.Empty);

            IHotUpdateBuildAdapter adapter = HotUpdatePublisherBuildAdapters.Create(
                new HotUpdateBaseReleaseRepository());
            Assert.That(adapter, Is.TypeOf<CompositeHotUpdateBuildAdapter>());
        }

        private static string ReadModuleSource()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(projectRoot, "Assets/StellarFramework/Editor/StellarToolsHub/Modules/HotUpdatePublisher/HotUpdatePublisherHubModule.cs");
            return File.ReadAllText(path);
        }

        private static int CountOccurrences(string value, string fragment)
        {
            int count = 0;
            int offset = 0;
            while ((offset = value.IndexOf(fragment, offset, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += fragment.Length;
            }
            return count;
        }
    }
}
