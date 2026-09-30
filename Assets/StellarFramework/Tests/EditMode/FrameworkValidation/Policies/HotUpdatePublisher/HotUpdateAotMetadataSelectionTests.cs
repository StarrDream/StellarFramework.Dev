using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace StellarFramework.Tests.Policies.HotUpdatePublisher
{
    public sealed class HotUpdateAotMetadataSelectionTests
    {
        [Test]
        public void SelectBaseReleasePathsReturnsOnlyConfiguredMetadataInConfiguredOrder()
        {
            IReadOnlyList<string> selected = SelectBaseReleasePaths(
                new[] { "BaseRelease/mscorlib.dll", "BaseRelease/System.dll", "BaseRelease/System.Core.dll" },
                new[]
                {
                    "Assets/GameHotUpdate/Metadata/System.Core.dll.bytes",
                    "Assets/GameHotUpdate/Metadata/mscorlib.dll.bytes"
                });

            CollectionAssert.AreEqual(
                new[] { "BaseRelease/System.Core.dll", "BaseRelease/mscorlib.dll" }, selected);
        }

        [Test]
        public void SelectBaseReleasePathsFailsWhenConfiguredMetadataIsMissing()
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                SelectBaseReleasePaths(
                    new[] { "BaseRelease/mscorlib.dll" },
                    new[] { "Assets/GameHotUpdate/Metadata/System.dll.bytes" }));

            Assert.That(exception.InnerException, Is.TypeOf<FileNotFoundException>());
            Assert.That(exception.InnerException.Message, Does.Contain("System.dll"));
        }

        [Test]
        public void GetGeneratedAssetPathsUsesConfiguredNamesUnderPublisherOutputRoot()
        {
            Type selectorType = RequireSelectorType();
            MethodInfo method = selectorType.GetMethod("GetGeneratedAssetPaths", BindingFlags.Public | BindingFlags.Static);

            string[] paths = (string[])method.Invoke(null, new object[]
            {
                "Assets/Publisher/Generated",
                new[]
                {
                    "Assets/GameHotUpdate/Metadata/mscorlib.dll.bytes",
                    "Assets/GameHotUpdate/Metadata/UnityEngine.CoreModule.dll.bytes"
                }
            });

            CollectionAssert.AreEqual(new[]
            {
                "Assets/Publisher/Generated/Metadata/mscorlib.dll.bytes",
                "Assets/Publisher/Generated/Metadata/UnityEngine.CoreModule.dll.bytes"
            }, paths);
        }

        private static IReadOnlyList<string> SelectBaseReleasePaths(
            IEnumerable<string> availablePaths,
            IEnumerable<string> metadataKeys)
        {
            Type selectorType = RequireSelectorType();
            MethodInfo method = selectorType.GetMethod("SelectBaseReleasePaths", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return (IReadOnlyList<string>)method.Invoke(null, new object[] { availablePaths, metadataKeys });
        }

        private static Type RequireSelectorType()
        {
            Type selectorType = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                selectorType = assembly.GetType(
                    "StellarFramework.Editor.HotUpdatePublisher.HotUpdateAotMetadataSelection", false);
                if (selectorType != null) break;
            }

            Assert.That(selectorType, Is.Not.Null, "AOT metadata selection helper was not loaded.");
            return selectorType;
        }
    }
}
