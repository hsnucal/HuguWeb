# Requires PowerShell 5.1 or later.
#
# Preferred daily workflow: Cursor / VS Code F5 -> "HuGuWeb Development".
# CLI fallback from the repository root (process-local Bypass; do not Set-ExecutionPolicy):
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\dev.ps1
# F5 task switches (process-local Bypass; do not Set-ExecutionPolicy):
#   -EnsurePostgres              PostgreSQL ready on localhost:5432
#   -StartVite                   Start Vite if localhost:5173 is not already HTTP 200
#   -WaitFrontend                Wait until Vite returns HTTP 200
#   -StartChromeHealthWatcher    Detach a helper that opens Chrome after /health is 200
#   -OpenChromeWhenHealthy       Wait for API /health 200, then open http://localhost:5173
#
# This script starts DEVELOPMENT processes only. It does not install software,
# create a PostgreSQL cluster, change passwords, or store credentials.

[CmdletBinding()]
param(
    [switch]$EnsurePostgres,
    [switch]$StartVite,
    [switch]$WaitFrontend,
    [switch]$StartChromeHealthWatcher,
    [switch]$OpenChromeWhenHealthy
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = $PSScriptRoot
$ApiProject = Join-Path $RepoRoot 'src\backend\HuGuWeb.Api\HuGuWeb.Api.csproj'
$LaunchSettings = Join-Path $RepoRoot 'src\backend\HuGuWeb.Api\Properties\launchSettings.json'
$FrontendDir = Join-Path $RepoRoot 'src\frontend\web'
$ViteConfig = Join-Path $FrontendDir 'vite.config.ts'
$StateDir = Join-Path $env:LOCALAPPDATA 'HuGuWeb'
$PidFile = Join-Path $StateDir 'dev-launcher.json'

$script:PgIsReady = $null
$script:PgCtl = $null
$script:NpmCmd = $null
$script:ApiUrl = 'http://localhost:5116'
$script:FrontendUrl = 'http://localhost:5173'
$script:StartedApiPid = $null
$script:StartedFrontendPid = $null

function Write-Step {
    param([string]$Name, [string]$Status)
    $pad = $Name.PadRight(18, '.')
    Write-Host "$pad $Status"
}

function Stop-WithError {
    param([string]$Message)
    Write-Host ''
    Write-Host $Message
    Write-Host 'HuGuWeb development startup stopped. No unrelated processes were changed.'
    exit 1
}

function Get-CommandPath {
    param([string[]]$Names)
    foreach ($name in $Names) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($command -and $command.Source) {
            return $command.Source
        }
    }
    return $null
}

function Test-TcpPortOpen {
    param([string]$HostName, [int]$Port)
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $async = $client.BeginConnect($HostName, $Port, $null, $null)
        $ok = $async.AsyncWaitHandle.WaitOne(800)
        if (-not $ok) {
            return $false
        }
        $client.EndConnect($async)
        $client.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Get-HttpStatus {
    param([string]$Url)
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
        return [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            return [int]$_.Exception.Response.StatusCode
        }
        return $null
    }
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutSeconds,
        [string]$FailureMessage
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (& $Condition) {
            return
        }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    Stop-WithError $FailureMessage
}

function Resolve-ApiUrl {
    if (-not (Test-Path $LaunchSettings)) {
        return $script:ApiUrl
    }

    $json = Get-Content -Path $LaunchSettings -Raw | ConvertFrom-Json
    $url = $json.profiles.http.applicationUrl
    if ($url) {
        return $url.TrimEnd('/')
    }

    return $script:ApiUrl
}

function Resolve-FrontendUrl {
    if (Test-Path $ViteConfig) {
        $text = Get-Content -Path $ViteConfig -Raw
        if ($text -match 'port:\s*(\d+)') {
            return "http://localhost:$($Matches[1])"
        }
    }

    return $script:FrontendUrl
}

function Assert-DotNet {
    $dotnet = Get-CommandPath -Names @('dotnet')
    if (-not $dotnet) {
        Stop-WithError 'Missing prerequisite: .NET SDK 10. Install the .NET 10 SDK and reopen the terminal.'
    }

    $version = (& $dotnet --version).Trim()
    if ($version -notmatch '^10\.') {
        Stop-WithError "Missing prerequisite: .NET SDK 10. Found $version."
    }
}

function Assert-Node {
    $node = Get-CommandPath -Names @('node')
    if (-not $node) {
        Stop-WithError 'Missing prerequisite: Node.js 24. Install Node.js 24 LTS and reopen the terminal.'
    }

    $version = (& $node -v).Trim()
    if ($version -notmatch '^v24\.') {
        Stop-WithError "Missing prerequisite: Node.js 24. Found $version."
    }
}

function Resolve-NpmCmd {
    $fromPath = Get-CommandPath -Names @('npm.cmd')
    if ($fromPath) {
        return $fromPath
    }

    $known = Join-Path $env:ProgramFiles 'nodejs\npm.cmd'
    if (Test-Path $known) {
        return $known
    }

    return $null
}

function Assert-Npm {
    $script:NpmCmd = Resolve-NpmCmd
    if (-not $script:NpmCmd) {
        Stop-WithError 'Missing prerequisite: npm.cmd. Install Node.js 24 LTS (includes npm). PowerShell execution policy is not changed; npm.cmd is used instead of npm.ps1.'
    }
}

function Find-PostgresBinary {
    param([string]$FileName)

    $fromPath = Get-CommandPath -Names @($FileName)
    if ($fromPath) {
        return $fromPath
    }

    $known = Join-Path $env:ProgramFiles "PostgreSQL\18\bin\$FileName"
    if (Test-Path $known) {
        return $known
    }

    return $null
}

function Find-PostgresDataDirectory {
    $candidates = @()
    if ($env:PGDATA) {
        $candidates += $env:PGDATA
    }
    $candidates += @(
        (Join-Path $env:LOCALAPPDATA 'HuGuWeb\PostgreSQL\data'),
        (Join-Path $env:ProgramFiles 'PostgreSQL\18\data')
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path (Join-Path $candidate 'PG_VERSION'))) {
            return $candidate
        }
    }

    return $null
}

function Get-PostgresMajorVersion {
    foreach ($tool in @($script:PgCtl, $script:PgIsReady)) {
        if (-not $tool) {
            continue
        }
        $output = & $tool --version 2>$null
        if ($output -match '(\d+)') {
            return $Matches[1]
        }
    }
    return $null
}

function Assert-PostgresTools {
    $script:PgIsReady = Find-PostgresBinary -FileName 'pg_isready.exe'
    $script:PgCtl = Find-PostgresBinary -FileName 'pg_ctl.exe'

    if (-not $script:PgIsReady -and -not $script:PgCtl) {
        Stop-WithError @"
Missing prerequisite: PostgreSQL 18 tools (pg_isready / pg_ctl).
Looked in PATH and C:\Program Files\PostgreSQL\18\bin.
Install PostgreSQL 18, then reopen the terminal.
"@
    }

    $major = Get-PostgresMajorVersion
    if ($major -and $major -ne '18') {
        Stop-WithError "PostgreSQL 18 is required. Found major version $major."
    }
}

function Test-PostgresReady {
    if ($script:PgIsReady) {
        & $script:PgIsReady -h localhost -p 5432 | Out-Null
        return ($LASTEXITCODE -eq 0)
    }

    return (Test-TcpPortOpen -HostName 'localhost' -Port 5432)
}

function Start-HuGuWebPostgres {
    if (Test-PostgresReady) {
        Write-Step 'PostgreSQL' 'Ready'
        return
    }

    if (-not $script:PgCtl) {
        Stop-WithError 'PostgreSQL is not ready on localhost:5432, and pg_ctl.exe was not found to start the existing development cluster.'
    }

    $data = Find-PostgresDataDirectory
    if (-not $data) {
        Stop-WithError @"
PostgreSQL is not ready on localhost:5432, and no existing HuGuWeb development data directory was found.
Looked in:
  %LOCALAPPDATA%\HuGuWeb\PostgreSQL\data
  C:\Program Files\PostgreSQL\18\data
This launcher does not create a cluster, reset data, or change passwords.
"@
    }

    Write-Host "Starting existing PostgreSQL cluster: $data"
    & $script:PgCtl start -D $data -w -t 30 | Out-Host
    Wait-Until -TimeoutSeconds 40 -Condition { Test-PostgresReady } -FailureMessage @"
PostgreSQL did not become ready on localhost:5432.
Tried to start the existing cluster at:
  $data
The cluster was not created or reset. Check that PostgreSQL 18 is installed and that this data directory belongs to the HuGuWeb development cluster.
"@
    Write-Step 'PostgreSQL' 'Ready'
}

function Test-ApiLive {
    $status = Get-HttpStatus -Url "$($script:ApiUrl)/health"
    return $status -eq 200
}

function Test-ApiReady {
    $status = Get-HttpStatus -Url "$($script:ApiUrl)/health/ready"
    return $status -eq 200
}

function Test-FrontendReady {
    $status = Get-HttpStatus -Url $script:FrontendUrl
    return $status -eq 200
}

function Open-DevelopmentBrowser {
    param([string]$Url)

    $candidates = @(
        (Join-Path $env:ProgramFiles 'Google\Chrome\Application\chrome.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Google\Chrome\Application\chrome.exe'),
        (Join-Path $env:LOCALAPPDATA 'Google\Chrome\Application\chrome.exe')
    )

    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path)) {
            Start-Process -FilePath $path -ArgumentList $Url
            return
        }
    }

    Start-Process $Url
}

function Start-OwnedWindow {
    param(
        [string]$Title,
        [string]$CommandLine,
        [string]$WorkingDirectory
    )

    # cmd.exe /k must receive one command string. Start-Process quotes any
    # ArgumentList item that contains spaces. If that item already contains
    # quotes (for example a Program Files npm.cmd path), cmd.exe receives
    # broken quoting: a console can open without actually running Vite, while
    # a leftover process on 5173 still makes the readiness check succeed.
    # Keep $CommandLine free of nested quotes; call npm.cmd / dotnet by PATH
    # name so cmd.exe never invokes npm.ps1.
    $command = "title $Title && $CommandLine"
    return Start-Process -FilePath 'cmd.exe' -ArgumentList "/k `"$command`"" -WorkingDirectory $WorkingDirectory -PassThru
}

function Save-LauncherState {
    if (-not (Test-Path $StateDir)) {
        New-Item -ItemType Directory -Path $StateDir | Out-Null
    }

    $state = @{
        apiPid      = $script:StartedApiPid
        frontendPid = $script:StartedFrontendPid
        startedAt   = [DateTimeOffset]::Now.ToString('o')
        apiUrl      = $script:ApiUrl
        frontendUrl = $script:FrontendUrl
    }
    ($state | ConvertTo-Json) | Set-Content -Path $PidFile -Encoding UTF8
}

function Start-HuGuWebApi {
    if (Test-ApiLive) {
        Write-Step 'API' 'Ready'
        return
    }

    if (-not (Test-Path $ApiProject)) {
        Stop-WithError "API project was not found: $ApiProject"
    }

    $relativeProject = 'src\backend\HuGuWeb.Api\HuGuWeb.Api.csproj'
    $proc = Start-OwnedWindow -Title 'HuGuWeb API' -WorkingDirectory $RepoRoot -CommandLine "dotnet run --project $relativeProject --launch-profile http"
    $script:StartedApiPid = $proc.Id
    Save-LauncherState

    Wait-Until -TimeoutSeconds 90 -Condition { Test-ApiLive } -FailureMessage @"
The HuGuWeb API did not become ready at $($script:ApiUrl)/health.
A console window titled "HuGuWeb API" should contain the failure details.
"@
    Write-Step 'API' 'Ready'
}

function Assert-DatabaseReady {
    Wait-Until -TimeoutSeconds 45 -Condition { Test-ApiReady } -FailureMessage @"
The API is running, but $($script:ApiUrl)/health/ready did not return 200.
PostgreSQL may be up while Identity/Workforce databases are not migrated or the connection string is not configured.
See docs/engineering/LOCAL_DEVELOPMENT.md. Connection strings and passwords are not printed here.
"@
    Write-Step 'Database' 'Ready'
}

function Start-HuGuWebFrontend {
    if (Test-FrontendReady) {
        Write-Step 'Frontend' 'Ready'
        return
    }

    $nodeModules = Join-Path $FrontendDir 'node_modules'
    if (-not (Test-Path $nodeModules)) {
        Stop-WithError @"
Frontend dependencies are missing.
Run:
  cd src\frontend\web
  npm.cmd install
Then start again with .\dev.ps1.
"@
    }

    $proc = Start-OwnedWindow -Title 'HuGuWeb Frontend' -WorkingDirectory $FrontendDir -CommandLine 'npm.cmd run dev'
    $script:StartedFrontendPid = $proc.Id
    Save-LauncherState

    Wait-Until -TimeoutSeconds 60 -Condition { Test-FrontendReady } -FailureMessage @"
The Vite frontend did not become ready at $script:FrontendUrl.
A console window titled "HuGuWeb Frontend" should contain the failure details.
On Windows, this launcher uses npm.cmd to avoid PowerShell execution-policy issues with npm.ps1.
"@
    Write-Step 'Frontend' 'Ready'
}

if ($EnsurePostgres) {
    Write-Host 'HuGuWeb PostgreSQL check'
    Write-Host ''
    Assert-PostgresTools
    Start-HuGuWebPostgres
    Write-Host ''
    Write-Host 'PostgreSQL is ready on localhost:5432.'
    Write-Host 'Port 5432 is PostgreSQL, not an HTTP URL. Do not open it in Chrome.'
    exit 0
}

if ($StartVite) {
    $script:FrontendUrl = Resolve-FrontendUrl
    Write-Host 'HuGuWeb Vite'
    Write-Host ''

    if (Test-FrontendReady) {
        Write-Host "Vite already ready at $script:FrontendUrl"
        Write-Host 'localhost:5173'
        exit 0
    }

    if (Test-TcpPortOpen -HostName 'localhost' -Port 5173) {
        Stop-WithError @"
Port 5173 is in use but did not return HTTP 200.
Close the leftover process on that port, then press F5 again.
No processes were killed.
"@
    }

    Assert-Node
    Assert-Npm

    $nodeModules = Join-Path $FrontendDir 'node_modules'
    if (-not (Test-Path $nodeModules)) {
        Stop-WithError @"
Frontend dependencies are missing.
Run:
  cd src\frontend\web
  npm.cmd install
Then press F5 again.
"@
    }

    Set-Location $FrontendDir
    & $script:NpmCmd run dev
    exit $LASTEXITCODE
}

if ($WaitFrontend) {
    $script:FrontendUrl = Resolve-FrontendUrl
    Write-Host 'HuGuWeb Vite wait'
    Write-Host ''
    Wait-Until -TimeoutSeconds 60 -Condition { Test-FrontendReady } -FailureMessage @"
The Vite frontend did not become ready at $script:FrontendUrl.
F5 stopped before the API debugger. Inspect the Vite terminal.
On Windows, F5 starts Vite with npm.cmd to avoid PowerShell execution-policy issues with npm.ps1.
"@
    Write-Step 'Frontend' "Ready ($script:FrontendUrl)"
    exit 0
}

if ($StartChromeHealthWatcher) {
    $powershell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    Start-Process -FilePath $powershell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', (Join-Path $RepoRoot 'dev.ps1'),
        '-OpenChromeWhenHealthy'
    ) -WorkingDirectory $RepoRoot -WindowStyle Hidden
    Write-Host 'Chrome will open after http://localhost:5116/health returns 200.'
    exit 0
}

if ($OpenChromeWhenHealthy) {
    $script:ApiUrl = Resolve-ApiUrl
    $script:FrontendUrl = Resolve-FrontendUrl
    Wait-Until -TimeoutSeconds 90 -Condition { Test-ApiLive } -FailureMessage @"
The HuGuWeb API did not become ready at $($script:ApiUrl)/health.
Chrome was not opened.
"@
    Open-DevelopmentBrowser -Url $script:FrontendUrl
    Write-Step 'Chrome' $script:FrontendUrl
    exit 0
}

Write-Host 'HuGuWeb local development'
Write-Host ''

Assert-DotNet
Assert-Node
Assert-Npm
Assert-PostgresTools

$script:ApiUrl = Resolve-ApiUrl
$script:FrontendUrl = Resolve-FrontendUrl

Start-HuGuWebPostgres
Start-HuGuWebApi
Assert-DatabaseReady
Start-HuGuWebFrontend

Write-Host ''
Write-Host 'HuGuWeb development environment is ready.'
Write-Host ''
Write-Host 'Frontend:'
Write-Host $script:FrontendUrl
Write-Host ''
Write-Host 'API:'
Write-Host $script:ApiUrl
Write-Host ''
Write-Host 'PostgreSQL:'
Write-Host 'localhost:5432'
Write-Host ''
Write-Host 'PostgreSQL port 5432 is not an HTTP URL and is not opened in Chrome.'
Write-Host 'PostgreSQL is left running if it was already running, and is also left running if this launcher started it.'
Write-Host 'API and frontend run in separate consoles titled "HuGuWeb API" and "HuGuWeb Frontend".'
Write-Host 'Close those windows to stop them, or run .\dev-stop.ps1 to stop only processes started by this launcher.'
Write-Host 'This window can be closed. Ctrl+C here does not kill unrelated dotnet, Node, or PostgreSQL processes.'
Write-Host 'Preferred daily workflow: Cursor / VS Code F5 -> HuGuWeb Development.'
