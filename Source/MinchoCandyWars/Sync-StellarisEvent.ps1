<#
.SYNOPSIS
    从 ProdrakeRace 重新移植 StellarisEvent 事件系统到 Mincho-the-Candy-Wars。

.DESCRIPTION
    ProdrakeRace 的 Source\PDR\StellarisEvent 是该事件系统的上游（source of truth）。
    Mincho 侧的 Source\MinchoCandyWars\StellarisEvent 是它的移植副本。
    本脚本把上游全部文件复制过来，并施加一组固定的、可审计的差异变换，
    使重新移植无需手工比对。

    施加的变换（这就是 Mincho 副本与 ProdrakeRace 之间的全部差异）：
      1. 换行统一为 LF（Mincho 仓库使用 LF，ProdrakeRace 使用 CRLF）
      2. UTF-8 无 BOM 编码
      3. 命名空间 PDR -> MinchoCandyWars.StellarisEvent
         （含 XML 文档示例中的 Class="PDR.Xxx" 与 typeof(PDR.StellarisEventDef)）
      4. 调试菜单分类 "PDROD" -> "Stellaris Events"
      5. 每个 .cs 文件首行加入 #nullable disable
         Mincho 工程启用了 <Nullable>enable</Nullable>，而上游未启用；
         该指令让这批移植文件保持与上游逐字一致，避免每次移植都要重新补可空注解。

    脚本位于 StellarisEvent 目录之外，因此该目录始终是上游的纯净镜像。

    注意: 本脚本含中文，必须保存为 UTF-8 with BOM（EF BB BF），否则
    Windows PowerShell 5.1 会按 ANSI 解析并报出莫名的语法错误。
    若用编辑器/工具改过本文件，请确认开头 BOM 仍然存在。
    运行方式:
      powershell -NoProfile -ExecutionPolicy Bypass -File .\Sync-StellarisEvent.ps1 -Verify

.PARAMETER ProdrakeSource
    上游 ProdrakeRace 的 StellarisEvent 目录。

.PARAMETER Target
    目标目录，默认 .\StellarisEvent。

.PARAMETER Verify
    只校验不写入：报告每个文件是否与"上游 + 变换"完全一致。

.PARAMETER Build
    同步后编译 MinchoCandyWars.csproj 以验证 0 错误 0 警告。

.EXAMPLE
    .\Sync-StellarisEvent.ps1 -Verify
    校验当前副本是否仍与上游一致（只读，不改动文件）。

.EXAMPLE
    .\Sync-StellarisEvent.ps1 -Build
    执行移植并编译验证。
#>
[CmdletBinding()]
param(
    [string]$ProdrakeSource = 'E:\SteamLibrary\steamapps\common\RimWorld\Mods\ProdrakeRace\Source\PDR\StellarisEvent',
    [string]$Target,
    [switch]$Verify,
    [switch]$Build
)

$ErrorActionPreference = 'Stop'

if (-not $Target) { $Target = Join-Path $PSScriptRoot 'StellarisEvent' }
$ProdrakeSource = (Resolve-Path $ProdrakeSource).Path
$Target = (Resolve-Path $Target).Path

if (-not (Test-Path $ProdrakeSource)) {
    throw "找不到上游目录: $ProdrakeSource"
}

$encoding = New-Object System.Text.UTF8Encoding($false)

# 本仓库 core.autocrlf=true，checkout 后工作区文件可能变成 CRLF。
# 比较时忽略换行差异，避免刚 checkout 就误报"不一致"。
function Normalize-Newlines {
    param([string]$Text)
    if ($null -eq $Text) { return $null }
    return ($Text -replace "`r`n", "`n")
}

function Convert-EventFile {
    param([string]$Text, [string]$Extension)

    # 1. 换行统一为 LF
    $t = $Text -replace "`r`n", "`n"

    if ($Extension -eq '.cs') {
        # 3. 命名空间（先替换 "PDR." 形式，再替换裸 namespace 声明）
        $t = $t.Replace('PDR.', 'MinchoCandyWars.StellarisEvent.')
        $t = $t.Replace('namespace PDR', 'namespace MinchoCandyWars.StellarisEvent')

        # 4. 调试菜单分类
        $t = $t.Replace('[DebugAction("PDROD"', '[DebugAction("Stellaris Events"')

        # 5. 关闭可空分析，保持与上游逐字一致
        if ($t -notmatch '(?m)^#nullable disable') {
            $t = "#nullable disable`n" + $t
        }
    }
    else {
        # Markdown 文档：只替换命名空间（含 `PDR` 这种行内代码形式）
        $t = $t.Replace('`PDR`', '`MinchoCandyWars.StellarisEvent`')
        $t = $t.Replace('PDR.', 'MinchoCandyWars.StellarisEvent.')
    }

    return $t
}

$sourceFiles = Get-ChildItem -Recurse -File $ProdrakeSource
$created = @()
$updated = @()
$unchanged = @()
$differs = @()

foreach ($file in $sourceFiles) {
    $rel = $file.FullName.Substring($ProdrakeSource.Length + 1)
    $targetPath = Join-Path $Target $rel
    $expected = Convert-EventFile -Text ([System.IO.File]::ReadAllText($file.FullName)) -Extension $file.Extension

    $exists = Test-Path $targetPath
    if ($exists) {
        $identical = ((Normalize-Newlines ([System.IO.File]::ReadAllText($targetPath))) -ceq (Normalize-Newlines $expected))
    }
    else {
        $identical = $false
    }

    if ($Verify) {
        if (-not $exists) { $differs += "缺失: $rel" }
        elseif (-not $identical) { $differs += "不一致: $rel" }
        else { $unchanged += $rel }
        continue
    }

    New-Item -ItemType Directory -Force -Path (Split-Path $targetPath) | Out-Null
    [System.IO.File]::WriteAllText($targetPath, $expected, $encoding)

    if (-not $exists) { $created += $rel }
    elseif ($identical) { $unchanged += $rel }
    else { $updated += $rel }
}

# 目标目录中已不存在于上游的文件（上游删除或改名）
$sourceRel = $sourceFiles | ForEach-Object { $_.FullName.Substring($ProdrakeSource.Length + 1) }
$orphans = @()
if (Test-Path $Target) {
    $orphans = Get-ChildItem -Recurse -File $Target |
        Where-Object { $_.Extension -in '.cs', '.md' } |
        ForEach-Object { $_.FullName.Substring($Target.Length + 1) } |
        Where-Object { $_ -notin $sourceRel }
}

if ($Verify) {
    Write-Host "校验: $($unchanged.Count) 个文件与上游一致" -ForegroundColor Green
    if ($differs) {
        Write-Host "发现 $($differs.Count) 个文件与上游不一致:" -ForegroundColor Yellow
        $differs | ForEach-Object { Write-Host "  $_" }
        Write-Host "`n运行 .\Sync-StellarisEvent.ps1 可重新移植。" -ForegroundColor Yellow
    }
}
else {
    Write-Host "新增 $($created.Count) 个，更新 $($updated.Count) 个，未变 $($unchanged.Count) 个。" -ForegroundColor Green
    if ($created) { $created | ForEach-Object { Write-Host "  + $_" -ForegroundColor Cyan } }
    if ($updated) { $updated | ForEach-Object { Write-Host "  ~ $_" -ForegroundColor Cyan } }
}

if ($orphans) {
    Write-Host "`n以下文件存在于目标目录、但上游已无对应文件，请确认是否应删除:" -ForegroundColor Yellow
    $orphans | ForEach-Object { Write-Host "  ? $_" -ForegroundColor Yellow }
}

if ($Build -and -not $Verify) {
    $csproj = Join-Path $PSScriptRoot 'MinchoCandyWars.csproj'
    Write-Host "`n编译验证: $csproj" -ForegroundColor Green
    dotnet build $csproj -t:Rebuild
}
