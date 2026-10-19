#Requires -Version 5.1
<#
.SYNOPSIS
    编译 Mincho, the Candy Wars（RimWorld 1.7 / Waylith / net10）。

.DESCRIPTION
    本机的情况：全局只装了 .NET 9，而本工程的 TargetFramework 必须是 net10.0
    （1.7 的游戏程序集引用 System.Runtime 10.0.0.0，见 Source/Directory.Build.props）。
    直接用 PATH 上的 dotnet 会报 NETSDK1045。

    这个脚本会自己找出一个可用的 .NET 10 SDK，顺序是：
      1. %LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe   （用户级安装，不需要管理员）
      2. PATH 上的 dotnet（仅当它确实是 10.x）
    找不到就明确报错，而不是退化成一堆 CS0246。

.PARAMETER Temp
    编译到 %TEMP%\mcw-out，不动 1.7\Assemblies\。
    游戏正在运行时 MinchoCandyWars.dll 会被 RimWorld 锁住，无法覆盖；
    这时候用 -Temp 可以先验证代码能不能过编译。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File Source\Build.ps1
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File Source\Build.ps1 -Temp
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [switch]$Temp
)

$ErrorActionPreference = 'Stop'

$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj      = Join-Path $sourceDir 'MinchoCandyWars\MinchoCandyWars.csproj'

if (-not (Test-Path -LiteralPath $proj)) {
    throw "找不到工程文件：$proj"
}

# ── 1. 找一个 >= 10 的 dotnet ────────────────────────────────────────────────
function Get-DotnetMajor([string]$exe) {
    try {
        $v = & $exe --version 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $v) { return -1 }
        return [int](($v -split '\.')[0])
    } catch {
        return -1
    }
}

$candidates = @(
    (Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'),
    'dotnet'
)

$dotnet = $null
foreach ($c in $candidates) {
    if ($c -eq 'dotnet') {
        $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
        if (-not $cmd) { continue }
        $c = $cmd.Source
    }
    if (-not (Test-Path -LiteralPath $c)) { continue }
    $major = Get-DotnetMajor $c
    Write-Verbose "候选 $c -> .NET $major"
    if ($major -ge 10) { $dotnet = $c; break }
}

if (-not $dotnet) {
    throw @'
没有找到 .NET 10 SDK（本工程 TargetFramework=net10.0，.NET 9 及以下无法编译）。

安装方法（不需要管理员，装到用户目录）：
  $s = "$env:TEMP\dotnet-install.ps1"
  Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $s -UseBasicParsing
  powershell -NoProfile -ExecutionPolicy Bypass -File $s -Channel 10.0 -Quality GA -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet" -NoPath

注意：C:\Program Files\dotnet 在“系统”PATH 里，优先级高于用户 PATH，
所以即使装好了用户级 SDK，直接敲 dotnet 仍然会用 9.x（需要管理员权限才能改）。
本脚本会自动优先使用用户目录里的那份。
'@
}

Write-Host "dotnet   : $dotnet"
Write-Host "版本     : $(& $dotnet --version)"
Write-Host "配置     : $Configuration"

# ── 2. 编译 ──────────────────────────────────────────────────────────────────
$args = @(
    'build', $proj,
    '-t:Rebuild',
    '-nologo',
    '-v:m',
    "-c:$Configuration"
)

if ($Temp) {
    $out = Join-Path $env:TEMP 'mcw-out'
    $args += "-p:OutputPath=$out\"
    Write-Host "输出     : $out （-Temp：不改动 1.7\Assemblies）"
} else {
    Write-Host "输出     : 1.7\Assemblies"
}

Write-Host ''
$log = Join-Path $env:TEMP 'mcw-build.log'
& $dotnet @args *>&1 | Tee-Object -FilePath $log | ForEach-Object { $_ }

$text = Get-Content -LiteralPath $log -Raw
$errors   = ([regex]::Matches($text, 'error [A-Z]+\d+')).Count
$warnings = ([regex]::Matches($text, 'warning [A-Z]+\d+')).Count

Write-Host ''
Write-Host ("错误 {0} 个，警告 {1} 个" -f $errors, $warnings)

if ($errors -gt 0) {
    # 目标 dll 被游戏占用时给一句人话，而不是只有 MSB3021
    if ($text -match 'because it is being used by another process|文件被') {
        Write-Host ''
        Write-Host '提示：代码编译是通过的，失败的是“把 dll 复制到 1.7\Assemblies”这一步 ——' -ForegroundColor Yellow
        Write-Host '      RimWorld 正在运行并锁定了 MinchoCandyWars.dll。' -ForegroundColor Yellow
        Write-Host '      关掉游戏后重新运行本脚本，或先用 -Temp 编译到临时目录验证代码。' -ForegroundColor Yellow
    }
    exit 1
}

exit 0
