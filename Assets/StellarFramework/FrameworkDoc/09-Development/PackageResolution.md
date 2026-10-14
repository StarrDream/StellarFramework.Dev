# 新机器首次打开与包解析

开发仓声明的环境是 Unity 2022.3 LTS。`com.unity.test-framework` 1.1.33 在 Unity 官方与中国包源中均存在；UnitySkills 2.8.4 本身也声明了这项依赖。因此，Package Manager 的下载或解析错误不能靠删除框架测试脚本解决。

在工程根目录运行以下命令可检查测试包版本及下载地址是否可访问：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/DevelopmentEnvironment/Repair-UnityPackageResolution.ps1
```

锁文件可能保留另一台电脑使用的包源。关闭此工程的 Unity 后，加 `-ResetLock` 会将当前 `Packages/packages-lock.json` 备份到 `Temp/PackageRecovery`，然后移除锁文件。下次打开工程时，Unity 会按当前 Editor 的包源重新解析。脚本保留 `manifest.json` 中的版本配置，不修改全局 Unity 缓存、代理或包源。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/DevelopmentEnvironment/Repair-UnityPackageResolution.ps1 -ResetLock
```

开发仓的测试程序集带有测试包版本条件，只有测试框架存在时才参与编译。发布仓与 Kit 导出包不包含框架测试程序集或 Test Runner 依赖。当前固定版本的 YooAsset / Addressables 编辑器代码直接引用 NUnit，因此对应导出项声明 `com.unity.ext.nunit` 1.0.6 作为第三方编辑器兼容依赖。
