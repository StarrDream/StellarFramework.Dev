# Tank Arena hot-update demo

`HotUpdateMain.Main()` starts **Iron Vanguard**, a compact portrait-mode tank survival game. The game is compiled into `HotUpdate.dll`; it is downloaded with YooAsset and entered through HybridCLR. The Android verification app therefore starts the game code from the remote package rather than from its Player assembly. The battlefield, vehicle silhouettes, combat feedback and match-end panels are built at runtime so the demo stays inside the hot-update assembly.

## Play

- Drag the lower-left pad to move and turn the hull.
- Drag the lower-right pad to aim the turret independently. In **MANUAL** mode, keep holding that pad to fire in the direction it points.
- **AUTO** mode aims at the nearest hostile and fires for you. Tap the mode control beside the language button to switch between **AUTO** and **MANUAL**.
- Collect a repair core to restore hull integrity.
- Pause, resume, restart, or switch between English and Chinese from the HUD.
- The badge at the upper right shows the hot-update package version currently built into this demo.
- On desktop, use **WASD** to move the hull and drag the right aim pad with the mouse. In **MANUAL** mode, hold the right pad or press **Space** to fire along the turret's current heading.

Hostile armor and movement speed increase as eliminations advance the wave. The match ends when hull integrity reaches zero.

## Framework path

- `TankArenaArchitecture` registers the game Model and Services.
- `TankArenaModel` owns score, hull, wave, eliminations, and match phase through BindableKit.
- `TankArenaService` applies match rules; `TankArenaGame` is the View and input/presentation layer.
- `TankArenaLocalizationService` creates the demo catalog and formats the end-of-match result with LocalizationKit.
- ActionKit runs bounded UI feedback animations.
- UIAdaptationKit applies the device safe area and selects the portrait/landscape CanvasScaler profile.
- YooAsset loads the versioned package and ResKit supplies the manifest and hot-update assembly; HybridCLR loads and enters `HotUpdateMain.Main()`.

## Android verification

Use **Tools → StellarFramework → Verification → Prepare Android HotUpdate Release Gate** to build the Android package. The prepared package version is `1.0.0`. The Android smoke script verifies a cold download, the loaded assembly hash, metadata loading, entry-point execution, and a restart from the YooAsset cache. When testing against an external HTTP CDN, Android's insecure-download setting is enabled only for the verification APK; production builds should use HTTPS or set the appropriate Player setting explicitly.
