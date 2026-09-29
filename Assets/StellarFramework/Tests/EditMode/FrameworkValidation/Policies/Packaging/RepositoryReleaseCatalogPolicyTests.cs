using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace StellarFramework.Tests.FrameworkValidation.Policies.Packaging
{
    public sealed class RepositoryReleaseCatalogPolicyTests
    {
        private const string DistributionCatalogPath =
            "Assets/StellarFramework/KitCatalog/KitDistributionCatalog.json";
        private const string RepositoryCatalogPath =
            "Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json";

        [Test]
        public void EveryDistributionProfileHasExactlyOneRepositoryOwner()
        {
            DistributionCatalog distribution = ReadJson<DistributionCatalog>(DistributionCatalogPath);
            RepositoryCatalog release = ReadJson<RepositoryCatalog>(RepositoryCatalogPath);

            Assert.That(release.schemaVersion, Is.EqualTo(1));
            Assert.That(release.sourceRepository, Is.EqualTo("StarrDream/StellarFramework.Dev"));
            Assert.That(release.general.repository, Is.EqualTo("StarrDream/StellarFramework"));
            Assert.That(release.extensions.repository, Is.EqualTo("StarrDream/StellarFramework.Extensions"));

            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            AddOwners(owners, release.general.profileIds, "general");
            foreach (ExtensionDomain domain in release.extensions.domains)
            {
                AddOwners(owners, domain.profileIds, "extensions." + domain.id);
            }

            string[] expected = distribution.profiles.Select(profile => profile.id).OrderBy(id => id).ToArray();
            string[] actual = owners.Keys.OrderBy(id => id).ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void GeneralProfilesNeverDependOnExtensionProfiles()
        {
            DistributionCatalog distribution = ReadJson<DistributionCatalog>(DistributionCatalogPath);
            RepositoryCatalog release = ReadJson<RepositoryCatalog>(RepositoryCatalogPath);
            var profiles = distribution.profiles.ToDictionary(profile => profile.id, StringComparer.Ordinal);
            var extensionIds = new HashSet<string>(
                release.extensions.domains.SelectMany(domain => domain.profileIds), StringComparer.Ordinal);

            foreach (string generalId in release.general.profileIds)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal);
                Visit(generalId, profiles, visited);
                string[] illegal = visited.Where(extensionIds.Contains).OrderBy(id => id).ToArray();
                Assert.That(illegal, Is.Empty,
                    $"General profile '{generalId}' depends on extension profiles: {string.Join(", ", illegal)}");
            }
        }

        [Test]
        public void ReleaseCatalogKeepsMaintainerSurfacesDevOnly()
        {
            RepositoryCatalog release = ReadJson<RepositoryCatalog>(RepositoryCatalogPath);
            CollectionAssert.Contains(release.devOnlyPaths, "Assets/StellarFramework/Tests");
            CollectionAssert.Contains(release.devOnlyPaths, "Assets/StellarFrameworkVerification");
            CollectionAssert.Contains(release.devOnlyPaths, "Assets/StellarFramework/FrameworkDoc/09-Development");
            CollectionAssert.Contains(release.devOnlyPaths, "Tools/AndroidVerification");
        }

        [Test]
        public void RepositoryPublisherHasDryRunBoundaryAndManifestGuards()
        {
            string source = File.ReadAllText(ToAbsolutePath("Tools/RepositoryPublisher/publish_repositories.py"));
            Assert.That(source, Does.Contain("--dry-run"));
            Assert.That(source, Does.Contain("General profile"));
            Assert.That(source, Does.Contain("requiredGeneralProfileIds"));
            Assert.That(source, Does.Contain("RELEASE-MANIFEST.json"));
            Assert.That(source, Does.Contain("verify_no_extension_assemblies_in_general"));
        }

        private static void AddOwners(Dictionary<string, string> owners, IEnumerable<string> ids, string owner)
        {
            foreach (string id in ids ?? Array.Empty<string>())
            {
                Assert.That(owners.ContainsKey(id), Is.False, $"Profile '{id}' has multiple repository owners.");
                owners.Add(id, owner);
            }
        }

        private static void Visit(string id, IReadOnlyDictionary<string, DistributionProfile> profiles,
            ISet<string> visited)
        {
            if (!visited.Add(id)) return;
            Assert.That(profiles.ContainsKey(id), Is.True, $"Unknown distribution profile '{id}'.");
            foreach (string dependency in profiles[id].requiredProfileIds ?? Array.Empty<string>())
            {
                Visit(dependency, profiles, visited);
            }
        }

        private static T ReadJson<T>(string assetPath)
        {
            string json = File.ReadAllText(ToAbsolutePath(assetPath));
            T result = JsonUtility.FromJson<T>(json);
            Assert.That(result, Is.Not.Null, assetPath);
            return result;
        }

        private static string ToAbsolutePath(string projectPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, projectPath.Replace('/', Path.DirectorySeparatorChar));
        }

        [Serializable]
        private sealed class DistributionCatalog
        {
            public DistributionProfile[] profiles;
        }

        [Serializable]
        private sealed class DistributionProfile
        {
            public string id;
            public string[] requiredProfileIds;
        }

        [Serializable]
        private sealed class RepositoryCatalog
        {
            public int schemaVersion;
            public string sourceRepository;
            public GeneralRelease general;
            public ExtensionsRelease extensions;
            public string[] devOnlyPaths;
        }

        [Serializable]
        private sealed class GeneralRelease
        {
            public string repository;
            public string[] profileIds;
        }

        [Serializable]
        private sealed class ExtensionsRelease
        {
            public string repository;
            public ExtensionDomain[] domains;
        }

        [Serializable]
        private sealed class ExtensionDomain
        {
            public string id;
            public string[] profileIds;
        }
    }
}
