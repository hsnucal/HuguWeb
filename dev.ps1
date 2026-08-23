# Requires PowerShell 5.1 or later.
#
# Preferred daily workflow: Cursor / VS Code F5 -> "HuGuWeb Development".
# CLI fallback from the repository root (process-local Bypass; do not Set-ExecutionPolicy):
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\dev.ps1
# F5 task switches (process-local Bypass; do not Set-ExecutionPolicy):
#   -EnsurePostgres              PostgreSQL ready on 127.0.0.1:5432 (pg_isready)
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
$script:PgHost = '127.0.0.1'
$script:PgPort = 5432
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

function Test-SameFilePath {
    param([string]$Left, [string]$Right)

    if (-not $Left -or -not $Right) {
        return $false
    }

    try {
        $a = [System.IO.Path]::GetFullPath($Left.Trim()).TrimEnd('\', '/')
        $b = [System.IO.Path]::GetFullPath($Right.Trim()).TrimEnd('\', '/')
        return ($a -ieq $b)
    }
    catch {
        return ($Left.Trim().TrimEnd('\', '/') -ieq $Right.Trim().TrimEnd('\', '/'))
    }
}

function Get-PostgresDataCandidates {
    $candidates = @()
    if ($env:PGDATA) {
        $candidates += $env:PGDATA
    }
    $candidates += @(
        (Join-Path $env:LOCALAPPDATA 'HuGuWeb\PostgreSQL\data'),
        (Join-Path $env:ProgramFiles 'PostgreSQL\18\data')
    )
    return $candidates
}

function Test-PostgresDataDirectory {
    param([string]$Path)

    return ($Path -and (Test-Path (Join-Path $Path 'PG_VERSION')))
}

function Find-PostgresDataDirectory {
    # Deterministic existing-cluster discovery. Never prefer a missing path
    # and never initialize a new cluster.
    # 1. %PGDATA% when it already contains a cluster
    # 2. %LOCALAPPDATA%\HuGuWeb\PostgreSQL\data when that HuGuWeb cluster exists
    # 3. %ProgramFiles%\PostgreSQL\18\data when that installer cluster exists
    foreach ($candidate in (Get-PostgresDataCandidates)) {
        if (Test-PostgresDataDirectory -Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Get-PostgresDataSearchNotes {
    return @"
Looked in (a path is used only when it already contains PG_VERSION):
  %PGDATA%
  %LOCALAPPDATA%\HuGuWeb\PostgreSQL\data
  %ProgramFiles%\PostgreSQL\18\data
This launcher does not create a cluster, reset data, change passwords, or recreate databases.
"@
}

function Invoke-PostgresTool {
    param(
        [string]$Exe,
        [string[]]$Arguments,
        [switch]$NoCapture
    )

    # pg_ctl start/stop must not have stdout/stderr captured. The postmaster
    # inherits those handles and pg_ctl -w then hangs after the server is up.
    if ($NoCapture) {
        & $Exe @Arguments
        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output   = ''
        }
    }

    $output = & $Exe @Arguments 2>&1
    $code = $LASTEXITCODE
    $text = ($output | ForEach-Object { "$_" }) -join [Environment]::NewLine
    return [pscustomobject]@{
        ExitCode = $code
        Output   = $text.Trim()
    }
}

function Get-PostgresLogHint {
    param([string]$DataDirectory)

    if (-not $DataDirectory) {
        return $null
    }

    foreach ($relative in @('log', 'pg_log')) {
        $dir = Join-Path $DataDirectory $relative
        if (-not (Test-Path $dir)) {
            continue
        }

        $latest = Get-ChildItem -Path $dir -Filter '*.log' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if ($latest) {
            return $latest.FullName
        }

        return $dir
    }

    return $DataDirectory
}

function Get-PostgresFailureMessage {
    param(
        [string]$Message,
        [string]$DataDirectory
    )

    $log = Get-PostgresLogHint -DataDirectory $DataDirectory
    $block = $Message.TrimEnd()
    if ($DataDirectory) {
        $block += [Environment]::NewLine + "Cluster: $DataDirectory"
    }
    if ($log) {
        $block += [Environment]::NewLine + "PostgreSQL log: $log"
    }
    $block += [Environment]::NewLine + 'No postmaster.pid was deleted. No postgres.exe process was force-killed. No cluster or database was created or reset.'
    return $block
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

    if (-not $script:PgIsReady) {
        Stop-WithError @"
Missing prerequisite: PostgreSQL 18 pg_isready.exe.
Looked in PATH and %ProgramFiles%\PostgreSQL\18\bin.
Install PostgreSQL 18, then reopen the terminal.
A listening TCP port or a running postgres.exe process is not treated as ready.
"@
    }

    $major = Get-PostgresMajorVersion
    if ($major -and $major -ne '18') {
        Stop-WithError "PostgreSQL 18 is required. Found major version $major."
    }
}

function Get-PostgresReadyResult {
    if (-not $script:PgIsReady) {
        return [pscustomobject]@{
            Ready    = $false
            ExitCode = $null
            Output   = 'pg_isready.exe was not found.'
        }
    }

    $result = Invoke-PostgresTool -Exe $script:PgIsReady -Arguments @('-h', $script:PgHost, '-p', "$($script:PgPort)", '-t', '3')
    # Exit 0 is the documented "accepting connections" state. Do not require
    # English stdout; pg_isready messages follow the OS locale.
    return [pscustomobject]@{
        Ready    = ($result.ExitCode -eq 0)
        ExitCode = $result.ExitCode
        Output   = $result.Output
    }
}

function Test-PostgresReady {
    # pg_isready exit 0 is the only readiness signal. Process, pid file, and
    # TCP listeners are not treated as healthy.
    return [bool]((Get-PostgresReadyResult).Ready)
}

function Get-PostgresClusterStatus {
    param([string]$DataDirectory)

    $result = Invoke-PostgresTool -Exe $script:PgCtl -Arguments @('status', '-D', $DataDirectory)
    $pidMatch = [regex]::Match($result.Output, '\(PID:\s*(\d+)\)')
    $dataMatch = [regex]::Match($result.Output, '"-D"\s+"([^"]+)"')
    if (-not $dataMatch.Success) {
        $dataMatch = [regex]::Match($result.Output, '(?i)(?:^|[\s"])-D"?\s+"?([^"\r\n]+?)"?(?:\s|$)')
    }

    # pg_ctl status exit codes are locale-stable: 0 running, 3 not running.
    $state = 'unknown'
    if ($result.ExitCode -eq 3) {
        $state = 'not-running'
    }
    elseif ($result.ExitCode -eq 0) {
        $state = 'running'
    }

    $reportedData = $null
    if ($dataMatch.Success) {
        $reportedData = $dataMatch.Groups[1].Value.Trim().TrimEnd('\', '/')
    }

    $statusPid = $null
    if ($pidMatch.Success) {
        $statusPid = [int]$pidMatch.Groups[1].Value
    }
    elseif ($state -eq 'running') {
        $pidFile = Join-Path $DataDirectory 'postmaster.pid'
        if (Test-Path $pidFile) {
            $first = ([string](@(Get-Content -Path $pidFile -ErrorAction SilentlyContinue)[0])).Trim()
            $parsedPid = 0
            if ([int]::TryParse($first, [ref]$parsedPid) -and $parsedPid -gt 0) {
                $statusPid = $parsedPid
            }
        }
    }

    return [pscustomobject]@{
        State        = $state
        ExitCode     = $result.ExitCode
        Output       = $result.Output
        ProcessId    = $statusPid
        ReportedData = $reportedData
    }
}

function Test-ResolvedClusterOwnership {
    param(
        [string]$DataDirectory,
        [object]$Status
    )

    if ($Status.State -ne 'running' -or -not $Status.ProcessId) {
        return [pscustomobject]@{
            Confirmed = $false
            Reason    = 'pg_ctl status did not report a PID for the resolved data directory. Ownership is ambiguous. No cluster was stopped.'
        }
    }

    if ($Status.ReportedData -and -not (Test-SameFilePath -Left $Status.ReportedData -Right $DataDirectory)) {
        return [pscustomobject]@{
            Confirmed = $false
            Reason    = "pg_ctl status reported data directory '$($Status.ReportedData)', which is not the resolved cluster. Ownership is ambiguous. No cluster was stopped."
        }
    }

    $pidFile = Join-Path $DataDirectory 'postmaster.pid'
    if (-not (Test-Path $pidFile)) {
        return [pscustomobject]@{
            Confirmed = $false
            Reason    = 'postmaster.pid was not found in the resolved data directory while pg_ctl reported the server running. Ownership is ambiguous. The pid file was not deleted.'
        }
    }

    $lines = @(Get-Content -Path $pidFile -ErrorAction SilentlyContinue)
    if ($lines.Count -lt 1) {
        return [pscustomobject]@{
            Confirmed = $false
            Reason    = 'postmaster.pid in the resolved data directory could not be read. Ownership is ambiguous. The pid file was not deleted.'
        }
    }

    $filePidText = ([string]$lines[0]).Trim()
    $filePid = 0
    if (-not [int]::TryParse($filePidText, [ref]$filePid) -or $filePid -le 0) {
        return [pscustomobject]@{
            Confirmed = $false
            Reason    = 'postmaster.pid did not contain a usable PID. Ownership is ambiguous. The pid file was not deleted.'
        }
    }

    if ($filePid -ne $Status.ProcessId) {
        return [pscustomobject]@{
            Confirmed = $false
            Reason    = "pg_ctl status PID $($Status.ProcessId) does not match postmaster.pid PID $filePid. Ownership is ambiguous. No cluster was stopped."
        }
    }

    if ($lines.Count -ge 2 -and ([string]$lines[1]).Trim()) {
        $fileData = ([string]$lines[1]).Trim()
        if (-not (Test-SameFilePath -Left $fileData -Right $DataDirectory)) {
            return [pscustomobject]@{
                Confirmed = $false
                Reason    = "postmaster.pid reports data directory '$fileData', which is not the resolved cluster. Ownership is ambiguous. No cluster was stopped."
            }
        }
    }

    return [pscustomobject]@{
        Confirmed = $true
        Reason    = $null
    }
}

function Wait-PostgresReady {
    param(
        [string]$DataDirectory,
        [int]$TimeoutSeconds = 40
    )

    Wait-Until -TimeoutSeconds $TimeoutSeconds -Condition { Test-PostgresReady } -FailureMessage (
        Get-PostgresFailureMessage -DataDirectory $DataDirectory -Message @"
PostgreSQL did not become ready on $($script:PgHost):$($script:PgPort).
pg_isready must report accepting connections. A running process or a listening port is not enough.
"@
    )
}

function Invoke-PgCtlStart {
    param([string]$DataDirectory)

    # Detach start from captured consoles (F5 tasks pipe stdout). Do not use
    # Start-Process -Wait: Windows waits for the postgres child tree.
    # Do not use pg_ctl -w under a redirected console: the postmaster can
    # inherit those handles and -w hangs after the server is already up.
    # -l appends to the existing cluster log; postgresql.conf is not modified.
    $logArg = ''
    $logDir = Join-Path $DataDirectory 'log'
    if (Test-Path $logDir) {
        $logFile = Join-Path $logDir 'postgresql.log'
        $logArg = " -l `"$logFile`""
    }

    $argumentList = "start -D `"$DataDirectory`"$logArg"
    $proc = Start-Process -FilePath $script:PgCtl -ArgumentList $argumentList -PassThru -WindowStyle Hidden
    if (-not $proc) {
        return [pscustomobject]@{
            ExitCode = 1
            Output   = 'pg_ctl start did not start.'
        }
    }

    $deadline = (Get-Date).AddSeconds(20)
    do {
        if ($proc.HasExited) {
            break
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)

    if (-not $proc.HasExited) {
        return [pscustomobject]@{
            ExitCode = 1
            Output   = 'pg_ctl start did not exit within 20 seconds. The existing cluster was not force-killed.'
        }
    }

    return [pscustomobject]@{
        ExitCode = $proc.ExitCode
        Output   = ''
    }
}

function Start-ExistingPostgresCluster {
    param([string]$DataDirectory)

    $result = Invoke-PgCtlStart -DataDirectory $DataDirectory
    if ($result.ExitCode -ne 0) {
        $detail = $result.Output
        if (-not $detail) {
            $detail = "pg_ctl start exited with code $($result.ExitCode)."
        }
        Stop-WithError (Get-PostgresFailureMessage -DataDirectory $DataDirectory -Message @"
Failed to start the existing PostgreSQL cluster.
$detail
"@)
    }

    Wait-PostgresReady -DataDirectory $DataDirectory
}

function Restart-ExistingPostgresCluster {
    param([string]$DataDirectory)

    $stop = Invoke-PostgresTool -Exe $script:PgCtl -Arguments @('stop', '-D', $DataDirectory, '-m', 'fast', '-w', '-t', '30') -NoCapture
    if ($stop.ExitCode -ne 0) {
        $detail = $stop.Output
        if (-not $detail) {
            $detail = "pg_ctl stop -m fast exited with code $($stop.ExitCode)."
        }
        Stop-WithError (Get-PostgresFailureMessage -DataDirectory $DataDirectory -Message @"
Graceful PostgreSQL stop failed for the resolved cluster.
$detail
No force kill was attempted. Fix the existing cluster, then press F5 again.
"@)
    }

    $deadline = (Get-Date).AddSeconds(15)
    $stopped = $false
    do {
        $afterStop = Get-PostgresClusterStatus -DataDirectory $DataDirectory
        if ($afterStop.State -eq 'not-running') {
            $stopped = $true
            break
        }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    if (-not $stopped) {
        Stop-WithError (Get-PostgresFailureMessage -DataDirectory $DataDirectory -Message @"
Graceful PostgreSQL stop did not leave the resolved cluster in a not-running state.
No force kill was attempted. Fix the existing cluster, then press F5 again.
"@)
    }

    Start-ExistingPostgresCluster -DataDirectory $DataDirectory
}

function Start-HuGuWebPostgres {
    if (Test-PostgresReady) {
        Write-Step 'PostgreSQL' 'Ready'
        return
    }

    Write-Step 'PostgreSQL' 'Not ready'

    if (-not $script:PgCtl) {
        Stop-WithError @"
PostgreSQL is not ready on $($script:PgHost):$($script:PgPort), and pg_ctl.exe was not found to start the existing development cluster.
Looked in PATH and %ProgramFiles%\PostgreSQL\18\bin.
"@
    }

    $data = Find-PostgresDataDirectory
    if (-not $data) {
        Stop-WithError @"
PostgreSQL is not ready on $($script:PgHost):$($script:PgPort), and no existing development data directory was found.
$(Get-PostgresDataSearchNotes)
"@
    }

    Write-Step 'Cluster' $data

    $status = Get-PostgresClusterStatus -DataDirectory $data
    if ($status.State -eq 'not-running') {
        Write-Step 'Status' 'Not running'
        Write-Step 'Action' 'Starting existing cluster'
        Start-ExistingPostgresCluster -DataDirectory $data
        Write-Step 'PostgreSQL' 'Ready'
        return
    }

    if ($status.State -eq 'running') {
        $owner = Test-ResolvedClusterOwnership -DataDirectory $data -Status $status
        if (-not $owner.Confirmed) {
            Stop-WithError (Get-PostgresFailureMessage -DataDirectory $data -Message @"
PostgreSQL is running but not accepting connections, and cluster ownership could not be confirmed.
$($owner.Reason)
"@)
        }

        Write-Step 'Status' 'Running but unhealthy'
        Write-Step 'Action' 'Restarting existing cluster'
        Restart-ExistingPostgresCluster -DataDirectory $data
        Write-Step 'PostgreSQL' 'Ready'
        return
    }

    Stop-WithError (Get-PostgresFailureMessage -DataDirectory $data -Message @"
PostgreSQL is not accepting connections, and pg_ctl status for the resolved cluster was ambiguous.
$($status.Output)
No cluster was started or stopped.
"@)
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
    Write-Host 'PostgreSQL is ready on 127.0.0.1:5432 (pg_isready accepting connections).'
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
Write-Host '127.0.0.1:5432'
Write-Host ''
Write-Host 'PostgreSQL port 5432 is not an HTTP URL and is not opened in Chrome.'
Write-Host 'PostgreSQL is left running if it was already running, and is also left running if this launcher started it.'
Write-Host 'API and frontend run in separate consoles titled "HuGuWeb API" and "HuGuWeb Frontend".'
Write-Host 'Close those windows to stop them, or run .\dev-stop.ps1 to stop only processes started by this launcher.'
Write-Host 'This window can be closed. Ctrl+C here does not kill unrelated dotnet, Node, or PostgreSQL processes.'
Write-Host 'Preferred daily workflow: Cursor / VS Code F5 -> HuGuWeb Development.'
