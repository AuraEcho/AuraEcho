# AuraEcho 构建脚本

$vsPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
$msBuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msBuildPath)) {
    Write-Host "错误: 未找到 MSBuild。" -ForegroundColor Red
    exit 1
}

$bundleProject = "./AuraEcho.Setup.wixproj"

$Config = Read-Host "选择构建配置 [1: Debug (默认), 2: Release]"
if ($Config -eq "2") { $Config = "Release" } else { $Config = "Debug" }

$Arch = Read-Host "选择目标架构 [1: x64 (默认), 2: x86]"
if ($Arch -eq "2") { $Arch = "x86" } else { $Arch = "x64" }

$Mode = Read-Host "选择构建模式 [1: Rebuild (默认), 2: Build]"
$isRebuild = if ($Mode -eq "2") { $false } else { $true }

$Runtime = Read-Host "选择构建版本 [1: Slim (默认), 2: Full]"
$isFull = if ($Runtime -eq "2") { "yes" } else { "no" }

$BundleVersion = "1.3.1"
$AppVersion = "1.3.1"
$LauncherVersion = "1.3.1"
$LauncherServiceVersion = "1.3.1"
$UpdaterServiceVersion = "1.3.1"
$TelemetryRelayServiceVersion = "1.3.1"
$DataMigratorVersion = $BundleVersion
$BundleFileName = "AuraEchoSetup"

Write-Host "`n--------------------------------------------------" -ForegroundColor Gray
Write-Host "构建计划:" -ForegroundColor Cyan
Write-Host ">> 配置: $Config"
Write-Host ">> 架构: $Arch"
Write-Host ">> 模式: $(if($isRebuild){"Rebuild"}else{"Build"})"
Write-Host ">> 版本: $(if($Runtime -eq 2){"Full"}else{"Slim"})"
Write-Host "--------------------------------------------------`n" -ForegroundColor Gray

if ($isRebuild) {
    Write-Host "正在清理旧的构建产物..." -ForegroundColor DarkGray
    $quietArgs = @("/v:m", "/nologo", "/clp:Summary=False", "/tl")
    & $msBuildPath $bundleProject /t:Clean /p:Configuration=$Config /p:Platform=$Arch @quietArgs
}

$buildArgs = @(
    $bundleProject,
    "/p:Configuration=$Config",
    "/p:Platform=$Arch",

    "/v:m",
    "/clp:NoSummary;HideProgress",
    "/nologo",
    "/tl"
    "/p:WarningLevel=0",

    "/p:BundleFileName=$BundleFileName",
    "/p:IncludeRuntime=$isFull",
    "/p:BundleVersion=$BundleVersion",
    "/p:AppVersion=$AppVersion",
    "/p:LauncherVersion=$LauncherVersion",
    "/p:DataMigratorVersion=$DataMigratorVersion",
    "/p:LauncherServiceVersion=$LauncherServiceVersion",
    "/p:UpdaterServiceVersion=$UpdaterServiceVersion",
    "/p:TelemetryRelayServiceVersion=$TelemetryRelayServiceVersion",
    "/m" #并行构建
)
Write-Host "`n开始构建..." -ForegroundColor DarkGray
& $msBuildPath @buildArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] 构建成功！" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] 构建失败。" -ForegroundColor Red
    exit 1
}
