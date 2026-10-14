param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '../..'),
    [switch]$ResetLock
)

$ErrorActionPreference = 'Stop'
$taskProjectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$manifestPath = Join-Path $taskProjectRoot 'Packages/manifest.json'
$lockPath = Join-Path $taskProjectRoot 'Packages/packages-lock.json'
if (!(Test-Path -LiteralPath (Join-Path $taskProjectRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'ProjectPath 必须是 Unity 工程根目录。'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$spec = $manifest.dependencies.'com.unity.test-framework'
if (!$spec) { $spec = '1.1.33' }
Write-Output "工程：$taskProjectRoot"
Write-Output "测试框架版本：$spec"

foreach ($registry in @('https://packages.unity.com', 'https://packages.unity.cn')) {
    try {
        $package = Invoke-RestMethod "$registry/com.unity.test-framework" -TimeoutSec 20
        $version = $package.versions.PSObject.Properties[$spec]
        if (!$version) {
            Write-Warning "$registry 可访问，但没有列出版本 $spec。"
            continue
        }
        Write-Output "${registry}：可访问，版本 $spec 存在。"
        try {
            $download = Invoke-WebRequest $version.Value.dist.tarball -Method Head -TimeoutSec 20 -UseBasicParsing
            Write-Output "安装包下载地址：可访问（HTTP $($download.StatusCode)）。"
        } catch {
            Write-Warning "安装包下载地址访问失败：$($_.Exception.Message)"
        }
    } catch {
        Write-Warning "$registry 访问失败：$($_.Exception.Message)"
    }
}

if (Test-Path -LiteralPath $lockPath) {
    $lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
    $entry = $lock.dependencies.'com.unity.test-framework'
    Write-Output "锁文件中的测试包源：$($entry.url)"
}

if ($ResetLock) {
    $active = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
        Where-Object { $_.CommandLine -match [regex]::Escape($taskProjectRoot) }
    if ($active) { throw '请先关闭此工程的 Unity Editor，再用 -ResetLock 运行本脚本。' }
    if (Test-Path -LiteralPath $lockPath) {
        $backupDirectory = Join-Path $taskProjectRoot ('Temp/PackageRecovery/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        New-Item -Path $backupDirectory -ItemType Directory -Force | Out-Null
        Copy-Item -LiteralPath $lockPath -Destination (Join-Path $backupDirectory 'packages-lock.json')
        Remove-Item -LiteralPath $lockPath
        Write-Output "已备份锁文件到 $backupDirectory。重新打开 Unity 后，会按当前 Editor 的包源重新解析。"
    }
} else {
    Write-Output '需要重新解析时：关闭 Unity，使用 -ResetLock 再运行本脚本。'
}
Write-Output '若包源或下载地址仍然无法访问，请检查 Package Manager 显示的网络/代理错误；删除工程测试脚本不会解决下载失败。'
