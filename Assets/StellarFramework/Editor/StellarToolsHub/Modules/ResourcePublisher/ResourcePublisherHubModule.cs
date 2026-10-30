// 【2026-10-30｜RESOURCE-PUBLISHER-01】通用资源 CDN 配置、只读校验与安全发布工具。
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using StellarFramework.Editor;

namespace StellarFramework.Editor.ResourcePublisher
{
    [StellarTool("CDN 资源发布", "资源管理", 2)]
    public sealed class ResourcePublisherHubModule : ToolModule
    {
        private const string PrefsPrefix = "StellarFramework.ResourcePublisher.";
        private static readonly string[] PlatformNames = { "Android", "iOS", "Windows" };
        private string _packageDirectory = string.Empty;
        private string _packageName = string.Empty;
        private string _version = "1.0.0";
        private string _expectedVersion = string.Empty;
        private string _resourceRoot = "GameBundles";
        private int _platformIndex;
        private ResourceEnvironmentKind _environment;
        private ResourceEnvironmentProfile _profile;
        private string _cdnBase = string.Empty;
        private string _cdnFallback = string.Empty;
        private bool _busy;
        private string _status = "请选择已构建的 YooAsset Package 版本目录。";
        private ResourcePublishPlan _plan;
        private string _planSignature = string.Empty;
        private Vector2 _scroll;

        public override string Description => "配置 CDN、检查版本与文件，向本地目录或 S3 兼容存储发布 YooAsset 资源。无需 HybridCLR。";
        public override string Icon => "d_CloudConnect";
        private string Prefix => PrefsPrefix + Application.dataPath + ".";

        [MenuItem("Tools/StellarFramework/资源构建/CDN 资源发布")]
        private static void Open() => StellarFrameworkTools.OpenModule<ResourcePublisherHubModule>();

        public override void OnEnable()
        {
            _resourceRoot = EditorPrefs.GetString(Prefix + "root", _resourceRoot);
            _packageDirectory = EditorPrefs.GetString(Prefix + "packageDir", "");
            _packageName = EditorPrefs.GetString(Prefix + "package", "");
            _version = EditorPrefs.GetString(Prefix + "version", "1.0.0");
            _platformIndex = Mathf.Clamp(EditorPrefs.GetInt(Prefix + "platform", 0), 0, 2);
            _environment = (ResourceEnvironmentKind)Mathf.Clamp(EditorPrefs.GetInt(Prefix + "environment", 0), 0, 2);
            LoadEnvironment();
        }

        private void LoadEnvironment()
        {
            string id = Prefix + _environment;
            string json = EditorPrefs.GetString(id + ".profile", "");
            try { _profile = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<ResourceEnvironmentProfile>(json); }
            catch { _profile = null; }
            if (_profile == null) _profile = ResourceEnvironmentProfile.CreateDefault(_environment);
            _profile.EnvironmentId = _environment.ToString();
            _cdnBase = EditorPrefs.GetString(id + ".cdnBase", "");
            _cdnFallback = EditorPrefs.GetString(id + ".cdnFallback", "");
            _expectedVersion = EditorPrefs.GetString(id + ".expected", "");
            ResetPlan();
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(Prefix + "root", _resourceRoot);
            EditorPrefs.SetString(Prefix + "packageDir", _packageDirectory);
            EditorPrefs.SetString(Prefix + "package", _packageName);
            EditorPrefs.SetString(Prefix + "version", _version);
            EditorPrefs.SetInt(Prefix + "platform", _platformIndex);
            EditorPrefs.SetInt(Prefix + "environment", (int)_environment);
            string id = Prefix + _environment;
            EditorPrefs.SetString(id + ".cdnBase", _cdnBase);
            EditorPrefs.SetString(id + ".cdnFallback", _cdnFallback);
            EditorPrefs.SetString(id + ".expected", _expectedVersion);
            EditorPrefs.SetString(id + ".profile", JsonUtility.ToJson(_profile));
        }

        public override void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("独立于代码热更。上传只支持本地目录和 S3 兼容存储；普通 HTTP(S) CDN URL 用于只读校验。不会修改 StreamingAssets 或当前 Unity 平台。", MessageType.Info);
            EditorGUI.BeginDisabledGroup(_busy);
            Section("资源包与目录");
            string root = EditorGUILayout.TextField("发布根目录名", _resourceRoot);
            int platform = EditorGUILayout.Popup("目标平台", _platformIndex, PlatformNames);
            using (new EditorGUILayout.HorizontalScope())
            {
                _packageDirectory = EditorGUILayout.TextField("构建产物目录", _packageDirectory);
                if (GUILayout.Button("选择", GUILayout.Width(65))) ChoosePackage();
            }
            _packageName = EditorGUILayout.TextField("Package 名称", _packageName);
            _version = EditorGUILayout.TextField("待发布版本", _version);
            ResourceEnvironmentKind environment = (ResourceEnvironmentKind)EditorGUILayout.EnumPopup("发布环境", _environment);
            if (environment != _environment)
            {
                SaveSettings();
                _environment = environment;
                LoadEnvironment();
            }
            if (root != _resourceRoot || platform != _platformIndex)
            {
                _resourceRoot = root;
                _platformIndex = platform;
                ResetPlan();
            }
            Section("CDN 读取地址");
            _cdnBase = EditorGUILayout.TextField("主 CDN 基础 URL", _cdnBase);
            _cdnFallback = EditorGUILayout.TextField("备用 CDN 基础 URL", _cdnFallback);
            Section("发布目标");
            bool s3 = _profile.PublishTarget == "S3Compatible";
            int targetIndex = EditorGUILayout.Popup("上传方式", s3 ? 1 : 0, new[] { "本地/挂载文件夹", "S3 兼容存储" });
            _profile.PublishTarget = targetIndex == 1 ? "S3Compatible" : "LocalFolder";
            if (targetIndex == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _profile.LocalFolderRoot = EditorGUILayout.TextField("部署目标根目录", _profile.LocalFolderRoot);
                    if (GUILayout.Button("选择", GUILayout.Width(65)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("部署资源的文件夹", _profile.LocalFolderRoot, "");
                        if (!string.IsNullOrWhiteSpace(selected)) _profile.LocalFolderRoot = selected;
                    }
                }
            }
            else
            {
                _profile.S3ServiceEndpoint = EditorGUILayout.TextField("S3 Endpoint", _profile.S3ServiceEndpoint);
                _profile.S3Bucket = EditorGUILayout.TextField("Bucket", _profile.S3Bucket);
                _profile.S3Region = EditorGUILayout.TextField("Region", _profile.S3Region);
                _profile.CredentialProfileName = EditorGUILayout.TextField("凭证 Profile", _profile.CredentialProfileName);
                EditorGUILayout.HelpBox("Access Key/Secret Key 从 STELLAR_RESOURCE_<PROFILE> 系统环境变量读取，不写入配置文件。", MessageType.None);
            }
            _expectedVersion = EditorGUILayout.TextField("远端当前版本（首次留空）", _expectedVersion);
            string relative = "";
            try
            {
                relative = ResourcePublisherService.CombineRemoteRoot(_resourceRoot, PlatformNames[_platformIndex], _packageName);
                EditorGUILayout.LabelField("目标相对路径", relative);
                if (!string.IsNullOrWhiteSpace(_cdnBase))
                    EditorGUILayout.SelectableLabel(ResourcePublisherService.PackageHost(_cdnBase, relative), EditorStyles.textField, GUILayout.Height(20));
            }
            catch (Exception e) { EditorGUILayout.HelpBox(e.Message, MessageType.Warning); }
            if (GUILayout.Button("保存配置")) { SaveSettings(); ResetPlan(); _status = "当前环境配置已保存到本机 EditorPrefs。"; }
            Section("只读检查");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("CDN 快速抽检")) RunRemote(false);
                if (GUILayout.Button("CDN 全量 SHA-256 校验")) RunRemote(true);
            }
            Section("上传与发布");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("预览差异（不上传）")) RunPreview();
                GUI.enabled = !_busy && _plan != null && _planSignature == Signature();
                if (GUILayout.Button("上传并发布版本")) RunPublish();
                GUI.enabled = !_busy;
            }
            if (_plan != null)
                EditorGUILayout.HelpBox("预览：待上传 " + _plan.UploadFiles.Count +
                    " 个文件，复用 " + _plan.ReusedFiles.Count + " 个文件，待上传 " +
                    (_plan.UploadBytes / (1024d * 1024d)).ToString("F2") + " MiB。", MessageType.Info);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(_status, MessageType.None);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
        }

        private void ChoosePackage()
        {
            string directory = EditorUtility.OpenFolderPanel("选择构建完成的 Package 版本目录", _packageDirectory, "");
            if (string.IsNullOrWhiteSpace(directory)) return;
            _packageDirectory = directory;
            var versionDirectory = new DirectoryInfo(directory);
            _version = versionDirectory.Name;
            if (versionDirectory.Parent != null) _packageName = versionDirectory.Parent.Name;
            ResetPlan();
        }

        private List<ResourcePublishFile> Collect() =>
            ResourcePublisherService.Collect(_packageDirectory, _packageName, _version);

        private string Signature() =>
            _packageDirectory + "|" + _packageName + "|" + _version + "|" + _resourceRoot + "|" +
            _platformIndex + "|" + _environment + "|" + JsonUtility.ToJson(_profile);
        private void ResetPlan() { _plan = null; _planSignature = ""; }

        private ResourceEnvironmentProfile CurrentProfile()
        {
            ResourceEnvironmentProfile profile = JsonUtility.FromJson<ResourceEnvironmentProfile>(JsonUtility.ToJson(_profile));
            profile.EnvironmentId = _environment.ToString();
            profile.RemoteRoot = ResourcePublisherService.CombineRemoteRoot(_resourceRoot, PlatformNames[_platformIndex], _packageName);
            if (!string.IsNullOrWhiteSpace(_cdnBase)) profile.MainHostServer = ResourcePublisherService.PackageHost(_cdnBase, profile.RemoteRoot);
            if (!string.IsNullOrWhiteSpace(_cdnFallback)) profile.FallbackHostServer = ResourcePublisherService.PackageHost(_cdnFallback, profile.RemoteRoot);
            if (string.IsNullOrWhiteSpace(_cdnBase) && profile.PublishTarget == "LocalFolder" &&
                _environment == ResourceEnvironmentKind.Development)
                profile.MainHostServer = ResourceEnvironmentProfile.CreateLocalFileHost(profile.LocalFolderRoot, profile.RemoteRoot);
            ResourceEnvironmentProfileValidationResult validation = profile.Validate();
            if (!validation.IsValid) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            return profile;
        }

        private IResourcePublishTarget Target()
        {
            ResourceEnvironmentProfile profile = CurrentProfile();
            if (profile.PublishTarget == "LocalFolder")
                return new LocalFolderResourcePublishTarget(profile);
            return new S3CompatibleResourcePublishTarget(profile, new S3CompatiblePublishTargetOptions {
                ServiceEndpoint = new Uri(profile.S3ServiceEndpoint),
                Bucket = profile.S3Bucket, Region = profile.S3Region
            }, new EnvironmentVariableResourceCredentialProvider());
        }

        private async void RunPreview()
        {
            if (_busy) return;
            _busy = true;
            try
            {
                List<ResourcePublishFile> files = Collect();
                _plan = await ResourcePublisherService.PreviewAsync(files, Target(), CancellationToken.None);
                _planSignature = Signature();
                _status = "只读差异扫描成功。确认无误后才能上传。";
            }
            catch (Exception e) { ResetPlan(); _status = "预览失败：" + e.Message; Debug.LogException(e); }
            finally { _busy = false; Window?.Repaint(); }
        }

        private async void RunRemote(bool strict)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                List<ResourcePublishFile> files = Collect();
                string relative = ResourcePublisherService.CombineRemoteRoot(_resourceRoot, PlatformNames[_platformIndex], _packageName);
                if (string.IsNullOrWhiteSpace(_cdnBase)) throw new InvalidOperationException("请先填写主 CDN URL。");
                foreach (string host in new[] { _cdnBase, _cdnFallback })
                {
                    if (string.IsNullOrWhiteSpace(host)) continue;
                    string url = ResourcePublisherService.PackageHost(host, relative);
                    ResourceRemoteVerificationResult result = await ResourceRemoteVerifier.VerifyAsync(
                        url, _packageName, _version, files, strict, CancellationToken.None);
                    _status = (strict ? "全量" : "快速抽检") + "通过：" + url + "，校验文件 " + result.CheckedFiles + " 个。";
                }
            }
            catch (Exception e) { _status = "CDN 检查失败：" + e.Message; Debug.LogException(e); }
            finally { _busy = false; Window?.Repaint(); }
        }

        private async void RunPublish()
        {
            if (_busy || _plan == null || _planSignature != Signature()) return;
            if (!EditorUtility.DisplayDialog("资源发布确认",
                "将发布 " + _packageName + "（" + PlatformNames[_platformIndex] + "），切换版本到 " +
                _version + "。文件不可变，只有全部验证成功才切换版本指针。是否继续？", "确认发布", "取消")) return;
            _busy = true;
            try
            {
                // 目标对象重新构造，避免发布时沿用陈旧凭证或目录。
                await ResourcePublisherService.PublishAsync(_plan, Target(),
                    _packageName, _version, _expectedVersion, CancellationToken.None);
                _expectedVersion = _version;
                SaveSettings();
                ResetPlan();
                _status = "上传、全文件 SHA-256 验证和版本指针切换已完成。";
            }
            catch (Exception e) { _status = "发布失败（不代表版本已切换）：" + e.Message; Debug.LogException(e); }
            finally { _busy = false; Window?.Repaint(); }
        }
    }
}
