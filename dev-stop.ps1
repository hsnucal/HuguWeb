# Stop API and frontend processes started by .\dev.ps1.
# PostgreSQL is left running. Unrelated dotnet/node processes are not killed.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$PidFile = Join-Path $env:LOCALAPPDATA 'HuGuWeb\dev-launcher.json'

function Get-ProcessCommandLine {
    param([int]$ProcessId)
    try {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction SilentlyContinue
        if ($process) {
            return [string]$process.CommandLine
        }
    }
    catch {
        return $null
    }
    return $null
}

function Test-OwnedHuGuWebProcess {
    param([int]$ProcessId, [string]$Kind)

    $commandLine = Get-ProcessCommandLine -ProcessId $ProcessId
    if (-not $commandLine) {
        return $false
    }

    if ($Kind -eq 'api') {
        return ($commandLine -match 'HuGuWeb\.Api' -or $commandLine -match 'title HuGuWeb API')
    }

    if ($Kind -eq 'frontend') {
        return (
            $commandLine -match 'src\\frontend\\web' -or
            $commandLine -match 'title HuGuWeb Frontend' -or
            ($commandLine -match 'npm\.cmd' -and $commandLine -match 'run dev')
        )
    }

    return $false
}

function Stop-OwnedProcessTree {
    param([int]$ProcessId)

    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        Stop-OwnedProcessTree -ProcessId $child.ProcessId
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Stop-TrackedProcess {
    param([object]$ProcessId, [string]$Kind, [string]$Label)

    if ($null -eq $ProcessId) {
        return
    }

    $id = [int]$ProcessId
    $running = Get-Process -Id $id -ErrorAction SilentlyContinue
    if (-not $running) {
        Write-Host "$Label was not running."
        return
    }

    if (-not (Test-OwnedHuGuWebProcess -ProcessId $id -Kind $Kind)) {
        Write-Host "$Label PID $id is running, but it does not look like a HuGuWeb launcher process. It was left unchanged."
        return
    }

    Stop-OwnedProcessTree -ProcessId $id
    Write-Host "Stopped $Label."
}

if (-not (Test-Path $PidFile)) {
    Write-Host 'No HuGuWeb launcher process file was found.'
    Write-Host 'Stop API/frontend by closing the consoles titled "HuGuWeb API" and "HuGuWeb Frontend".'
    Write-Host 'PostgreSQL is not stopped by this script.'
    exit 0
}

$state = Get-Content -Path $PidFile -Raw | ConvertFrom-Json
Stop-TrackedProcess -ProcessId $state.apiPid -Kind 'api' -Label 'HuGuWeb API'
Stop-TrackedProcess -ProcessId $state.frontendPid -Kind 'frontend' -Label 'HuGuWeb Frontend'

Remove-Item -Path $PidFile -Force -ErrorAction SilentlyContinue

Write-Host 'PostgreSQL was left running.'
