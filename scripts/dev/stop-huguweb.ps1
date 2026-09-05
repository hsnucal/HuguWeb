# Stop leftover HuGuWeb F5 debug processes for THIS repository only.
# Invoked automatically as the HuGuWeb Development postDebugTask (Shift+F5).
# Safe to run repeatedly from the repository as a manual fallback:
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\dev\stop-huguweb.ps1
# Do not run Set-ExecutionPolicy.
#
# Never stops unrelated dotnet/netcoredbg/node processes.
# Never uses taskkill /IM or Stop-Process -Name for dotnet/node.
# Vite, PostgreSQL, and the Cursor editor are left running.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-MatchText {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    return (($Value.ToLowerInvariant()) -replace '[/\\]+', '\')
}

function Get-ProcessLabel {
    param([string]$Name)

    $lower = $Name.ToLowerInvariant()
    if ($lower -like 'huguweb.api*') {
        return 'HuGuWeb.Api'
    }

    if ($lower -like 'netcoredbg*') {
        return 'HuGuWeb netcoredbg'
    }

    if ($lower -like 'dotnet*') {
        return 'HuGuWeb.Api'
    }

    return "HuGuWeb $Name"
}

function Test-IsProtectedName {
    param([string]$Name)

    $lower = $Name.ToLowerInvariant()
    return @(
        'cursor.exe',
        'code.exe',
        'devenv.exe',
        'explorer.exe',
        'services.exe',
        'lsass.exe',
        'csrss.exe',
        'svchost.exe',
        'system'
    ) -contains $lower
}

function Test-IsDotnetToolingCommand {
    param([string]$Hay)

    if ([string]::IsNullOrWhiteSpace($Hay)) {
        return $false
    }

    return (
        $Hay -match '\\msbuild\.dll' -or
        $Hay -match '\sbuild(\s|$)' -or
        $Hay -match '\srestore(\s|$)' -or
        $Hay -match '\spublish(\s|$)' -or
        $Hay -match '\stest(\s|$)' -or
        $Hay -match '\spack(\s|$)' -or
        $Hay -match '\svbcscompiler' -or
        $Hay -match 'stop-huguweb\.ps1'
    )
}

function Test-IsHuGuWebApiHost {
    param(
        $Process,
        [string]$RepoNorm,
        [string]$ApiBinNorm
    )

    $name = $Process.Name.ToLowerInvariant()
    if (Test-IsProtectedName -Name $name) {
        return $false
    }

    $hay = ConvertTo-MatchText -Value ("{0} {1}" -f $Process.CommandLine, $Process.ExecutablePath)

    if ($name -like 'huguweb.api*') {
        return $true
    }

    if (Test-IsDotnetToolingCommand -Hay $hay) {
        return $false
    }

    $inRepo = $hay.Contains($RepoNorm)
    $inApiBin = $hay.Contains($ApiBinNorm)
    $hasApiDll = $hay.Contains('huguweb.api.dll')
    $hasApiExe = $hay.Contains('huguweb.api.exe')

    if ($inApiBin -and ($hasApiDll -or $hasApiExe)) {
        return $true
    }

    if ($inRepo -and $hasApiDll) {
        return $true
    }

    if ($inRepo -and $hasApiExe) {
        return $true
    }

    if (
        $name -like 'dotnet*' -and
        $inRepo -and
        $hay.Contains('huguweb.api') -and
        $hay -match '(^|[\s"])run(\s|$)' -and
        $hay -notmatch '\sbuild(\s|$)'
    ) {
        return $true
    }

    return $false
}

Write-Host 'HuGuWeb cleanup:'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$repoNorm = ConvertTo-MatchText -Value $repoRoot
$apiBinNorm = ConvertTo-MatchText -Value (Join-Path $repoRoot 'src\backend\HuGuWeb.Api\bin')

$snapshot = @(Get-CimInstance Win32_Process -ErrorAction Stop)
$byId = @{}
foreach ($process in $snapshot) {
    $byId[[int]$process.ProcessId] = $process
}

$protected = @{}
$walker = [int]$PID
$guard = 0
while ($walker -gt 0 -and $guard -lt 40) {
    $protected[$walker] = $true
    if (-not $byId.ContainsKey($walker)) {
        break
    }

    $walker = [int]$byId[$walker].ParentProcessId
    $guard++
}

$selected = @{}
foreach ($process in $snapshot) {
    $id = [int]$process.ProcessId
    if ($protected.ContainsKey($id)) {
        continue
    }

    if (Test-IsHuGuWebApiHost -Process $process -RepoNorm $repoNorm -ApiBinNorm $apiBinNorm) {
        $selected[$id] = $true
    }
}

$added = $true
while ($added) {
    $added = $false
    $currentIds = @($selected.Keys)
    foreach ($id in $currentIds) {
        if (-not $byId.ContainsKey($id)) {
            continue
        }

        $process = $byId[$id]
        $parentId = [int]$process.ParentProcessId
        if (
            $parentId -gt 0 -and
            -not $protected.ContainsKey($parentId) -and
            -not $selected.ContainsKey($parentId) -and
            $byId.ContainsKey($parentId)
        ) {
            $parent = $byId[$parentId]
            if ($parent.Name.ToLowerInvariant() -like 'netcoredbg*') {
                $selected[$parentId] = $true
                $added = $true
            }
        }
    }

    foreach ($process in $snapshot) {
        $id = [int]$process.ProcessId
        $parentId = [int]$process.ParentProcessId
        if (
            $selected.ContainsKey($parentId) -and
            -not $selected.ContainsKey($id) -and
            -not $protected.ContainsKey($id) -and
            -not (Test-IsProtectedName -Name $process.Name)
        ) {
            $childHay = ConvertTo-MatchText -Value ("{0} {1}" -f $process.CommandLine, $process.ExecutablePath)
            $childName = $process.Name.ToLowerInvariant()
            $parent = $byId[$parentId]
            $parentIsDbg = $parent -and ($parent.Name.ToLowerInvariant() -like 'netcoredbg*')
            if (
                $childName -like 'dotnet*' -or
                $childName -like 'huguweb.api*' -or
                $childName -like 'netcoredbg*' -or
                ($parentIsDbg -and $childName -eq 'conhost.exe') -or
                ($childHay.Contains($apiBinNorm))
            ) {
                $selected[$id] = $true
                $added = $true
            }
        }
    }
}

if ($selected.Count -eq 0) {
    Write-Host 'No HuGuWeb processes are running.'
    exit 0
}

$ordered = @($selected.Keys | Sort-Object -Descending)
foreach ($id in $ordered) {
    if (-not $byId.ContainsKey($id)) {
        continue
    }

    $process = $byId[$id]
    Write-Host ("Stopping {0} PID {1}" -f (Get-ProcessLabel -Name $process.Name), $id)
    Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Milliseconds 400

$remaining = @()
foreach ($id in $ordered) {
    if (Get-Process -Id $id -ErrorAction SilentlyContinue) {
        Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
        $remaining += $id
    }
}

if ($remaining.Count -gt 0) {
    Start-Sleep -Milliseconds 400
}

Write-Host 'HuGuWeb processes stopped.'
exit 0
