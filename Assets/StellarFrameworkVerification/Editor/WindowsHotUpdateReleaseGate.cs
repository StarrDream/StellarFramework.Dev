#if UNITY_EDITOR
using System;
using System.IO;
using HybridCLR.Editor.Commands;
using StellarFramework.Editor.Modules;
using StellarFramework.HybridCLR;
using StellarFrameworkVerification.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using YooAsset;

namespace StellarFrameworkVerification.Editor
{
    /// <summary>
    /// Builds a Windows x64 IL2CPP Player and its matching YooAsset package, then arms the
    /// Player's local Range-resume and HybridCLR verification bootstrap.
    /// </summary>
    public static class WindowsHotUpdateReleaseGate
    {
        private const BuildTarget Target = BuildTarget.StandaloneWindows64;
        private const string ScenePath =
            "Assets/StellarFramework/Samples/ArchitectureDemo/Scene/FrameworkArchitecture_Playable.unity";

        [Serializable]
        private sealed class BuildState
        {
            public string status;
            public string buildTarget;
            public string scriptingBackend;
            public string packageName;
            public string packageVersion;
            public string packageDirectory;
            public string playerPath;
            public string runtimeConfigPath;
            public string manifestSha256;
            public int packageBundleCount;
            public int totalErrors;
            public int totalWarnings;
            public string error;
        }

        [MenuItem("Tools/StellarFramework/Verification/Build Windows Release HotUpdate Player")]
        public static void BuildReleasePlayer()
        {
            if (EditorUserBuildSettings.activeBuildTarget != Target)
            {
                throw new BuildFailedException(
                    "Windows HotUpdate verification requires the active BuildTarget to be StandaloneWindows64.");
            }
            if (BuildPipeline.isBuildingPlayer)
            {
                throw new BuildFailedException("Another Unity Player build is already running.");
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new FileNotFoundException("Windows verification scene was not found.", ScenePath);
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unable to resolve the Unity project root.");
            string runName = "HotUpdate-v" + YooAssetHotUpdateVerificationBuilder.PackageVersion + "-" +
                             DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string playerDirectory = Path.Combine(projectRoot, "Builds", "WindowsVerification", runName);
            string playerPath = Path.Combine(playerDirectory, "StellarFramework-HotUpdate-Verification.exe");
            string statePath = Path.Combine(
                projectRoot,
                "Library",
                "StellarFramework",
                "WindowsVerification",
                "windows-hotupdate-release-build.json");
            Directory.CreateDirectory(playerDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(statePath));

            var state = new BuildState
            {
                status = "RUNNING",
                buildTarget = Target.ToString(),
                packageName = YooAssetHotUpdateVerificationBuilder.PackageName,
                packageVersion = YooAssetHotUpdateVerificationBuilder.PackageVersion,
                playerPath = playerPath,
                error = string.Empty
            };
            WriteState(statePath, state);

            NamedBuildTarget standalone = NamedBuildTarget.Standalone;
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(standalone);
            InsecureHttpOption previousHttpOption = PlayerSettings.insecureHttpOption;
            bool previousDevelopment = EditorUserBuildSettings.development;
            StandaloneBuildSubtarget previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            EditorBuildSettingsScene[] previousScenes = EditorBuildSettings.scenes;
            bool temporarySceneAdded = false;

            try
            {
                PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.IL2CPP);
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

                if (previousScenes == null || previousScenes.Length == 0)
                {
                    EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                    temporarySceneAdded = true;
                }

                state.scriptingBackend = PlayerSettings.GetScriptingBackend(standalone).ToString();
                Debug.Log("[WindowsHotUpdateReleaseGate] Running HybridCLR Generate/All for " + Target + ".");
                PrebuildCommand.GenerateAll();

                string hotUpdateDllPath =
                    HybridCLRHotUpdateAssetExporter.GetGeneratedHotUpdateSourceDirectory(Target) + "/HotUpdate.dll";
                if (!File.Exists(hotUpdateDllPath))
                {
                    throw new FileNotFoundException(
                        "HybridCLR Generate/All did not produce the Windows HotUpdate.dll.",
                        hotUpdateDllPath);
                }

                HybridCLRHotUpdateExportReport export = HybridCLRHotUpdateAssetExporter.ExportGeneratedAssets(Target);
                if (export == null || !export.Success)
                {
                    throw new BuildFailedException(
                        "Windows HybridCLR asset export failed: " +
                        (export == null ? "No export report." : string.Join(" | ", export.Errors)));
                }

                HotUpdateManifest manifest = HotUpdateManifest.FromJson(export.ManifestJson);
                string actualDllSha256 = HybridCLRHotUpdateAssetExporter.ComputeSha256Hex(
                    File.ReadAllBytes(hotUpdateDllPath));
                if (manifest == null || !string.Equals(manifest.buildTarget, Target.ToString(), StringComparison.Ordinal))
                {
                    throw new BuildFailedException("Generated HotUpdateManifest does not identify StandaloneWindows64.");
                }
                if (!string.Equals(
                        HotUpdateManifest.NormalizeSha256(manifest.hotUpdateAssemblySha256),
                        actualDllSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new BuildFailedException("Windows HotUpdateManifest SHA256 does not match HotUpdate.dll.");
                }
                state.manifestSha256 = actualDllSha256;

                YooAsset.Editor.BuildResult package = YooAssetHotUpdateVerificationBuilder.Build(
                    packageVersion: YooAssetHotUpdateVerificationBuilder.PackageVersion);
                state.packageDirectory = package.OutputPackageDirectory.Replace('\\', '/');
                state.packageBundleCount = Directory.GetFiles(
                    state.packageDirectory,
                    "*.bundle",
                    SearchOption.TopDirectoryOnly).Length;
                if (state.packageBundleCount <= 0)
                {
                    throw new BuildFailedException("Windows YooAsset verification package contains no bundles.");
                }

                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = playerPath,
                    target = Target,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                });
                state.totalErrors = (int)report.summary.totalErrors;
                state.totalWarnings = (int)report.summary.totalWarnings;
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded ||
                    !File.Exists(playerPath))
                {
                    throw new BuildFailedException(
                        $"Windows IL2CPP Player build failed: {report.summary.result}, " +
                        $"errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}.");
                }

                string verificationRoot = Path.Combine(playerDirectory, "Temp", "StellarHotUpdateVerification");
                Directory.CreateDirectory(verificationRoot);
                string configPath = Path.Combine(verificationRoot, "runtime-config.json");
                var config = new HotUpdateVerificationConfig
                {
                    packageDirectory = state.packageDirectory,
                    cacheRoot = Path.Combine(verificationRoot, "ClientCache"),
                    packageName = state.packageName,
                    expectedPackageVersion = state.packageVersion,
                    interruptAfterBytes = 256 * 1024
                };
                File.WriteAllText(configPath, JsonUtility.ToJson(config, true), new System.Text.UTF8Encoding(false));
                state.runtimeConfigPath = configPath;
                state.status = "PASS";
                Debug.Log(
                    $"[WindowsHotUpdateReleaseGate] Player prepared. Package={state.packageVersion}, " +
                    $"Bundles={state.packageBundleCount}, Output={playerPath}.");
            }
            catch (Exception exception)
            {
                state.status = "FAIL";
                state.error = exception.ToString();
                throw;
            }
            finally
            {
                if (temporarySceneAdded)
                {
                    EditorBuildSettings.scenes = previousScenes;
                }
                PlayerSettings.SetScriptingBackend(standalone, previousBackend);
                PlayerSettings.insecureHttpOption = previousHttpOption;
                EditorUserBuildSettings.development = previousDevelopment;
                EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;
                WriteState(statePath, state);
            }
        }

        private static void WriteState(string statePath, BuildState state)
        {
            string temporaryPath = statePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true), new System.Text.UTF8Encoding(false));
            if (File.Exists(statePath)) File.Delete(statePath);
            File.Move(temporaryPath, statePath);
        }
    }
}
#endif
