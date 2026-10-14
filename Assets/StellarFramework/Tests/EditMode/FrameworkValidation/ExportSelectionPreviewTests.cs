using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace StellarFramework.Tests.FrameworkValidation
{
    public sealed class ExportSelectionPreviewTests
    {
        private static Type Publisher => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("StellarFramework.Editor.Modules.StellarFrameworkPackagePublisher"))
            .First(type => type != null);

        [Test]
        public void ResourcesAndAssetBundleMergeWithoutOtherBackendsOrEditorTools()
        {
            object preview = Preview(new[] { "reskit.resources", "reskit.assetbundle", "reskit.resources" });
            Assert.That(ProfileIds(preview, "selectedProfiles"), Is.EquivalentTo(new[] { "reskit.resources", "reskit.assetbundle" }));
            Assert.That(ProfileIds(preview, "dependencies"), Is.Unique);
            Assert.That(ProfileIds(preview, "dependencies"), Does.Contain("reskit.core"));
            Assert.That(ProfileIds(preview, "dependencies"), Does.Not.Contain("toolshub.core"));
            Assert.That(Field<string[]>(preview, "requiredUpm"), Is.EquivalentTo(new[] { "com.cysharp.unitask" }));
        }

        [Test]
        public void ResourceAndCodeUpdatingCanBeCombinedWithAnUnrelatedKit()
        {
            var selected = new[] { "httpkit", "reskit.contentupdate.yooasset", "reskit.codeupdate.hybridclr" };
            object preview = Preview(selected);
            Assert.That(ProfileIds(preview, "selectedProfiles"), Is.EquivalentTo(selected));
            Assert.That(ProfileIds(preview, "dependencies"), Does.Not.Contain("reskit.addressables"));
            Assert.That(ProfileIds(preview, "dependencies"), Does.Not.Contain("reskit.yooasset"));
            Assert.That(Field<string[]>(preview, "requiredUpm"), Does.Contain("com.unity.ext.nunit"));
            Assert.That(Field<string[]>(preview, "requiredUpm"), Does.Not.Contain("com.unity.test-framework"));
        }

        [Test]
        public void DocumentationIsOptionalAndAnEmptySelectionHasNoAssets()
        {
            object docs = Preview(new[] { "httpkit" });
            object sourceOnly = Preview(new[] { "httpkit" }, false);
            Assert.That(Field<int>(sourceOnly, "assetCount"), Is.LessThan(Field<int>(docs, "assetCount")));
            Assert.That(Field<int>(Preview(Array.Empty<string>()), "assetCount"), Is.Zero);
        }

        [Test]
        public void EditorCompatibilityPrerequisitesAreInstalledBeforeResourceBackends()
        {
            object result = Publisher.GetMethod("CreatePackageDependencies", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new[] { "com.tuyoogame.yooasset", "com.unity.addressables", "com.unity.ext.nunit" } });
            string[] ids = ((IEnumerable)result).Cast<object>().Select(value => Field<string>(value, "packageId")).ToArray();
            Assert.That(ids[0], Is.EqualTo("com.unity.ext.nunit"));
            Assert.That(ids, Is.EquivalentTo(new[] { "com.tuyoogame.yooasset", "com.unity.addressables", "com.unity.ext.nunit" }));
        }

        [TestCase("Assets/StellarFramework/Tests/Fixtures/example.prefab")]
        [TestCase("Assets/GameHotUpdate/Code/UnityEngine.TestRunner.dll.bytes")]
        [TestCase("Assets/GameHotUpdate/Code/MCPForUnity.Runtime.dll.bytes")]
        [TestCase("Assets/HotUpdatePublisherConsumerE2E/Content/example.txt")]
        public void DevelopmentContentIsRejectedEvenWhenItLiesInsideASelectedSourceRoot(string path)
        {
            var method = Publisher.GetMethod("IsDevelopmentOnlyAsset", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method.Invoke(null, new object[] { path }), Is.True);
        }

        private static object Preview(string[] ids, bool docs = true) => Publisher
            .GetMethod("PreviewExportSelection", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { ids, docs });

        private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name).GetValue(value);
        private static string[] ProfileIds(object value, string name) => Field<IEnumerable>(value, name)
            .Cast<object>().Select(profile => Field<string>(profile, "id")).ToArray();
    }
}
