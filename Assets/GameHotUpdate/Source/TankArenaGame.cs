using System;
using System.Collections.Generic;
using System.Globalization;
using StellarFramework;
using StellarFramework.Bindable;
using StellarFramework.Localization;
using StellarFramework.UI.Adaptation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate
{
    /// <summary>
    /// Small, fully hot-updated game loop used by the public Android demo.
    /// All gameplay and presentation code lives in HotUpdate.dll; the Player only supplies
    /// Unity and StellarFramework runtime Kits.
    /// </summary>
    public sealed class TankArenaGame : MonoBehaviour, IView
    {
        private const string RootName = "StellarTankArena";
        private const int WaveSize = 5;
        private const int MaximumEnemies = 8;

        private static readonly Color Navy = new Color32(7, 16, 29, 255);
        private static readonly Color Board = new Color32(12, 29, 45, 255);
        private static readonly Color Grid = new Color32(25, 53, 70, 255);
        private static readonly Color Cyan = new Color32(79, 234, 218, 255);
        private static readonly Color Amber = new Color32(255, 190, 92, 255);
        private static readonly Color Coral = new Color32(255, 103, 99, 255);
        private static readonly Color Ink = new Color32(7, 17, 29, 255);

        private sealed class Enemy
        {
            public GameObject Root;
            public int HitPoints;
            public float ContactCooldown;
            public float Speed;
        }

        private sealed class Projectile
        {
            public GameObject Root;
            public Vector3 Direction;
            public float Age;
        }

        private sealed class RepairCore
        {
            public GameObject Root;
            public float Age;
        }

        private sealed class ArenaEffect
        {
            public GameObject Root;
            public float Age;
            public float Duration;
            public float MaximumScale;
            public float BaseWidth;
        }

        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly List<RepairCore> _repairCores = new List<RepairCore>();
        private readonly List<ArenaEffect> _arenaEffects = new List<ArenaEffect>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly List<Material> _lineMaterials = new List<Material>();

        private TankArenaModel _model;
        private TankArenaService _service;
        private TankArenaLocalizationService _localization;
        private Transform _worldRoot;
        private Transform _player;
        private Transform _playerTurret;
        private TankArenaJoystick _joystick;
        private TankArenaJoystick _aimJoystick;
        private Canvas _canvas;
        private UIAdaptationProfile _adaptationProfile;
        private RectTransform _safeAreaRoot;
        private Text _scoreText;
        private Text _waveText;
        private Text _hullText;
        private Text _objectiveText;
        private Text _fireHintText;
        private Text _phaseText;
        private Text _gameOverTitle;
        private Text _gameOverResult;
        private Text _repairNotice;
        private Image _hullFill;
        private Text _hullPercent;
        private Text _scoreCaption;
        private Text _waveCaption;
        private GameObject _pauseOverlay;
        private GameObject _gameOverOverlay;
        private Button _pauseButton;
        private Button _languageButton;
        private Button _fireModeButton;
        private Sprite _roundedSprite;
        private Sprite _circleSprite;
        private Font _font;
        private float _spawnTimer;
        private float _fireTimer;
        private float _repairNoticeTimer;
        private bool _autoFire = true;
        private Vector3 _turretAimDirection = Vector3.forward;
        private bool _isBound;

        public IReadOnlyArchitecture Architecture => TankArenaArchitecture.Interface;

        public static void Launch()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.Log("[StellarTankArena] Existing game instance is already running.");
                return;
            }

            // The verification Player opens the ordinary framework onboarding scene first.
            // The hot-updated game takes over its presentation once the remote assembly runs.
            Canvas[] oldCanvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < oldCanvases.Length; i++)
            {
                if (oldCanvases[i] != null) oldCanvases[i].enabled = false;
            }
            Camera[] oldCameras = FindObjectsOfType<Camera>(true);
            for (int i = 0; i < oldCameras.Length; i++)
            {
                if (oldCameras[i] != null) oldCameras[i].gameObject.SetActive(false);
            }

            // Hide the Architecture sample's Model/Service/View diagram. Those colored boxes
            // and connecting lines belong to the bootstrap scene, not to this game demo.
            GameObject architectureFlow = GameObject.Find("ArchitectureFlow");
            if (architectureFlow != null) architectureFlow.SetActive(false);

            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;

            TankArenaArchitecture architecture = TankArenaArchitecture.Interface;
            if (architecture.State == ArchitectureState.Uninitialized)
            {
                architecture.Init();
            }

            var root = new GameObject(RootName);
            DontDestroyOnLoad(root);
            root.AddComponent<TankArenaGame>();
            Debug.Log("[StellarTankArena] HotUpdate game launched. marker=TankArenaMainEntered");
        }

        private void Start()
        {
            _model = TankArenaArchitecture.Interface.GetModel<TankArenaModel>();
            _service = TankArenaArchitecture.Interface.GetService<TankArenaService>();
            _localization = TankArenaArchitecture.Interface.GetService<TankArenaLocalizationService>();
            if (_model == null || _service == null || _localization == null)
            {
                LogKit.LogError("[StellarTankArena] Architecture registration is incomplete.");
                return;
            }

            _service.ResetMatch();
            BuildWorld();
            BuildInterface();
            OnBind();
            SpawnEnemy(-2.7f, 5.7f);
            SpawnEnemy(2.5f, 6.6f);
            _spawnTimer = Time.time + 2f;
            LogKit.Log("[StellarTankArena] Match ready. Controls: drag the left pad to move; hold FIRE to engage.");
        }

        public void OnBind()
        {
            if (_isBound || _model == null) return;
            _model.Score.RegisterWithInitValue(RefreshScore).UnRegisterWhenGameObjectDestroyed(gameObject);
            _model.Wave.RegisterWithInitValue(RefreshWave).UnRegisterWhenGameObjectDestroyed(gameObject);
            _model.Hull.RegisterWithInitValue(RefreshHull).UnRegisterWhenGameObjectDestroyed(gameObject);
            _model.Eliminations.RegisterWithInitValue(RefreshResult).UnRegisterWhenGameObjectDestroyed(gameObject);
            _model.Phase.RegisterWithInitValue(OnPhaseChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
            if (_localization.Text != null)
            {
                _localization.Text.LocaleChanged += OnLocaleChanged;
                RefreshLocalizedContent();
            }
            _isBound = true;
        }

        public void OnUnbind()
        {
            if (!_isBound) return;
            if (_localization != null && _localization.Text != null)
                _localization.Text.LocaleChanged -= OnLocaleChanged;
            _isBound = false;
        }

        private void Update()
        {
            if (_model == null || _model.Phase.Value != TankArenaPhase.Active || _player == null) return;

            Vector2 move = _joystick != null ? _joystick.Value : Vector2.zero;
            try
            {
                move += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
            catch (Exception)
            {
                // A project may use the new Input System only; the on-screen controls remain available.
            }
            move = Vector2.ClampMagnitude(move, 1f);

            Vector3 displacement = new Vector3(move.x, 0f, move.y) * (4.25f * Time.deltaTime);
            _player.position += displacement;
            Vector3 playerPosition = _player.position;
            playerPosition.x = Mathf.Clamp(playerPosition.x, -4.15f, 4.15f);
            playerPosition.z = Mathf.Clamp(playerPosition.z, -6.6f, 5.3f);
            _player.position = playerPosition;
            if (move.sqrMagnitude > 0.01f)
                _player.rotation = Quaternion.LookRotation(new Vector3(move.x, 0f, move.y));

            Vector2 aim = _aimJoystick != null ? _aimJoystick.Value : Vector2.zero;
            if (!_autoFire && aim.sqrMagnitude > 0.025f)
                _turretAimDirection = new Vector3(aim.x, 0f, aim.y).normalized;

            bool hasHostile = TryGetNearestHostileDirection(out Vector3 hostileDirection);
            if (_autoFire && hasHostile) _turretAimDirection = hostileDirection;
            if (_turretAimDirection.sqrMagnitude > 0.001f && _playerTurret != null)
                _playerTurret.rotation = Quaternion.LookRotation(_turretAimDirection, Vector3.up);

            bool firing = (_autoFire && hasHostile) ||
                          (!_autoFire && _aimJoystick != null && _aimJoystick.IsHeld);
            try { firing |= Input.GetKey(KeyCode.Space); }
            catch (Exception) { }
            if (firing && Time.time >= _fireTimer)
            {
                FireAlongTurretDirection();
                _fireTimer = Time.time + 0.28f;
            }

            UpdateEnemies();
            UpdateProjectiles();
            UpdateRepairCores();
            UpdateArenaEffects();
            if (_enemies.Count < MaximumEnemies && Time.time >= _spawnTimer)
            {
                SpawnEnemyAtRandomEdge();
                _spawnTimer = Time.time + Mathf.Max(0.8f, 2.3f - _model.Wave.Value * 0.08f);
            }

            if (_repairNoticeTimer > 0f)
            {
                _repairNoticeTimer -= Time.deltaTime;
                if (_repairNoticeTimer <= 0f && _repairNotice != null)
                    _repairNotice.gameObject.SetActive(false);
            }
        }

        private void BuildWorld()
        {
            RenderSettings.ambientLight = new Color(0.52f, 0.62f, 0.73f, 1f);
            var lightObject = new GameObject("ArenaKeyLight", typeof(Light));
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            Light keyLight = lightObject.GetComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.72f, 0.91f, 1f);
            keyLight.intensity = 1.15f;
            keyLight.shadows = LightShadows.None;

            GameObject cameraObject = new GameObject("ArenaCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 25f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Navy;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60f;

            _worldRoot = new GameObject("ArenaWorld").transform;
            _worldRoot.SetParent(transform, false);
            Material floorMaterial = CreateMaterial(Board, 0.15f);
            Material gridMaterial = CreateMaterial(Grid, 0f);
            Material railMaterial = CreateMaterial(new Color32(44, 123, 148, 255), 0.3f);
            Material crateMaterial = CreateMaterial(new Color32(31, 54, 69, 255), 0.18f);
            Material markerMaterial = CreateMaterial(new Color32(24, 72, 87, 255), 0.05f);
            CreatePrimitive("Field", PrimitiveType.Plane, _worldRoot,
                new Vector3(0f, -0.42f, 0f), new Vector3(1.02f, 1f, 1.6f), floorMaterial);

            for (int x = -4; x <= 4; x++)
                CreatePrimitive("GridLine", PrimitiveType.Cube, _worldRoot,
                    new Vector3(x, -0.39f, 0f), new Vector3(0.018f, 0.01f, 15.4f), gridMaterial);
            for (int z = -7; z <= 7; z++)
                CreatePrimitive("GridLine", PrimitiveType.Cube, _worldRoot,
                    new Vector3(0f, -0.39f, z), new Vector3(9.7f, 0.01f, 0.018f), gridMaterial);

            CreatePrimitive("Rail", PrimitiveType.Cube, _worldRoot,
                new Vector3(-4.9f, -0.15f, 0f), new Vector3(0.12f, 0.36f, 15.8f), railMaterial);
            CreatePrimitive("Rail", PrimitiveType.Cube, _worldRoot,
                new Vector3(4.9f, -0.15f, 0f), new Vector3(0.12f, 0.36f, 15.8f), railMaterial);
            CreatePrimitive("Rail", PrimitiveType.Cube, _worldRoot,
                new Vector3(0f, -0.15f, 7.9f), new Vector3(9.9f, 0.36f, 0.12f), railMaterial);
            CreatePrimitive("Rail", PrimitiveType.Cube, _worldRoot,
                new Vector3(0f, -0.15f, -7.9f), new Vector3(9.9f, 0.36f, 0.12f), railMaterial);

            CreateCrate(new Vector3(-2.7f, 0f, 1.6f), crateMaterial, railMaterial);
            CreateCrate(new Vector3(2.6f, 0f, 3.2f), crateMaterial, railMaterial);
            CreateCrate(new Vector3(-2.8f, 0f, -1.9f), crateMaterial, railMaterial);
            CreateCrate(new Vector3(2.9f, 0f, -3.4f), crateMaterial, railMaterial);

            CreateArenaMarker(new Vector3(-4.48f, -0.36f, 6.9f), markerMaterial);
            CreateArenaMarker(new Vector3(4.48f, -0.36f, 6.9f), markerMaterial);
            CreateArenaMarker(new Vector3(-4.48f, -0.36f, -6.9f), markerMaterial);
            CreateArenaMarker(new Vector3(4.48f, -0.36f, -6.9f), markerMaterial);

            _player = CreateTank("PlayerTank", new Color32(54, 219, 205, 255),
                new Color32(177, 255, 241, 255), new Vector3(0f, 0f, -4.8f));
            _playerTurret = _player.Find("TurretPivot");
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            GameObject canvasObject = new GameObject("TankArenaHUD",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform fullScreen = canvasObject.GetComponent<RectTransform>();
            _safeAreaRoot = CreateRect("SafeAreaRoot", fullScreen,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var profile = ScriptableObject.CreateInstance<UIAdaptationProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            var breakpoints = new[]
            {
                new UIAdaptationBreakpoint(),
                new UIAdaptationBreakpoint()
            };
            breakpoints[0].Configure("Portrait", 1f, 4f, UIAdaptationOrientation.Portrait, 1f);
            breakpoints[1].Configure("Landscape", 1f, 4f, UIAdaptationOrientation.Landscape, 0f);
            profile.Configure(new Vector2(1080f, 1920f), 0.5f, true, breakpoints);
            _adaptationProfile = profile;
            UIAdaptationController controller = canvasObject.AddComponent<UIAdaptationController>();
            controller.DisplayGeometryChanged += OnDisplayGeometryChanged;
            controller.Configure(profile, _safeAreaRoot);
            controller.ApplyCurrentScreen();

            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _roundedSprite = CreateRoundedSprite(256, 256, 0.12f);
            _circleSprite = CreateRoundedSprite(256, 256, 0.5f);

            CreateImage("ArenaHUDTint", _safeAreaRoot, Vector2.zero, Vector2.one,
                _roundedSprite, new Color(0.02f, 0.045f, 0.075f, 0.19f));

            _languageButton = CreateButton("Language", _safeAreaRoot,
                new Vector2(0.70f, 0.925f), new Vector2(0.79f, 0.977f),
                _roundedSprite, new Color32(21, 42, 57, 240), "EN", 20,
                new Color32(198, 222, 232, 255), OnToggleLanguage);
            _fireModeButton = CreateButton("FireModeButton", _safeAreaRoot,
                new Vector2(0.80f, 0.925f), new Vector2(0.92f, 0.977f),
                _roundedSprite, new Color32(142, 86, 42, 245), "AUTO", 17,
                new Color32(255, 230, 190, 255), OnToggleFireMode);
            _pauseButton = CreateButton("Pause", _safeAreaRoot,
                new Vector2(0.925f, 0.925f), new Vector2(0.985f, 0.977f),
                _circleSprite, new Color32(21, 42, 57, 240), "Ⅱ", 30,
                Color.white, OnPausePressed);

            CreateText("GameTitle", _safeAreaRoot,
                new Vector2(0.045f, 0.922f), new Vector2(0.685f, 0.972f),
                string.Empty, 39, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            CreateText("GameSubtitle", _safeAreaRoot,
                new Vector2(0.05f, 0.894f), new Vector2(0.685f, 0.925f),
                string.Empty, 18, new Color32(127, 165, 185, 255), TextAnchor.MiddleLeft, FontStyle.Normal);

            CreateMetricCard("ScoreCard", _safeAreaRoot, new Vector2(0.045f, 0.812f),
                new Vector2(0.47f, 0.88f), "SCORE", out _scoreCaption, out _scoreText);
            CreateMetricCard("WaveCard", _safeAreaRoot, new Vector2(0.53f, 0.812f),
                new Vector2(0.955f, 0.88f), "WAVE", out _waveCaption, out _waveText);

            _hullText = CreateText("HullLabel", _safeAreaRoot,
                new Vector2(0.05f, 0.778f), new Vector2(0.33f, 0.812f),
                string.Empty, 17, new Color32(177, 204, 217, 255), TextAnchor.MiddleLeft, FontStyle.Bold);
            _hullPercent = CreateText("HullPercent", _safeAreaRoot,
                new Vector2(0.68f, 0.778f), new Vector2(0.95f, 0.812f),
                string.Empty, 17, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            CreateImage("HullTrack", _safeAreaRoot,
                new Vector2(0.05f, 0.765f), new Vector2(0.95f, 0.778f),
                _roundedSprite, new Color32(33, 55, 70, 255));
            _hullFill = CreateImage("HullFill", _safeAreaRoot,
                new Vector2(0.05f, 0.765f), new Vector2(0.95f, 0.778f),
                _roundedSprite, Cyan);
            RectTransform hullFillRect = (RectTransform)_hullFill.transform;
            hullFillRect.anchorMax = new Vector2(0.95f, 0.778f);
            hullFillRect.pivot = new Vector2(0f, 0.5f);
            _hullFill.rectTransform.anchorMin = new Vector2(0.05f, 0.765f);
            _hullFill.rectTransform.anchorMax = new Vector2(0.95f, 0.778f);
            _hullFill.rectTransform.offsetMin = Vector2.zero;
            _hullFill.rectTransform.offsetMax = Vector2.zero;
            _hullFill.rectTransform.localScale = Vector3.one;

            _objectiveText = CreateText("Objective", _safeAreaRoot,
                new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.756f),
                string.Empty, 16, new Color32(111, 179, 190, 255), TextAnchor.MiddleCenter, FontStyle.Bold);
            _phaseText = CreateText("PhaseNotice", _safeAreaRoot,
                new Vector2(0.25f, 0.62f), new Vector2(0.75f, 0.68f),
                "SECTOR STABLE", 20, new Color32(178, 223, 228, 210), TextAnchor.MiddleCenter, FontStyle.Bold);

            CreateText("MoveHint", _safeAreaRoot,
                new Vector2(0.055f, 0.175f), new Vector2(0.39f, 0.213f),
                string.Empty, 16, new Color32(152, 184, 197, 255), TextAnchor.MiddleCenter, FontStyle.Bold);
            _fireHintText = CreateText("FireHint", _safeAreaRoot,
                new Vector2(0.63f, 0.175f), new Vector2(0.96f, 0.213f),
                string.Empty, 14, new Color32(152, 184, 197, 255), TextAnchor.MiddleCenter, FontStyle.Bold);

            Image joystickBase = CreateImage("MovePad", _safeAreaRoot,
                new Vector2(0.075f, 0.035f), new Vector2(0.355f, 0.17f),
                _circleSprite, new Color32(25, 48, 64, 215));
            joystickBase.raycastTarget = true;
            _joystick = joystickBase.gameObject.AddComponent<TankArenaJoystick>();
            _joystick.Initialize(joystickBase.rectTransform);
            Image joystickRing = CreateImage("MovePadRing", joystickBase.transform,
                new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f),
                _circleSprite, new Color32(67, 106, 123, 95));
            Image joystickKnob = CreateImage("MovePadKnob", joystickBase.transform,
                new Vector2(0.34f, 0.34f), new Vector2(0.66f, 0.66f),
                _circleSprite, new Color32(119, 168, 183, 235));
            _joystick.SetVisuals(joystickRing.rectTransform, joystickKnob.rectTransform);

            Image aimBase = CreateImage("AimPad", _safeAreaRoot,
                new Vector2(0.695f, 0.045f), new Vector2(0.94f, 0.17f),
                _circleSprite, new Color32(25, 48, 64, 215));
            aimBase.raycastTarget = true;
            _aimJoystick = aimBase.gameObject.AddComponent<TankArenaJoystick>();
            _aimJoystick.Initialize(aimBase.rectTransform);
            Image aimRing = CreateImage("AimPadRing", aimBase.transform,
                new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f),
                _circleSprite, new Color32(67, 106, 123, 95));
            Image aimKnob = CreateImage("AimPadKnob", aimBase.transform,
                new Vector2(0.34f, 0.34f), new Vector2(0.66f, 0.66f),
                _circleSprite, new Color32(255, 190, 92, 235));
            _aimJoystick.SetVisuals(aimRing.rectTransform, aimKnob.rectTransform);
            _repairNotice = CreateText("RepairNotice", _safeAreaRoot,
                new Vector2(0.25f, 0.245f), new Vector2(0.75f, 0.29f),
                string.Empty, 21, Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
            _repairNotice.gameObject.SetActive(false);

            CreateOverlayPanels(_safeAreaRoot);
            OnLocaleChanged(this, null);
            RefreshScore(_model.Score.Value);
            RefreshWave(_model.Wave.Value);
            RefreshHull(_model.Hull.Value);

            ActionKit.Sequence(gameObject)
                .ScaleTo(_pauseButton.transform, Vector3.one, 0.28f, Ease.OutBack)
                .Start();
        }

        private void CreateOverlayPanels(RectTransform parent)
        {
            _pauseOverlay = CreateOverlay("PauseOverlay", parent);
            CreateModalCard("PauseCard", _pauseOverlay.transform, new Vector2(0.08f, 0.28f),
                new Vector2(0.92f, 0.72f), new Color32(79, 234, 218, 255));
            CreateText("PausedTitle", _pauseOverlay.transform,
                new Vector2(0.15f, 0.59f), new Vector2(0.85f, 0.69f),
                string.Empty, 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateButton("ResumeButton", _pauseOverlay.transform,
                new Vector2(0.28f, 0.44f), new Vector2(0.72f, 0.54f),
                _roundedSprite, new Color32(61, 198, 185, 255), "RESUME", 20,
                Ink, OnResumePressed);
            CreateButton("PauseRestartButton", _pauseOverlay.transform,
                new Vector2(0.28f, 0.31f), new Vector2(0.72f, 0.41f),
                _roundedSprite, new Color32(38, 65, 81, 255), "REDEPLOY", 18,
                Color.white, RestartMatch);
            _pauseOverlay.SetActive(false);

            _gameOverOverlay = CreateOverlay("GameOverOverlay", parent);
            CreateModalCard("GameOverCard", _gameOverOverlay.transform, new Vector2(0.08f, 0.29f),
                new Vector2(0.92f, 0.71f), new Color32(255, 190, 92, 255));
            _gameOverTitle = CreateText("GameOverTitle", _gameOverOverlay.transform,
                new Vector2(0.13f, 0.58f), new Vector2(0.87f, 0.68f),
                string.Empty, 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _gameOverResult = CreateText("GameOverResult", _gameOverOverlay.transform,
                new Vector2(0.12f, 0.48f), new Vector2(0.88f, 0.55f),
                string.Empty, 17, new Color32(168, 202, 214, 255), TextAnchor.MiddleCenter, FontStyle.Normal);
            CreateButton("GameOverRestartButton", _gameOverOverlay.transform,
                new Vector2(0.28f, 0.32f), new Vector2(0.72f, 0.42f),
                _roundedSprite, new Color32(255, 190, 92, 255), "DEPLOY AGAIN", 19,
                Ink, RestartMatch);
            _gameOverOverlay.SetActive(false);
        }

        private void CreateModalCard(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color accent)
        {
            CreateImage(name + "Border", parent, anchorMin, anchorMax, _roundedSprite, accent);
            Vector2 inset = new Vector2(0.008f, 0.009f);
            CreateImage(name + "Surface", parent, anchorMin + inset, anchorMax - inset,
                _roundedSprite, new Color32(10, 25, 38, 255));
            CreateImage(name + "TopRule", parent,
                new Vector2(anchorMin.x + 0.04f, anchorMax.y - 0.018f),
                new Vector2(anchorMax.x - 0.04f, anchorMax.y - 0.012f),
                _roundedSprite, new Color(accent.r, accent.g, accent.b, 0.8f));
        }

        private GameObject CreateOverlay(string name, RectTransform parent)
        {
            var overlay = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)overlay.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = overlay.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color(0.015f, 0.035f, 0.06f, 0.91f);
            return overlay;
        }

        private void UpdateEnemies()
        {
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _enemies[i];
                if (enemy.Root == null)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }

                Vector3 direction = _player.position - enemy.Root.transform.position;
                direction.y = 0f;
                float distance = direction.magnitude;
                if (distance > 1.1f)
                {
                    direction.Normalize();
                    enemy.Root.transform.position += direction * enemy.Speed * Time.deltaTime;
                    enemy.Root.transform.rotation = Quaternion.LookRotation(direction);
                }
                else
                {
                    enemy.ContactCooldown -= Time.deltaTime;
                    if (enemy.ContactCooldown <= 0f)
                    {
                        _service.ApplyDamage(10);
                        enemy.ContactCooldown = 1.05f;
                        if (_model.Hull.Value <= 0) return;
                    }
                }
            }
        }

        private void UpdateProjectiles()
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = _projectiles[i];
                if (projectile.Root == null)
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                projectile.Age += Time.deltaTime;
                projectile.Root.transform.position += projectile.Direction * (13.5f * Time.deltaTime);
                bool consumed = false;
                for (int enemyIndex = _enemies.Count - 1; enemyIndex >= 0; enemyIndex--)
                {
                    Enemy enemy = _enemies[enemyIndex];
                    if (enemy.Root == null ||
                        FlatDistance(projectile.Root.transform.position, enemy.Root.transform.position) > 0.7f)
                        continue;

                    enemy.HitPoints--;
                    Vector3 hitPosition = enemy.Root.transform.position;
                    if (enemy.HitPoints <= 0)
                    {
                        Vector3 destroyedAt = hitPosition;
                        Destroy(enemy.Root);
                        _enemies.RemoveAt(enemyIndex);
                        _service.RegisterElimination();
                        SpawnRingEffect(destroyedAt, Amber, 1.35f, 0.34f);
                        SpawnRingEffect(destroyedAt, new Color32(255, 112, 87, 255), 0.82f, 0.22f);
                        if (_model.Eliminations.Value % 4 == 0) SpawnRepairCore(destroyedAt);
                        LogKit.Log($"[StellarTankArena] Hostile destroyed. eliminations={_model.Eliminations.Value}");
                    }
                    else
                    {
                        enemy.Root.transform.localScale *= 0.92f;
                        SpawnRingEffect(hitPosition, new Color32(255, 222, 163, 255), 0.62f, 0.18f);
                    }
                    consumed = true;
                    break;
                }

                if (consumed || projectile.Age > 1.7f ||
                    Mathf.Abs(projectile.Root.transform.position.x) > 5f ||
                    Mathf.Abs(projectile.Root.transform.position.z) > 8f)
                {
                    Destroy(projectile.Root);
                    _projectiles.RemoveAt(i);
                }
            }
        }

        private void UpdateRepairCores()
        {
            for (int i = _repairCores.Count - 1; i >= 0; i--)
            {
                RepairCore core = _repairCores[i];
                if (core.Root == null)
                {
                    _repairCores.RemoveAt(i);
                    continue;
                }
                core.Age += Time.deltaTime;
                core.Root.transform.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
                core.Root.transform.position = new Vector3(
                    core.Root.transform.position.x,
                    0.4f + Mathf.Sin(core.Age * 3f) * 0.08f,
                    core.Root.transform.position.z);
                if (FlatDistance(_player.position, core.Root.transform.position) > 0.8f) continue;

                Destroy(core.Root);
                _repairCores.RemoveAt(i);
                _service.RepairHull(25);
                _repairNotice.text = _localization.Get("tank.pickup");
                _repairNotice.gameObject.SetActive(true);
                _repairNoticeTimer = 2f;
                ActionKit.Sequence(gameObject)
                    .ScaleTo(_repairNotice.transform, Vector3.one * 1.12f, 0.15f, Ease.OutBack)
                    .ScaleTo(_repairNotice.transform, Vector3.one, 0.16f)
                    .Start();
            }
        }

        private bool TryGetNearestHostileDirection(out Vector3 direction)
        {
            direction = _turretAimDirection;
            if (_enemies.Count == 0) return false;
            Enemy closest = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].Root == null) continue;
                float distance = FlatDistance(_player.position, _enemies[i].Root.transform.position);
                if (distance < closestDistance)
                {
                    closest = _enemies[i];
                    closestDistance = distance;
                }
            }
            if (closest == null) return false;

            direction = closest.Root.transform.position - _player.position;
            direction.y = 0f;
            direction.Normalize();
            return true;
        }

        private void FireAlongTurretDirection()
        {
            Vector3 direction = _turretAimDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            direction.Normalize();
            Vector3 muzzle = _playerTurret != null
                ? _playerTurret.position + direction * 1.04f + Vector3.up * 0.03f
                : _player.position + direction * 0.9f + Vector3.up * 0.36f;
            GameObject shot = CreatePrimitive("PlasmaRound", PrimitiveType.Capsule,
                _worldRoot, muzzle, new Vector3(0.12f, 0.18f, 0.12f), GetMaterial(Amber));
            shot.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
            _projectiles.Add(new Projectile { Root = shot, Direction = direction });
            SpawnRingEffect(muzzle, new Color32(255, 226, 162, 255), 0.42f, 0.13f);
        }

        private void SpawnEnemyAtRandomEdge()
        {
            float x = UnityEngine.Random.Range(-3.6f, 3.6f);
            float z = UnityEngine.Random.value > 0.5f
                ? UnityEngine.Random.Range(5.7f, 7f)
                : UnityEngine.Random.Range(-6.5f, -5.8f);
            SpawnEnemy(x, z);
        }

        private void SpawnEnemy(float x, float z)
        {
            bool heavy = _model.Eliminations.Value >= 5 && UnityEngine.Random.value > 0.68f;
            Color bodyColor = heavy ? new Color32(174, 91, 71, 255) : Coral;
            Color accent = heavy ? new Color32(255, 190, 92, 255) : new Color32(255, 183, 151, 255);
            Transform root = CreateTank("HostileTank", bodyColor, accent, new Vector3(x, 0f, z));
            _enemies.Add(new Enemy
            {
                Root = root.gameObject,
                HitPoints = heavy ? 3 : 2,
                Speed = heavy ? 0.95f : 1.25f + Mathf.Min(0.5f, _model.Wave.Value * 0.035f)
            });
        }

        private void SpawnRepairCore(Vector3 position)
        {
            var core = new GameObject("RepairCore");
            core.transform.SetParent(_worldRoot, false);
            core.transform.position = new Vector3(position.x, 0.22f, position.z);
            CreatePrimitive("CoreBase", PrimitiveType.Cylinder, core.transform,
                new Vector3(0f, 0f, 0f), new Vector3(0.36f, 0.08f, 0.36f), GetMaterial(new Color32(25, 81, 93, 255)));
            CreatePrimitive("CoreCell", PrimitiveType.Cylinder, core.transform,
                new Vector3(0f, 0.16f, 0f), new Vector3(0.23f, 0.16f, 0.23f), GetMaterial(Cyan));
            CreatePrimitive("CoreCap", PrimitiveType.Sphere, core.transform,
                new Vector3(0f, 0.34f, 0f), new Vector3(0.16f, 0.08f, 0.16f), GetMaterial(new Color32(207, 255, 246, 255)));
            CreatePrimitive("CoreBand", PrimitiveType.Cube, core.transform,
                new Vector3(0f, 0.16f, 0f), new Vector3(0.48f, 0.035f, 0.08f), GetMaterial(Amber));
            _repairCores.Add(new RepairCore { Root = core });
        }

        private Transform CreateTank(string name, Color body, Color accent, Vector3 position)
        {
            var tank = new GameObject(name);
            tank.transform.SetParent(_worldRoot, false);
            tank.transform.position = position;
            Material bodyMaterial = GetMaterial(body);
            Material accentMaterial = GetMaterial(accent);
            Material treadMaterial = GetMaterial(new Color32(21, 36, 47, 255));

            Material trackPadMaterial = GetMaterial(new Color32(83, 103, 113, 255));
            bool playerTank = string.Equals(name, "PlayerTank", StringComparison.Ordinal);
            int wheelCount = playerTank ? 4 : 3;
            int padCount = playerTank ? 4 : 2;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.45f;
                GameObject track = CreatePrimitive(side < 0 ? "LeftTrack" : "RightTrack", PrimitiveType.Capsule,
                    tank.transform, new Vector3(x, 0.17f, 0f), new Vector3(0.22f, 0.56f, 0.22f), treadMaterial);
                track.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                for (int wheel = 0; wheel < wheelCount; wheel++)
                {
                    float z = Mathf.Lerp(-0.38f, 0.38f, wheel / (float)(wheelCount - 1));
                    CreatePrimitive("RoadWheel", PrimitiveType.Cylinder, tank.transform,
                        new Vector3(x, 0.265f, z), new Vector3(0.085f, 0.032f, 0.085f), trackPadMaterial);
                }

                for (int pad = 0; pad < padCount; pad++)
                {
                    float z = Mathf.Lerp(-0.45f, 0.45f, pad / (float)(padCount - 1));
                    CreatePrimitive("TrackPad", PrimitiveType.Cube, tank.transform,
                        new Vector3(x, 0.285f, z), new Vector3(0.18f, 0.025f, 0.065f), treadMaterial);
                }
            }

            GameObject hull = CreatePrimitive("ArmoredHull", PrimitiveType.Capsule,
                tank.transform, new Vector3(0f, 0.34f, 0f), new Vector3(0.80f, 0.55f, 0.36f), bodyMaterial);
            hull.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject deck = CreatePrimitive("UpperDeck", PrimitiveType.Capsule,
                tank.transform, new Vector3(0f, 0.48f, -0.02f), new Vector3(0.58f, 0.25f, 0.12f), accentMaterial);
            deck.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive("FrontArmor", PrimitiveType.Cube, tank.transform,
                new Vector3(0f, 0.38f, 0.47f), new Vector3(0.5f, 0.16f, 0.18f), bodyMaterial);
            CreatePrimitive("LeftHeadlight", PrimitiveType.Sphere, tank.transform,
                new Vector3(-0.22f, 0.39f, 0.57f), new Vector3(0.08f, 0.07f, 0.06f), GetMaterial(new Color32(255, 216, 138, 255)));
            CreatePrimitive("RightHeadlight", PrimitiveType.Sphere, tank.transform,
                new Vector3(0.22f, 0.39f, 0.57f), new Vector3(0.08f, 0.07f, 0.06f), GetMaterial(new Color32(255, 216, 138, 255)));
            CreatePrimitive("TurretRing", PrimitiveType.Cylinder, tank.transform,
                new Vector3(0f, 0.535f, 0f), new Vector3(0.38f, 0.045f, 0.38f), treadMaterial);
            var turretPivot = new GameObject("TurretPivot").transform;
            turretPivot.SetParent(tank.transform, false);
            turretPivot.localPosition = new Vector3(0f, 0.59f, 0.02f);
            CreatePrimitive("Turret", PrimitiveType.Cylinder, turretPivot,
                new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.10f, 0.34f), accentMaterial);
            CreatePrimitive("TurretMantlet", PrimitiveType.Cube, turretPivot,
                new Vector3(0f, 0.025f, 0.24f), new Vector3(0.25f, 0.13f, 0.22f), bodyMaterial);
            GameObject barrel = CreatePrimitive("Barrel", PrimitiveType.Cylinder, turretPivot,
                new Vector3(0f, 0.045f, 0.56f), new Vector3(0.075f, 0.38f, 0.075f), bodyMaterial);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive("Muzzle", PrimitiveType.Cylinder, turretPivot,
                new Vector3(0f, 0.045f, 0.96f), new Vector3(0.105f, 0.07f, 0.105f), treadMaterial);
            CreatePrimitive("TurretHatch", PrimitiveType.Cylinder, turretPivot,
                new Vector3(0f, 0.105f, -0.08f), new Vector3(0.13f, 0.035f, 0.13f), bodyMaterial);
            CreatePrimitive("CommandLight", PrimitiveType.Sphere, turretPivot,
                new Vector3(0f, 0.155f, -0.08f), new Vector3(0.09f, 0.05f, 0.09f), GetMaterial(new Color32(211, 255, 246, 255)));
            return tank.transform;
        }

        private void CreateCrate(Vector3 position, Material body, Material accent)
        {
            var crate = new GameObject("SupplyCrate");
            crate.transform.SetParent(_worldRoot, false);
            crate.transform.position = position;
            crate.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-12f, 12f), 0f);
            CreatePrimitive("CrateBody", PrimitiveType.Cube, crate.transform,
                new Vector3(0f, 0.14f, 0f), new Vector3(0.94f, 0.28f, 0.86f), body);
            CreatePrimitive("CrateLid", PrimitiveType.Cube, crate.transform,
                new Vector3(0f, 0.30f, 0f), new Vector3(0.78f, 0.035f, 0.69f), GetMaterial(new Color32(49, 80, 94, 255)));
            CreatePrimitive("CrateRail", PrimitiveType.Cube, crate.transform,
                new Vector3(0f, 0.33f, 0f), new Vector3(0.085f, 0.025f, 0.72f), accent);
            CreatePrimitive("CrateLatch", PrimitiveType.Cube, crate.transform,
                new Vector3(0f, 0.34f, 0.21f), new Vector3(0.22f, 0.04f, 0.1f), GetMaterial(Amber));
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    CreatePrimitive("CrateCorner", PrimitiveType.Cylinder, crate.transform,
                        new Vector3(x * 0.39f, 0.30f, z * 0.34f), new Vector3(0.075f, 0.035f, 0.075f), accent);
                }
            }
        }

        private void CreateArenaMarker(Vector3 position, Material material)
        {
            CreatePrimitive("SectorMarker", PrimitiveType.Cube, _worldRoot,
                position, new Vector3(0.62f, 0.012f, 0.045f), material);
            CreatePrimitive("SectorMarker", PrimitiveType.Cube, _worldRoot,
                position, new Vector3(0.045f, 0.012f, 0.62f), material);
        }

        private void SpawnRingEffect(Vector3 position, Color color, float maximumScale, float duration)
        {
            var effectObject = new GameObject("CombatShockRing", typeof(LineRenderer));
            effectObject.transform.SetParent(_worldRoot, false);
            effectObject.transform.position = new Vector3(position.x, 0.015f, position.z);
            effectObject.transform.localScale = Vector3.one * 0.08f;

            LineRenderer line = effectObject.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 25;
            line.widthMultiplier = 0.055f;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = GetLineMaterial(color);
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / (line.positionCount - 1);
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }

            _arenaEffects.Add(new ArenaEffect
            {
                Root = effectObject,
                Duration = Mathf.Max(0.05f, duration),
                MaximumScale = Mathf.Max(0.1f, maximumScale),
                BaseWidth = line.widthMultiplier
            });
        }

        private void UpdateArenaEffects()
        {
            for (int i = _arenaEffects.Count - 1; i >= 0; i--)
            {
                ArenaEffect effect = _arenaEffects[i];
                if (effect.Root == null)
                {
                    _arenaEffects.RemoveAt(i);
                    continue;
                }

                effect.Age += Time.deltaTime;
                float progress = Mathf.Clamp01(effect.Age / effect.Duration);
                float eased = 1f - Mathf.Pow(1f - progress, 2f);
                effect.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.08f, effect.MaximumScale, eased);
                LineRenderer line = effect.Root.GetComponent<LineRenderer>();
                if (line != null) line.widthMultiplier = Mathf.Lerp(effect.BaseWidth, effect.BaseWidth * 0.35f, progress);
                if (progress < 1f) continue;

                Destroy(effect.Root);
                _arenaEffects.RemoveAt(i);
            }
        }

        private GameObject CreatePrimitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            Collider collider = instance.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return instance;
        }

        private void CreateMetricCard(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string initialLabel,
            out Text captionText,
            out Text valueText)
        {
            CreateImage(name, parent, anchorMin, anchorMax,
                _roundedSprite, new Color32(14, 32, 48, 234));
            captionText = CreateText(name + "Caption", parent,
                new Vector2(anchorMin.x + 0.03f, anchorMin.y + 0.006f),
                new Vector2(anchorMax.x - 0.02f, anchorMin.y + 0.034f), initialLabel,
                15, new Color32(125, 165, 182, 255), TextAnchor.MiddleLeft, FontStyle.Bold);
            valueText = CreateText(name + "Value", parent,
                new Vector2(anchorMin.x + 0.03f, anchorMin.y + 0.03f),
                new Vector2(anchorMax.x - 0.02f, anchorMax.y - 0.002f), "00000",
                34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        private Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite sprite,
            Color background,
            string label,
            int fontSize,
            Color labelColor,
            Action clicked)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = background;
            image.type = Image.Type.Sliced;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.16f);
            colors.selectedColor = background;
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.5f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (clicked != null) button.onClick.AddListener(() => clicked());

            Text text = CreateText(name + "Label", rect, Vector2.zero, Vector2.one,
                label, fontSize, labelColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.raycastTarget = false;
            return button;
        }

        private Text CreateText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment,
            FontStyle style,
            out Text text)
        {
            text = CreateText(name, parent, anchorMin, anchorMax, value, fontSize, color, alignment, style);
            return text;
        }

        private Text CreateText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment,
            FontStyle style)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Image CreateImage(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite sprite,
            Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)imageObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite != null && sprite == _roundedSprite) image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)rectObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static Sprite CreateRoundedSprite(int size, int resolution, float radius)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "TankArenaRoundedSprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[resolution * resolution];
            float corner = (resolution - 1) * radius;
            float left = corner;
            float right = resolution - 1 - corner;
            float bottom = corner;
            float top = resolution - 1 - corner;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nearestX = Mathf.Clamp(x, left, right);
                    float nearestY = Mathf.Clamp(y, bottom, top);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    byte alpha = (byte)(Mathf.Clamp01(corner + 0.5f - distance) * 255f);
                    pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect,
                new Vector4(corner, corner, corner, corner));
        }

        private Material CreateMaterial(Color color, float smoothness)
        {
            string shaderName = "Universal Render Pipeline/Lit";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.12f);
            _materials.Add(material);
            return material;
        }

        private Material GetMaterial(Color color)
        {
            for (int i = 0; i < _materials.Count; i++)
            {
                if (ColorDistance(_materials[i].color, color) < 0.01f) return _materials[i];
            }
            return CreateMaterial(color, 0.32f);
        }

        private Material GetLineMaterial(Color color)
        {
            for (int i = 0; i < _lineMaterials.Count; i++)
            {
                if (ColorDistance(_lineMaterials[i].color, color) < 0.01f) return _lineMaterials[i];
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return GetMaterial(color);

            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            _lineMaterials.Add(material);
            _materials.Add(material);
            return material;
        }

        private static float ColorDistance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private void OnDisplayGeometryChanged(UIDisplayGeometry geometry)
        {
            float width = Mathf.Max(1, geometry.Width);
            float height = Mathf.Max(1, geometry.Height);
            Vector2 expectedMin = new Vector2(geometry.SafeArea.xMin / width, geometry.SafeArea.yMin / height);
            Vector2 expectedMax = new Vector2(geometry.SafeArea.xMax / width, geometry.SafeArea.yMax / height);
            bool anchorsMatch = _safeAreaRoot != null &&
                Vector2.Distance(_safeAreaRoot.anchorMin, expectedMin) <= 0.005f &&
                Vector2.Distance(_safeAreaRoot.anchorMax, expectedMax) <= 0.005f;
            string message = $"[StellarTankArena][UIAdaptation] result={(anchorsMatch ? "PASS" : "FAIL")}, " +
                             $"resolution={geometry.Width}x{geometry.Height}, safeArea={geometry.SafeArea}, " +
                             $"insets={geometry.HasSafeAreaInsets}, breakpoint={_canvas.GetComponent<UIAdaptationController>().CurrentBreakpointId}";
            if (anchorsMatch) Debug.Log(message);
            else Debug.LogError(message);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var eventSystem = new GameObject("StellarTankArenaEventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform, false);
        }

        private void RefreshScore(int score)
        {
            if (_scoreText != null) _scoreText.text = score.ToString("00000", CultureInfo.InvariantCulture);
            RefreshResult(_model != null ? _model.Eliminations.Value : 0);
        }

        private void RefreshWave(int wave)
        {
            if (_waveText != null) _waveText.text = wave.ToString("00", CultureInfo.InvariantCulture);
        }

        private void RefreshHull(int hull)
        {
            if (_hullPercent != null) _hullPercent.text = hull.ToString("000", CultureInfo.InvariantCulture) + "%";
            if (_hullFill != null)
            {
                RectTransform rect = _hullFill.rectTransform;
                float minX = 0.05f;
                float maxX = Mathf.Lerp(minX, 0.95f, hull / 100f);
                rect.anchorMin = new Vector2(minX, 0.765f);
                rect.anchorMax = new Vector2(maxX, 0.778f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _hullFill.color = hull > 35 ? Cyan : Coral;
            }
        }

        private void RefreshResult(int eliminations)
        {
            if (_gameOverResult == null || _localization == null) return;
            var arguments = new[]
            {
                new LocalizationFormatArgument("score", _model.Score.Value.ToString("N0", CultureInfo.InvariantCulture)),
                new LocalizationFormatArgument("kills", eliminations.ToString(CultureInfo.InvariantCulture))
            };
            if (_localization.Text.TryFormat(LocalizationKey.From("tank.result"), arguments,
                    out string formatted, out string error))
            {
                _gameOverResult.text = formatted;
            }
            else
            {
                LogKit.LogError("[StellarTankArena] Could not format the result label: " + error);
            }
        }

        private void RefreshLocalizedContent()
        {
            if (_localization == null || _localization.Text == null) return;
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                switch (texts[i].name)
                {
                    case "GameTitle": texts[i].text = _localization.Get("tank.title"); break;
                    case "GameSubtitle": texts[i].text = _localization.Get("tank.subtitle"); break;
                    case "Objective": texts[i].text = _localization.Get("tank.objective"); break;
                    case "MoveHint": texts[i].text = _localization.Get("tank.move"); break;
                    case "FireHint":
                        texts[i].text = _localization.Get(_autoFire ? "tank.fire_hint_auto" : "tank.fire_hint_manual");
                        break;
                    case "FireModeButtonLabel":
                        texts[i].text = _localization.Get(_autoFire ? "tank.mode_auto" : "tank.mode_manual");
                        break;
                    case "PausedTitle": texts[i].text = _localization.Get("tank.paused"); break;
                    case "GameOverTitle": texts[i].text = _localization.Get("tank.game_over"); break;
                    case "PhaseNotice": texts[i].text = _localization.Get("tank.phase_ready"); break;
                    case "GameOverRestartButtonLabel":
                    case "PauseRestartButtonLabel": texts[i].text = _localization.Get("tank.restart"); break;
                    case "ResumeButtonLabel": texts[i].text = _localization.Get("tank.resume"); break;
                    case "HullLabel": texts[i].text = _localization.Get("tank.hull"); break;
                    case "RepairNotice":
                        if (_repairNotice != null && _repairNotice.gameObject.activeSelf)
                            texts[i].text = _localization.Get("tank.pickup");
                        break;
                }
            }

            if (_scoreCaption != null) _scoreCaption.text = _localization.Get("tank.score");
            if (_waveCaption != null) _waveCaption.text = _localization.Get("tank.wave");
            if (_hullText != null) _hullText.text = _localization.Get("tank.hull");
            if (_languageButton != null)
            {
                Text languageLabel = _languageButton.GetComponentInChildren<Text>(true);
                if (languageLabel != null) languageLabel.text = _localization.Text.CurrentLocale.Value == "en-US" ? "EN" : "中";
            }
            if (_fireModeButton != null)
            {
                Text modeLabel = _fireModeButton.GetComponentInChildren<Text>(true);
                if (modeLabel != null)
                    modeLabel.text = _localization.Get(_autoFire ? "tank.mode_auto" : "tank.mode_manual");
            }
            RefreshResult(_model.Eliminations.Value);
        }

        private void OnLocaleChanged(object sender, LocalizationChangedEventArgs args)
        {
            RefreshLocalizedContent();
        }

        private void OnToggleLanguage()
        {
            _localization.ToggleLanguage();
            ActionKit.Sequence(gameObject)
                .ScaleTo(_languageButton.transform, Vector3.one * 1.12f, 0.1f)
                .ScaleTo(_languageButton.transform, Vector3.one, 0.14f, Ease.OutBack)
                .Start();
        }

        private void OnToggleFireMode()
        {
            _autoFire = !_autoFire;
            RefreshLocalizedContent();
            ActionKit.Sequence(gameObject)
                .ScaleTo(_fireModeButton.transform, Vector3.one * 1.08f, 0.08f)
                .ScaleTo(_fireModeButton.transform, Vector3.one, 0.12f, Ease.OutBack)
                .Start();
        }

        private void OnPausePressed()
        {
            if (_model.Phase.Value != TankArenaPhase.Active) return;
            _service.SetPhase(TankArenaPhase.Paused);
            Time.timeScale = 0f;
        }

        private void OnResumePressed()
        {
            Time.timeScale = 1f;
            _service.SetPhase(TankArenaPhase.Active);
        }

        private void OnPhaseChanged(TankArenaPhase phase)
        {
            if (_pauseOverlay != null) _pauseOverlay.SetActive(phase == TankArenaPhase.Paused);
            if (_gameOverOverlay != null) _gameOverOverlay.SetActive(phase == TankArenaPhase.GameOver);
            if (_pauseButton != null) _pauseButton.gameObject.SetActive(phase == TankArenaPhase.Active);
            if (phase == TankArenaPhase.GameOver)
            {
                Time.timeScale = 0f;
                if (_gameOverTitle != null) _gameOverTitle.text = _localization.Get("tank.game_over");
                RefreshResult(_model.Eliminations.Value);
                LogKit.Log($"[StellarTankArena] Match complete. score={_model.Score.Value}, eliminations={_model.Eliminations.Value}");
            }
            else if (phase == TankArenaPhase.Active)
            {
                Time.timeScale = 1f;
            }
        }

        private void RestartMatch()
        {
            Time.timeScale = 1f;
            for (int i = 0; i < _enemies.Count; i++) if (_enemies[i].Root != null) Destroy(_enemies[i].Root);
            for (int i = 0; i < _projectiles.Count; i++) if (_projectiles[i].Root != null) Destroy(_projectiles[i].Root);
            for (int i = 0; i < _repairCores.Count; i++) if (_repairCores[i].Root != null) Destroy(_repairCores[i].Root);
            _enemies.Clear();
            _projectiles.Clear();
            _repairCores.Clear();
            if (_player != null)
            {
                _player.position = new Vector3(0f, 0f, -4.8f);
                _player.rotation = Quaternion.identity;
            }
            _turretAimDirection = Vector3.forward;
            if (_playerTurret != null) _playerTurret.localRotation = Quaternion.identity;
            _service.ResetMatch();
            _spawnTimer = Time.time + 2f;
            _fireTimer = 0f;
            SpawnEnemy(-2.7f, 5.7f);
            SpawnEnemy(2.5f, 6.6f);
        }

        private void OnDestroy()
        {
            OnUnbind();
            Time.timeScale = 1f;
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null) Destroy(_materials[i]);
            if (_adaptationProfile != null) Destroy(_adaptationProfile);
            if (_roundedSprite != null) Destroy(_roundedSprite);
            if (_circleSprite != null) Destroy(_circleSprite);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }

    public sealed class TankArenaJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _root;
        private RectTransform _knob;
        private Vector2 _value;
        private bool _isHeld;

        public Vector2 Value => _value;
        public bool IsHeld => _isHeld;

        public void Initialize(RectTransform root)
        {
            _root = root;
        }

        public void SetVisuals(RectTransform ring, RectTransform knob)
        {
            _knob = knob;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isHeld = true;
            UpdateValue(eventData);
        }
        public void OnDrag(PointerEventData eventData) => UpdateValue(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            _isHeld = false;
            _value = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (_root == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _root, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                Vector2 half = _root.rect.size * 0.5f;
                _value = Vector2.ClampMagnitude(new Vector2(
                    local.x / Mathf.Max(1f, half.x), local.y / Mathf.Max(1f, half.y)), 1f);
                if (_knob != null) _knob.anchoredPosition = _value * Mathf.Min(half.x, half.y) * 0.46f;
            }
        }
    }
}
