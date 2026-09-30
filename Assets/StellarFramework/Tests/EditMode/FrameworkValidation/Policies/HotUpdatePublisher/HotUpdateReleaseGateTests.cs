using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using StellarFramework.Editor.HotUpdatePublisher;

namespace StellarFramework.Tests.FrameworkValidation.Policies.HotUpdatePublisher
{
    public sealed class HotUpdateReleaseGateTests
    {
        [Test]
        public void GreenHotPatchRequiresFastGate()
        {
            Assert.That(HotUpdateReleaseGatePolicy.Resolve(Context(GreenFacts())), Is.EqualTo(HotUpdateReleaseGateLevel.Fast));
        }

        [Test]
        public void YellowChangeRequiresFullGate()
        {
            var facts = new HotUpdateChangeFacts("Assets/_Project/Content/Shader.shader",
                HotUpdateChangeAssetKind.Shader, HotUpdateProjectLayer.RemoteContent);
            Assert.That(HotUpdateReleaseGatePolicy.Resolve(Context(facts)), Is.EqualTo(HotUpdateReleaseGateLevel.Full));
        }

        [Test]
        public void MajorHotPatchAndBaseAppReleaseRequireFullGate()
        {
            HotUpdatePublishContext major = Context(GreenFacts());
            major.IsMajorHotPatch = true;
            Assert.That(HotUpdateReleaseGatePolicy.Resolve(major), Is.EqualTo(HotUpdateReleaseGateLevel.Full));

            HotUpdatePublishContext baseRelease = Context(
                new HotUpdateChangeFacts("ProjectSettings/ProjectSettings.asset",
                    HotUpdateChangeAssetKind.ProjectConfiguration, HotUpdateProjectLayer.Unknown));
            baseRelease.IsBaseAppRelease = true;
            Assert.That(HotUpdateReleaseGatePolicy.Resolve(baseRelease), Is.EqualTo(HotUpdateReleaseGateLevel.Full));
        }

        [Test]
        public void RedOrdinaryHotPatchIsRejectedBeforeFastGate()
        {
            HotUpdatePublishContext context = Context(
                new HotUpdateChangeFacts("Assets/_Project/Base/Bootstrap.cs",
                    HotUpdateChangeAssetKind.CSharpSource, HotUpdateProjectLayer.BaseApp));
            var fast = new StubRunner(true);
            var stage = new HotUpdateReleaseGateStageHandler(fast, new StubRunner(true));

            HotUpdatePublishStepResult result = stage.ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(HotUpdatePublishErrorCode.ReleaseGatePolicyRejected));
            Assert.That(fast.CallCount, Is.Zero);
        }

        [Test]
        public void FullGateRunsOnlyAfterFastGatePasses()
        {
            bool originalOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            EnterPlayModeOptions originalOptions = EditorSettings.enterPlayModeOptions;
            string editorSettingsPath = Path.Combine(GetProjectRoot(), "ProjectSettings", "EditorSettings.asset");
            byte[] originalSerializedSettings = File.ReadAllBytes(editorSettingsPath);
            HotUpdatePublishContext context = Context(
                new HotUpdateChangeFacts("Assets/_Project/Content/Shader.shader",
                    HotUpdateChangeAssetKind.Shader, HotUpdateProjectLayer.RemoteContent));
            var order = new List<string>();
            var fast = new StubRunner(true, "Fast", order, () =>
            {
                Assert.That(EditorSettings.enterPlayModeOptionsEnabled, Is.True);
                Assert.That(EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload), Is.True);
            });
            var full = new StubRunner(true, "Full", order);

            HotUpdatePublishStepResult result = new HotUpdateReleaseGateStageHandler(fast, full)
                .ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.True);
            CollectionAssert.AreEqual(new[] { "Fast", "Full" }, order);
            Assert.That(context.ReleaseGateReport.Passed, Is.True);
            Assert.That(context.ReleaseGateReport.RequiredLevel, Is.EqualTo(HotUpdateReleaseGateLevel.Full));
            Assert.That(EditorSettings.enterPlayModeOptionsEnabled, Is.EqualTo(originalOptionsEnabled));
            Assert.That(EditorSettings.enterPlayModeOptions, Is.EqualTo(originalOptions));
            CollectionAssert.AreEqual(originalSerializedSettings, File.ReadAllBytes(editorSettingsPath));
        }

        [Test]
        public void FastFailureDoesNotStartFullGate()
        {
            HotUpdatePublishContext context = Context(
                new HotUpdateChangeFacts("Assets/_Project/Content/Shader.shader",
                    HotUpdateChangeAssetKind.Shader, HotUpdateProjectLayer.RemoteContent));
            var order = new List<string>();
            var fast = new StubRunner(false, "Fast", order);
            var full = new StubRunner(true, "Full", order);

            HotUpdatePublishStepResult result = new HotUpdateReleaseGateStageHandler(fast, full)
                .ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(HotUpdatePublishErrorCode.ReleaseGateFailed));
            CollectionAssert.AreEqual(new[] { "Fast" }, order);
            Assert.That(context.ReleaseGateReport.Passed, Is.False);
        }

        [Test]
        public void RequiredFullGateWithoutConfiguredRunnerFailsClosed()
        {
            HotUpdatePublishContext context = Context(
                new HotUpdateChangeFacts("Assets/_Project/Content/Shader.shader",
                    HotUpdateChangeAssetKind.Shader, HotUpdateProjectLayer.RemoteContent));
            var fast = new StubRunner(true);

            HotUpdatePublishStepResult result = new HotUpdateReleaseGateStageHandler(fast)
                .ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(HotUpdatePublishErrorCode.FullReleaseGateUnavailable));
            Assert.That(fast.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void FastGateRunnerReusesExactPassingEvidenceAfterEditorReload()
        {
            string releaseId = "fast-cache-test-" + Guid.NewGuid().ToString("N");
            string evidencePath = GetReleaseGateEvidencePath(releaseId, "-fast-gate.json");
            var processRunner = new NeverCalledGateProcessRunner();
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
            try
            {
                File.WriteAllText(evidencePath,
                    "{\"status\":\"PASS\",\"test\":{\"totalTests\":1,\"passedTests\":1,\"failedTests\":0,\"skippedTests\":0,\"inconclusiveTests\":0}}",
                    new UTF8Encoding(false));
                var runner = new PowerShellHotUpdateFastReleaseGateRunner(
                    GetProjectRoot(), "http://localhost:8090", processRunner, timeoutMinutes: 1);
                HotUpdateReleaseGateRun result = runner.RunAsync(
                    new HotUpdatePublishContext { ReleaseId = releaseId }, CancellationToken.None).GetAwaiter().GetResult();

                Assert.That(result.Passed, Is.True);
                Assert.That(result.EvidencePath, Is.EqualTo(evidencePath));
                Assert.That(processRunner.CallCount, Is.Zero);
            }
            finally
            {
                if (File.Exists(evidencePath)) File.Delete(evidencePath);
            }
        }

        [Test]
        public void AndroidFullGateRunnerReusesPassingColdStartAndRestartEvidence()
        {
            string releaseId = "android-cache-test-" + Guid.NewGuid().ToString("N");
            string evidencePath = GetReleaseGateEvidencePath(releaseId, "-android-full-gate.json");
            var processRunner = new NeverCalledGateProcessRunner();
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
            try
            {
                File.WriteAllText(evidencePath, PassingAndroidFullGateJson(), new UTF8Encoding(false));
                var runner = new PowerShellHotUpdateAndroidFullReleaseGateRunner(
                    GetProjectRoot(), "http://localhost:8090", processRunner, buildTimeoutMinutes: 1);
                HotUpdateReleaseGateRun result = runner.RunAsync(
                    new HotUpdatePublishContext { ReleaseId = releaseId, Platform = BuildTarget.Android },
                    CancellationToken.None).GetAwaiter().GetResult();

                Assert.That(result.Passed, Is.True);
                Assert.That(result.EvidencePath, Is.EqualTo(evidencePath));
                Assert.That(processRunner.CallCount, Is.Zero);
            }
            finally
            {
                if (File.Exists(evidencePath)) File.Delete(evidencePath);
            }
        }

        private static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string GetReleaseGateEvidencePath(string releaseId, string suffix)
        {
            return Path.Combine(GetProjectRoot(), "BuildArtifacts", "HotUpdate", "ReleaseGates", releaseId + suffix);
        }

        private static string PassingAndroidFullGateJson()
        {
            const string runtime = "{\"status\":\"PASS\",\"platform\":\"Android\",\"manifestBuildTarget\":\"Android\",\"contentUpdateSucceeded\":true,\"resKitManifestLoaded\":true,\"resKitAssemblyLoaded\":true,\"assemblySha256Verified\":true,\"aotMetadataLoadSucceeded\":true,\"assemblyLoadSucceeded\":true,\"entryPointInvoked\":true,\"entryMarkerObserved\":true,\"loadedAssemblyFullName\":\"HotUpdate, Version=1.0.0.0\",\"expectedAssemblySha256\":\"abc\",\"actualAssemblySha256\":\"abc\"}";
            return "{\"status\":\"PASS\",\"profile\":\"HotUpdate\",\"productVerificationStatus\":\"PASS\",\"cleanupStatus\":\"PASS\",\"hotUpdateRuntime\":{\"coldStart\":" + runtime + ",\"restart\":" + runtime + "}}";
        }

        private static HotUpdatePublishContext Context(params HotUpdateChangeFacts[] facts)
        {
            return new HotUpdatePublishContext
            {
                ReleaseId = "gate-test",
                ChangeClassification = HotUpdateChangeClassifier.Classify(facts)
            };
        }

        private static HotUpdateChangeFacts GreenFacts()
        {
            return new HotUpdateChangeFacts("Assets/_Project/HotUpdate/Gameplay/Player.cs",
                HotUpdateChangeAssetKind.CSharpSource, HotUpdateProjectLayer.HotUpdate);
        }

        private sealed class StubRunner : IHotUpdateFastReleaseGateRunner, IHotUpdateFullReleaseGateRunner
        {
            private readonly bool _pass;
            private readonly string _name;
            private readonly List<string> _order;
            private readonly Action _onRun;

            public StubRunner(bool pass, string name = "Gate", List<string> order = null, Action onRun = null)
            {
                _pass = pass;
                _name = name;
                _order = order;
                _onRun = onRun;
            }

            public int CallCount { get; private set; }

            public Task<HotUpdateReleaseGateRun> RunAsync(HotUpdatePublishContext context, CancellationToken cancellationToken)
            {
                CallCount++;
                _order?.Add(_name);
                _onRun?.Invoke();
                return Task.FromResult(new HotUpdateReleaseGateRun
                {
                    Passed = _pass,
                    EvidencePath = _name + ".json",
                    Diagnostic = _pass ? string.Empty : _name + " failed."
                });
            }
        }

        private sealed class NeverCalledGateProcessRunner : IHotUpdateGateProcessRunner
        {
            public int CallCount { get; private set; }

            public Task<HotUpdateGateProcessResult> RunAsync(
                string executable,
                string arguments,
                string workingDirectory,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(new HotUpdateGateProcessResult
                {
                    ExitCode = 99,
                    Diagnostic = "The cached evidence path should not start a process."
                });
            }
        }
    }
}
