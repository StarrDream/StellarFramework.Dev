# StellarFramework Android Verification

Dedicated Android emulator automation for StellarFramework release validation.

## First-time setup

1. Install Unity **2022.3.62f3c1** with the **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** modules. Unity uses its bundled SDK/JDK to build the APK.
2. Install Android command-line tools into an SDK root containing `platform-tools/adb.exe` and `build-tools/*/aapt.exe`. The scripts default to `C:\Android\Sdk`; override it for the current PowerShell session with `$env:STELLAR_ANDROID_SDK_ROOT = 'D:\Android\Sdk'`.
3. In SDK Manager, install `platform-tools`, `emulator`, `platforms;android-35`, `system-images;android-35;google_apis;x86_64`, and `build-tools;35.0.0`. The API 35 build-tools package provides `aapt` for reading APK metadata.
4. Create an AVD named `StellarFramework_API35` using the `android-35;google_apis;x86_64` image for the default automated setup. The launcher addresses this AVD by name, so other connected devices are left alone. A running MuMu instance or another Android device can be selected explicitly instead; see **Use a connected device** below.
5. Enable CPU virtualization in firmware and the Windows Hypervisor Platform feature, then restart Windows if the feature installer requests it. The standard environment check requires hardware acceleration; software CPU emulation is too slow and is not the normal release-gate configuration.
6. Run the environment check below. It verifies `adb`, `emulator`, `aapt`, the AVD, and emulator acceleration before running a release test.

For example, when `avdmanager` is on PATH and `ANDROID_SDK_ROOT` points at the SDK:

```powershell
avdmanager create avd -n StellarFramework_API35 -k "system-images;android-35;google_apis;x86_64" -d pixel_2
```

Open the resulting AVD configuration and set its RAM to **4096 MB** for the HotUpdate Release profile. A smaller device is sufficient for the ordinary APK smoke profile.

## Expected local configuration

- Default SDK root: `C:\Android\Sdk` (or `STELLAR_ANDROID_SDK_ROOT`)
- AVD: `StellarFramework_API35`
- Image: Android 15 / API 35 / Google APIs / x86_64
- Acceleration: Windows Hypervisor Platform (WHPX)
- ADB/Platform Tools: installed in the dedicated SDK, separate from Unity Hub's embedded SDK
- JDK for Android CLI: Microsoft OpenJDK 17
- Unity 2022.3 keeps using its own embedded JDK/SDK unless its External Tools settings are changed manually.

The default scripts identify the target emulator by **AVD name**, not by “first ADB device”, so connected PICO/phones/other emulators are not targeted accidentally. An external device is used only when its exact ADB serial is supplied explicitly.

## Use a connected device

Set the SDK root and the exact serial shown by `adb devices`. This mode needs `adb` and `aapt`; it does not require the Android Emulator binary or an installed AVD.

```powershell
$env:STELLAR_ANDROID_SDK_ROOT = 'D:\Program Files\UnityEditor\2022.3.62f3c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK'
$env:STELLAR_ANDROID_DEVICE_SERIAL = '127.0.0.1:16416'
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1 -HotUpdate
```

The HotUpdate profile checks that the configured device is online and reports at least 3584 MB of visible RAM. It leaves a configured external device running after the gate; the script removes only the `adb reverse` mapping it created.

## Validate environment

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Test-StellarAndroidEnvironment.ps1
```

## Start the dedicated emulator

Headless:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Start-StellarAndroid.ps1
```

Windowed:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Start-StellarAndroid.ps1 -Windowed
```

The launcher uses a free emulator console port and later resolves the device by AVD name.

## Stop

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Stop-StellarAndroid.ps1
```

## APK smoke test

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarApkSmoke.ps1 `
  -ApkPath .\Builds\MyValidation.apk `
  -RuntimeSeconds 20
```

`PackageName` and the launch activity are read from the APK with the official Android `aapt` tool by default. They can still be overridden explicitly for diagnosis.

The smoke runner:

1. finds/starts only `StellarFramework_API35`;
2. waits for `sys.boot_completed=1`;
3. disables Android animation scales for deterministic automation;
4. installs/replaces the APK and clears app data;
5. cold starts the launchable Activity and asserts the process stays alive;
6. records full/app-scoped logcat, package/activity dumps, screenshot, and `result.json`;
7. fails on configured Unity Error / FATAL EXCEPTION / ANR patterns;
8. force-stops and restarts the app, then repeats process/log validation.

For a build that contains the ArchitectureDemo UIAdaptation integration, add `-RequireUIAdaptationPass` to require a SafeAreaRoot geometry record. Add `-RequireSafeAreaInsets` when the connected Android device has an active display cutout; this requires at least one cold-start or restart record with non-zero Safe Area insets. Android emulator images commonly expose cutout overlays through `cmd overlay`; restore the overlay to its original state after the check.

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1 `
  -RequireUIAdaptationPass `
  -RequireSafeAreaInsets
```

## Full Release verification pipeline

Run from a shell while no other Unity process has the same project open:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1
```

This performs:

`Unity Release Build -> Start/Wait Emulator -> Install -> Clear Data -> Cold Start -> Smoke -> logcat -> Screenshot -> Stop/Restart App -> PASS/FAIL`.

The Unity build entry point is `StellarFrameworkAndroidReleaseVerificationBuild.BuildRelease`. It temporarily selects IL2CPP + x86_64 + non-development APK output and restores the previous Unity Android build settings in `finally`. It does not modify Unity Hub's embedded SDK/JDK configuration.

Unity build coordination state is written to `Library/StellarFramework/AndroidVerification/android-build-state.json`, intentionally separate from the APK output directory so Unity/Gradle output cleanup cannot delete the pipeline handshake file.

For emulator/ADB-only validation of an already built APK:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1 `
  -SkipBuild `
  -ApkPath .\Builds\AndroidEmulator\StellarFramework-ArchitectureDemo-x86_64.apk
```

## Android Release IL2CPP HotUpdate profile

Run the full target-platform HotUpdate gate from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1 `
  -HotUpdate
```

This reuses the Android Release pipeline and smoke runner. It requires the Unity Active Build Target to be Android, runs HybridCLR `Generate/All` under temporary IL2CPP + x86_64 settings, exports freshly generated Android AOT metadata / `HotUpdate.dll` / Manifest SHA, and rebuilds the verification YooAsset package. The HotUpdate verification APK temporarily sets Unity's `insecureHttpOption` to `AlwaysAllowed` so it can exercise the HTTP CDN; the build method restores the previous Player setting in `finally`.

That setting change applies only to the verification APK. If a shipping Android app downloads from a plain HTTP host, set **Allow downloads over HTTP** to **Always allowed** in the app's Android Player Settings, or serve the package over HTTPS. Unity blocks plain HTTP by default, and notes that unencrypted connections are not secure ([`PlayerSettings.insecureHttpOption`](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/PlayerSettings-insecureHttpOption.html), [`InsecureHttpOption.AlwaysAllowed`](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/InsecureHttpOption.AlwaysAllowed.html)).

The default pipeline serves the package from a local Python standard-library HTTP server bound only to `127.0.0.1`, then uses `adb reverse` to connect the emulator to it. It builds a non-Development IL2CPP APK and passes CDN/package/version values through Android launch Intent extras. The app must prove a cold download and a force-stop/restart cache hit. During each HotUpdate launch, the smoke runner waits for the Player's `BootstrapEntered` marker; it uses UIAutomator to find Android's accessible startup prompt buttons and selects **Wait** for `android:id/aerr_wait` when a system app is unresponsive. It records the number of handled system prompts and fails if the Player bootstrap does not appear within the bounded startup window. Each run emits the complete structured result as ordered, 512-character Base64 log chunks because Unity's Android logger truncates long messages; the smoke runner requires every chunk exactly once, reconstructs and parses the JSON, then asserts Manifest target Android, ResKit loads, SHA match, successful AOT metadata load, loaded `HotUpdate` Assembly, entry execution marker, and 0 redownloads after restart.

### Testing against an external CDN

Use this two-step flow when the CDN is on another machine and the package must be copied to that machine. From the repository root, prepare a fresh Android HotUpdate APK and YooAsset package without starting the local test CDN or installing the APK:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1 `
  -HotUpdate `
  -PrepareOnly
```

The command prints the APK path, package directory and a ZIP archive and records them in `Tools/AndroidVerification/Results/<run-id>/pipeline-result.json`. For a simple upload, copy `StellarHotUpdateVerification-android.zip` to the server and extract its contents directly into the CDN document root, preserving all relative paths. For the Caddy setup in the external-CDN test, the extracted files go directly under `D:\StellarHotUpdate`, which Caddy serves at the site root. Do not create an extra subdirectory for the ZIP contents. Before testing the APK, open `http://dreamstarry.cn:18743/StellarHotUpdateVerification.version`; it should return the version recorded in the APK's verification manifest (currently `tank-arena-v7`).

After the upload is complete, use the already running MuMu device and run the APK smoke gate against the public hostname. Set the ADB serial to the MuMu serial reported by `adb devices` (the example below uses `127.0.0.1:16416`):

```powershell
$env:STELLAR_ANDROID_DEVICE_SERIAL = '127.0.0.1:16416'
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarApkSmoke.ps1 `
  -ApkPath .\Builds\AndroidVerification\StellarFramework-HotUpdate-x86_64-release.apk `
  -RequireHotUpdatePass `
  -HotUpdateHost dreamstarry.cn `
  -HotUpdatePort 18743 `
  -HotUpdatePackageName StellarHotUpdateVerification `
  -HotUpdatePackageVersion tank-arena-v7 `
  -RuntimeSeconds 90 `
  -RestartRuntimeSeconds 30
```

The smoke runner accepts a DNS hostname or IPv4 address and constructs an HTTP origin using the supplied port. It clears the app data for a cold download, then force-stops and restarts the app to verify that YooAsset uses the cached package without downloading the files again. The result is written to `Tools/AndroidVerification/Results/<run-id>/result.json`; a successful external-CDN run must report both cold-start and restart HotUpdate evidence as `PASS`.

Without an explicit device serial, the HotUpdate profile starts the existing `StellarFramework_API35` AVD with 4096 MB RAM. A manually running AVD is reused only when Android reports at least 3584 MB in `/proc/meminfo`; otherwise the pipeline stops before install and asks for that same AVD to be restarted through the existing helper. With an explicit device serial, it uses only that device and applies the same 3584 MB check. The ordinary smoke profile keeps its 2048 MB default.

Evidence is written under `Tools/AndroidVerification/Results/<run-id>/`, including both app logcats, full logcats, screenshot, `result.json`, `pipeline-result.json`, build state references, and CDN request JSONL. Android runtime logs include `[StellarHotUpdateVerificationStage]` milestones so a stalled updater can be located without treating a startup message as PASS; only the final structured result satisfies the smoke gate. Use `-CdnPort` or `-PythonExe` when the local default is unavailable. `-SkipBuild` is intentionally unsupported with `-HotUpdate` because the gate must regenerate target artifacts and build a fresh Release APK.

If the UnitySkills gateway returns HTTP 504 after its request timeout, the pipeline waits for the Editor-written build state and continues only when that state is `PASS`, the Android build result is `Succeeded`, and the recorded APK exists. `pipeline-result.json` records the recovered transport warning; a missing, failed, or incomplete build artifact remains a hard failure.

The PlayMode Range interruption/resume Gate remains a separate complementary check; Android runs here do not inject a download interruption.

Headless startup uses Google SwiftShader instead of the host GPU renderer. This is slower than direct GPU rendering but materially more deterministic for unattended CI and avoids host-driver instability observed with `-gpu auto`.

This is a **smoke gate**, not a replacement for real-device GPU, vendor-ROM, ARM64/native-plugin, XR, PICO or hardware-sensor testing.
