# AuraEcho Telemetry 数据库迁移脚本
# 交互式封装 dotnet ef，针对 TelemetryDbContext

$ErrorActionPreference = "Stop"

# 切换到脚本所在目录（即 AuraEcho.Telemetry 项目根），使 dotnet ef 无需 --project
Set-Location -Path $PSScriptRoot

# 检查 dotnet ef 工具是否可用
$efInstalled = $false
try {
    dotnet ef --version *> $null
    if ($LASTEXITCODE -eq 0) { $efInstalled = $true }
} catch {}

if (-not $efInstalled) {
    Write-Host "错误: 未找到 dotnet ef 工具。" -ForegroundColor Red
    Write-Host "请先运行: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    exit 1
}

$Context = "TelemetryDbContext"
$OutputDir = "Migrations"

# 选择操作
$Action = Read-Host "选择操作 [1: 新增迁移 (默认), 2: 列出迁移, 3: 回滚上一个迁移, 4: 更新数据库]"

Write-Host "`n--------------------------------------------------" -ForegroundColor Gray
Write-Host "迁移计划:" -ForegroundColor Cyan
Write-Host ">> 数据库: $Context"
Write-Host ">> 目录:   $OutputDir"
Write-Host "--------------------------------------------------`n" -ForegroundColor Gray

switch ($Action) {
    "2" {
        Write-Host "正在列出已有迁移..." -ForegroundColor DarkGray
        dotnet ef migrations list --context $Context
    }
    "3" {
        Write-Host "警告: 将移除最后一个尚未应用的迁移。" -ForegroundColor Yellow
        $Confirm = Read-Host "确认回滚? [y/N]"
        if ($Confirm -eq "y" -or $Confirm -eq "Y") {
            dotnet ef migrations remove --context $Context
        } else {
            Write-Host "已取消。" -ForegroundColor DarkGray
            exit 0
        }
    }
    "4" {
        Write-Host "正在将数据库更新到最新迁移..." -ForegroundColor DarkGray
        dotnet ef database update --context $Context
    }
    default {
        $Name = Read-Host "输入迁移名称"
        if ([string]::IsNullOrWhiteSpace($Name)) {
            Write-Host "错误: 迁移名称不能为空。" -ForegroundColor Red
            exit 1
        }
        Write-Host "正在创建迁移 '$Name'..." -ForegroundColor DarkGray
        dotnet ef migrations add $Name --context $Context -o $OutputDir
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] 操作成功！" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] 操作失败。" -ForegroundColor Red
    exit 1
}
