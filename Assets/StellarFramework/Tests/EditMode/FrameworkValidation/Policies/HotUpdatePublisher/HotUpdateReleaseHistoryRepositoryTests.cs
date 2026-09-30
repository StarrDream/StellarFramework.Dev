using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StellarFramework.Editor.HotUpdatePublisher;
using UnityEditor;

namespace StellarFramework.Tests.FrameworkValidation.Policies.HotUpdatePublisher
{
    public sealed class HotUpdateReleaseHistoryRepositoryTests
    {
        private string _root;
        private HotUpdateReleaseHistoryRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "stellar-release-history-" + Guid.NewGuid().ToString("N"));
            _repository = new HotUpdateReleaseHistoryRepository(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void ActivatedRecordPersistsClassificationAndFileHashes()
        {
            HotUpdateReleaseRecord record = Record("release-001", "2026.09.24.001");
            _repository.SaveActivated(record);

            HotUpdateReleaseRecord loaded = _repository.Load(record.ReleaseId);
            Assert.That(loaded.Status, Is.EqualTo(HotUpdateReleaseRecordStatus.Active));
            Assert.That(loaded.Files, Has.Length.EqualTo(2));
            Assert.That(loaded.Files[1].RelativePath, Is.EqualTo("Bundles/a.bundle"));
            Assert.That(loaded.Files[0].Sha256, Is.EqualTo(new string('a', 64)));
            Assert.That(loaded.ChangeClassification.Safety, Is.EqualTo("GREEN"));
            Assert.That(_repository.List(), Has.Count.EqualTo(1));
            Assert.That(Directory.GetFiles(Path.Combine(_root, "Events"), "*.json").Length, Is.EqualTo(1));
        }

        [Test]
        public void CreationTimestampPersistsAndNewestReleaseSortsFirst()
        {
            DateTime older = new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
            DateTime newer = older.AddMinutes(5);
            HotUpdateReleaseRecord first = Record("release-001", "2026.09.30.001");
            first.CreatedAtUtc = older;
            HotUpdateReleaseRecord second = Record("release-002", "2026.09.30.002");
            second.CreatedAtUtc = newer;

            _repository.SaveActivated(first);
            _repository.SaveActivated(second);

            Assert.That(_repository.Load(first.ReleaseId).CreatedAtUtc, Is.EqualTo(older));
            Assert.That(_repository.Load(second.ReleaseId).CreatedAtUtc, Is.EqualTo(newer));
            Assert.That(_repository.List().Select(item => item.ReleaseId).ToArray(),
                Is.EqualTo(new[] { second.ReleaseId, first.ReleaseId }));
            Assert.That(File.ReadAllText(Path.Combine(_root, "release-" + second.ReleaseId + ".json")),
                Does.Contain("CreatedAtUtcIso8601"));
            Assert.That(File.ReadAllText(Directory.GetFiles(Path.Combine(_root, "Events"), "*.json")[0]),
                Does.Contain("CreatedAtUtcIso8601"));
        }

        [Test]
        public void LegacyRecordWithoutTimestampUsesActivationEventTimeForNewestFirstOrdering()
        {
            HotUpdateReleaseRecord older = Record("release-legacy-001", "2026.09.30.001");
            HotUpdateReleaseRecord newer = Record("release-legacy-002", "2026.09.30.002");
            older.CreatedAtUtcIso8601 = null;
            newer.CreatedAtUtcIso8601 = null;
            string olderPath = Path.Combine(_root, "release-" + older.ReleaseId + ".json");
            string newerPath = Path.Combine(_root, "release-" + newer.ReleaseId + ".json");
            Directory.CreateDirectory(_root);
            File.WriteAllText(olderPath, UnityEngine.JsonUtility.ToJson(older, true));
            File.WriteAllText(newerPath, UnityEngine.JsonUtility.ToJson(newer, true));
            DateTime baseTime = new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(olderPath, baseTime.AddMinutes(4));
            File.SetLastWriteTimeUtc(newerPath, baseTime.AddMinutes(3));
            WriteLegacyActivationEvent(older.ReleaseId, older.PackageVersion,
                Path.Combine(_root, "Events", "event-legacy-001.json"), baseTime);
            WriteLegacyActivationEvent(newer.ReleaseId, newer.PackageVersion,
                Path.Combine(_root, "Events", "event-legacy-002.json"), baseTime.AddMinutes(1));

            Assert.That(_repository.List().Select(item => item.ReleaseId).ToArray(),
                Is.EqualTo(new[] { newer.ReleaseId, older.ReleaseId }));
            Assert.That(_repository.Load(newer.ReleaseId).CreatedAtUtc, Is.EqualTo(baseTime.AddMinutes(1)));
        }

        [Test]
        public void TimestampLaterThanActivationEventIsRepairedFromImmutableEvent()
        {
            HotUpdateReleaseRecord record = Record("release-legacy-003", "2026.09.30.003");
            DateTime activationTime = new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
            record.CreatedAtUtcIso8601 = activationTime.AddMinutes(5).ToString("O");
            Directory.CreateDirectory(_root);
            string recordPath = Path.Combine(_root, "release-" + record.ReleaseId + ".json");
            File.WriteAllText(recordPath, UnityEngine.JsonUtility.ToJson(record, true));
            WriteLegacyActivationEvent(record.ReleaseId, record.PackageVersion,
                Path.Combine(_root, "Events", "event-legacy-003.json"), activationTime);

            HotUpdateReleaseRecord loaded = _repository.Load(record.ReleaseId);

            Assert.That(loaded.CreatedAtUtc, Is.EqualTo(activationTime));
            Assert.That(loaded.CreatedAtUtcIso8601, Is.EqualTo(activationTime.ToString("O")));
        }

        [Test]
        public void NewActiveReleaseSupersedesPriorActiveForSamePackageEnvironmentAndPlatform()
        {
            _repository.SaveActivated(Record("release-001", "2026.09.24.001"));
            _repository.SaveActivated(Record("release-002", "2026.09.24.002"));

            Assert.That(_repository.Load("release-001").Status, Is.EqualTo(HotUpdateReleaseRecordStatus.Superseded));
            Assert.That(_repository.Load("release-002").Status, Is.EqualTo(HotUpdateReleaseRecordStatus.Active));
            Assert.That(_repository.List().Select(item => item.Status).ToArray(),
                Is.EquivalentTo(new[] { HotUpdateReleaseRecordStatus.Superseded, HotUpdateReleaseRecordStatus.Active }));
        }

        [Test]
        public void DuplicateReleaseIdIsRejectedWithoutReplacingExistingRecord()
        {
            _repository.SaveActivated(Record("release-001", "2026.09.24.001"));
            HotUpdateReleaseRecord duplicate = Record("release-001", "2026.09.24.999");

            Assert.Throws<IOException>(() => _repository.SaveActivated(duplicate));
            Assert.That(_repository.Load("release-001").PackageVersion, Is.EqualTo("2026.09.24.001"));
        }

        [Test]
        public void StatusTransitionPersistsSeparateHistoryEvent()
        {
            _repository.SaveActivated(Record("release-001", "2026.09.24.001"));

            _repository.SetStatus("release-001", HotUpdateReleaseRecordStatus.RolledBack,
                new HotUpdateReleaseHistoryEvent
                {
                    EventType = "Rollback",
                    FromVersion = "2026.09.24.001",
                    ToVersion = "2026.09.24.000",
                    Diagnostic = "Verified historical package and switched pointer."
                });

            Assert.That(_repository.Load("release-001").Status, Is.EqualTo(HotUpdateReleaseRecordStatus.RolledBack));
            string[] events = Directory.GetFiles(Path.Combine(_root, "Events"), "*.json");
            Assert.That(events, Has.Length.EqualTo(2));
            bool rollbackEventFound = false;
            for (int index = 0; index < events.Length; index++)
                rollbackEventFound |= File.ReadAllText(events[index]).Contains("Rollback");
            Assert.That(rollbackEventFound, Is.True);
            Assert.That(events.Select(File.ReadAllText), Has.Some.Contains("CreatedAtUtcIso8601"));
        }

        [Test]
        public void ReleaseIdPathTraversalIsRejected()
        {
            Assert.Throws<ArgumentException>(() => _repository.Load("../outside"));
        }

        private static HotUpdateReleaseRecord Record(string releaseId, string version)
        {
            return new HotUpdateReleaseRecord
            {
                ReleaseId = releaseId,
                PackageName = "GameContent",
                PackageVersion = version,
                BaseAppVersion = "1.0.0",
                Platform = BuildTarget.Android,
                Environment = "Production",
                GitCommit = "deadbeef",
                GitBranch = "main",
                GitDirty = false,
                HotUpdateDllSha256 = new string('d', 64),
                BundleCount = 1,
                ChangeClassification = new HotUpdateReleaseChangeClassification
                {
                    GreenCount = 2,
                    Safety = "GREEN"
                },
                Files = new[]
                {
                    new HotUpdateReleaseFileRecord { RelativePath = "manifest.json", Length = 123, Sha256 = new string('a', 64) },
                    new HotUpdateReleaseFileRecord { RelativePath = "Bundles/a.bundle", Length = 456, Sha256 = new string('b', 64) }
                },
                ManifestFiles = new[] { "manifest.hash", "manifest.json", "manifest.version", "PackageVersion" },
                TotalBytes = 579,
                GateResult = "FAST_PASS",
                ServerRoot = "https://cdn.example.test/GameContent/Android/Production",
                CreatedAtUtc = DateTime.UtcNow,
                Status = HotUpdateReleaseRecordStatus.Active
            };
        }

        private static void WriteLegacyActivationEvent(string releaseId, string version, string path, DateTime fileTimeUtc)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(new HotUpdateReleaseHistoryEvent
            {
                EventId = Path.GetFileNameWithoutExtension(path),
                ReleaseId = releaseId,
                EventType = "Activated",
                ToVersion = version
            }, true));
            File.SetLastWriteTimeUtc(path, fileTimeUtc);
        }
    }
}
