[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$UnityEditorPath,
    [switch]$BuildWindows,
    [switch]$SmokeStartup
)
$ErrorActionPreference = 'Stop'
if ($SmokeStartup -and !$BuildWindows) { throw '-SmokeStartup requires -BuildWindows.' }
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unityExe = (Resolve-Path -LiteralPath $UnityEditorPath).Path
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ')
$logs = Join-Path $projectRoot "Logs/Validation/$runId"
$buildOutput = Join-Path $projectRoot "Builds/Validation/$runId"
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$expectedVersion = ((Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') |
    Select-String '^m_EditorVersion: ').Line -replace '^m_EditorVersion: ', '').Trim()
$actualVersion = (Get-Item -LiteralPath $unityExe).VersionInfo.ProductVersion
if ($actualVersion -notlike "$expectedVersion*") { throw "Expected Unity $expectedVersion; found $actualVersion" }
# Unity owns the project lock. Refuse before starting another process so an
# already-open Editor is never closed or modified by this script.
$lock = Join-Path $projectRoot 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lock) {
    try { $handle = [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None'); $handle.Dispose() }
    catch { throw 'Close this project in Unity before running batch validation.' }
}
function Invoke-Unity([string[]]$UnityArgs) {
    # Start-Process joins argument strings, so quote each path explicitly.
    $quoted = $UnityArgs | ForEach-Object {
        if ($_ -match '"') { throw 'Arguments containing quote characters are not supported.' }
        '"' + $_ + '"'
    }
    $process = Start-Process -FilePath $unityExe -ArgumentList $quoted -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See $logs" }
}
$record = [ordered]@{
    status = 'Running'; startedUtc = [DateTime]::UtcNow.ToString('O'); unityVersion = $expectedVersion
    commit = (& git -C $projectRoot rev-parse HEAD); workingTree = @(& git -C $projectRoot status --short)
    tests = $null; buildOutput = $null; restoredSourceFiles = @(); finishedUtc = $null
}
$sourceBackups = @()
try {
    # TMP clears dynamic atlas data on build AND Editor exit. Snapshot the
    # serialized fonts before either process starts, and restore after exit.
    # Texture sub-assets may precede the MonoBehaviour, so search the whole
    # asset without retaining or printing its texture payload.
    $sourcePaths = @(Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -File -Filter '*.asset' | Where-Object {
        Select-String -LiteralPath $_.FullName -SimpleMatch 'guid: 71c1514a6bd24e1e882cebbe1904ce04' -Quiet
    } | ForEach-Object FullName)
    $sourcePaths += @('ProjectSettings/ProjectSettings.asset', 'ProjectSettings/ScriptableBuildPipeline.json') |
        ForEach-Object { Join-Path $projectRoot $_ }
    $sourcePaths += @(Get-ChildItem (Join-Path $projectRoot 'Assets/AddressableAssetsData') -Recurse -File -Filter 'addressables_content_state.bin' |
        ForEach-Object FullName)
    foreach ($source in $sourcePaths | Sort-Object -Unique) {
        if (!(Test-Path -LiteralPath $source)) { continue }
        $relative = $source.Substring($projectRoot.Length + 1)
        $backup = Join-Path $logs "source-backup/$relative"
        New-Item -ItemType Directory -Path (Split-Path $backup) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $backup
        $sourceBackups += @{ source = $source; backup = $backup; relative = $relative }
    }
    $results = Join-Path $logs 'editmode.xml'
    Invoke-Unity @('-batchmode', '-projectPath', $projectRoot, '-buildTarget', 'Win64',
        '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'RPGSilent.Tests.Editor',
        '-testResults', $results, '-logFile', (Join-Path $logs 'editmode.log'))
    if (!(Test-Path -LiteralPath $results)) { throw 'Unity did not produce test results.' }
    [xml]$xml = Get-Content -LiteralPath $results -Raw
    $testRun = $xml.'test-run'
    $record.tests = @{ total = [int]$testRun.total; passed = [int]$testRun.passed; failed = [int]$testRun.failed; skipped = [int]$testRun.skipped }
    if ([int]$testRun.total -eq 0 -or [int]$testRun.passed -ne [int]$testRun.total -or $testRun.result -ne 'Passed') {
        throw "EditMode tests did not all pass. See $results"
    }
    if ($BuildWindows) {
        Invoke-Unity @('-batchmode', '-quit', '-projectPath', $projectRoot, '-buildTarget', 'Win64',
            '-executeMethod', 'RPGSilent.Editor.ProjectBuild.BuildWindows', '-rpgBuildOutput', $buildOutput,
            '-logFile', (Join-Path $logs 'build.log'))
        $build = Get-Content -LiteralPath (Join-Path $buildOutput 'build-report.json') -Raw | ConvertFrom-Json
        if ($build.status -ne 'Succeeded' -or $build.errors -ne 0 -or !(Test-Path -LiteralPath $build.output)) { throw 'Missing error-free successful build report/player.' }
        $record.buildOutput = $buildOutput
        # Content-update builds need the state belonging to this exact player.
        Get-ChildItem (Join-Path $projectRoot 'Assets/AddressableAssetsData') -Recurse -File -Filter 'addressables_content_state.bin' |
            ForEach-Object {
                $stateFolder = Join-Path $buildOutput ('AddressablesState/' + $_.Directory.Name)
                New-Item -ItemType Directory -Path $stateFolder -Force | Out-Null
                Copy-Item -LiteralPath $_.FullName -Destination $stateFolder
            }
        Get-ChildItem -LiteralPath $buildOutput -Recurse -File |
            Where-Object Name -ne 'sha256.json' | ForEach-Object {
                @{ path = $_.FullName.Substring($buildOutput.Length + 1).Replace('\', '/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
            } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $buildOutput 'sha256.json') -Encoding UTF8
        if ($SmokeStartup) {
            $playerLog = Join-Path $logs 'player-startup.log'
            $player = Start-Process -FilePath $build.output -ArgumentList @('-batchmode', '-logFile', ('"' + $playerLog + '"')) -PassThru -WindowStyle Hidden
            try {
                $earlyExit = $player.WaitForExit(30000)
                $playerText = Get-Content -LiteralPath $playerLog -Raw
                $startup = [ordered]@{
                    seconds = 30; earlyExit = $earlyExit
                    startMenuLoaded = $playerText.Contains('[StartUI] 已打开')
                    errorPatternFound = [bool]($playerText -match 'Exception:|Crash!!!|Failed to load|Error:')
                }
                $startup | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $logs 'player-startup.json') -Encoding UTF8
                if ($earlyExit -or !$startup.startMenuLoaded -or $startup.errorPatternFound) { throw 'Independent Player startup check failed.' }
            } finally { if (!$player.HasExited) { Stop-Process -InputObject $player } }
        }
    }
    $record.status = 'Passed'
}
catch { $record.status = 'Failed'; throw }
finally {
    foreach ($snapshot in $sourceBackups) {
        if (!(Test-Path -LiteralPath $snapshot.source) -or
            (Get-FileHash -LiteralPath $snapshot.source).Hash -ne (Get-FileHash -LiteralPath $snapshot.backup).Hash) {
            Copy-Item -LiteralPath $snapshot.backup -Destination $snapshot.source -Force
            $record.restoredSourceFiles += $snapshot.relative
        }
    }
    $record.finishedUtc = [DateTime]::UtcNow.ToString('O')
    $record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $logs 'validation.json') -Encoding UTF8
    Write-Host "Validation record: $logs"
}
