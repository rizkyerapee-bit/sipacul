#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$EnvironmentFile = "",
    [string]$ComposeProject = "sipacul-production",
    [string]$StateDirectory = "",
    [string]$BackupOutputDirectory = "",
    [int]$HealthTimeoutSeconds = 180,

    [switch]$Execute,
    [switch]$AcknowledgeDatabaseCompatibility
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Revision = "Sprint 20D2G2 Application Rollback 1 - PowerShell 5.1"
$modulePath = Join-Path $PSScriptRoot "SiPaculDeployment.psm1"
$pendingFile = $null
$stage = "preflight"

try {
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
        throw "Deployment module tidak ditemukan: $modulePath"
    }

    Import-Module -Name $modulePath -Force -ErrorAction Stop

    Write-Host "=== PREFLIGHT APPLICATION ROLLBACK SIPACUL ==="

    $root = Resolve-SiPaculRepositoryRoot -RepositoryRoot $RepositoryRoot
    Assert-SiPaculGitClean -RepositoryRoot $root
    $composeFile = Join-Path $root "compose.production.yml"

    if ([string]::IsNullOrWhiteSpace($EnvironmentFile)) {
        $EnvironmentFile = Join-Path $root ".env.production"
    }
    $EnvironmentFile = Resolve-SiPaculFilePath `
        -Path $EnvironmentFile `
        -BaseDirectory $root `
        -Label "Production environment"

    if ([string]::IsNullOrWhiteSpace($StateDirectory)) {
        $StateDirectory = Join-Path $env:USERPROFILE "SiPaculDeploymentState"
    }
    $StateDirectory = Resolve-SiPaculDirectoryPath `
        -Path $StateDirectory `
        -BaseDirectory $root
    Assert-SiPaculOutsideRepository `
        -RepositoryRoot $root `
        -Path $StateDirectory `
        -Label "StateDirectory"

    if ([string]::IsNullOrWhiteSpace($BackupOutputDirectory)) {
        $BackupOutputDirectory = Join-Path $env:USERPROFILE "SiPaculBackups"
    }
    $BackupOutputDirectory = Resolve-SiPaculDirectoryPath `
        -Path $BackupOutputDirectory `
        -BaseDirectory $root
    Assert-SiPaculOutsideRepository `
        -RepositoryRoot $root `
        -Path $BackupOutputDirectory `
        -Label "BackupOutputDirectory"

    [void](Assert-SiPaculProductionEnvironment -EnvironmentFile $EnvironmentFile)

    $currentFile = Join-Path $StateDirectory "current-deployment.json"
    $pendingFile = Join-Path $StateDirectory "pending-operation.json"
    $releaseEnvironmentFile = Join-Path $StateDirectory "current-release.env"

    if (Test-Path -LiteralPath $pendingFile -PathType Leaf) {
        throw "Pending deployment operation ditemukan: $pendingFile. Rollback ditolak sampai operasi tersebut diinvestigasi."
    }

    $state = Get-SiPaculDeploymentState -StateFile $currentFile
    if ($null -eq $state) {
        throw "Managed deployment state tidak ditemukan: $currentFile"
    }

    if ([string]$state.composeProject -cne $ComposeProject) {
        throw "ComposeProject tidak cocok dengan deployment state."
    }

    Assert-SiPaculReleaseEnvironmentMatchesState `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -State $state

    $databaseSha = Normalize-SiPaculReleaseSha -ReleaseSha ([string]$state.databaseReleaseSha)
    $runtimeSha = Normalize-SiPaculReleaseSha -ReleaseSha ([string]$state.runtimeReleaseSha)
    $targetSha = [string]$state.previousRuntimeReleaseSha

    if ([string]::IsNullOrWhiteSpace($targetSha)) {
        throw "previousRuntimeReleaseSha tidak tersedia; application rollback tidak memiliki target."
    }

    $targetSha = Normalize-SiPaculReleaseSha -ReleaseSha $targetSha
    if ($targetSha -eq $runtimeSha) {
        throw "Target rollback sama dengan runtime release aktif."
    }

    $targetRuntimeImages = Get-SiPaculSplitReleaseImageMap `
        -DatabaseReleaseSha $databaseSha `
        -RuntimeReleaseSha $targetSha `
        -RegistryOwner ([string]$state.registryOwner)

    Invoke-SiPaculComposeConfigWithImageMap `
        -EnvironmentFile $EnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -ImageMap $targetRuntimeImages

    Write-Host "[OK] Repository: $root"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Database release tetap: $databaseSha"
    Write-Host "[OK] Runtime release aktif: $runtimeSha"
    Write-Host "[OK] Target runtime rollback: $targetSha"
    Write-Host "[OK] Migrator tetap menunjuk database release $databaseSha dan tidak akan dijalankan."

    Write-Host ""
    Write-Host "=== ROLLBACK PLAN ==="
    Write-Host "1. Pull dan verifikasi API/frontend/edge target rollback."
    Write-Host "2. Pastikan PostgreSQL sehat dan buat backup sebelum switch runtime."
    Write-Host "3. Hentikan edge/frontend/API."
    Write-Host "4. Pertahankan migrator pada database release; hanya API/frontend/edge diganti."
    Write-Host "5. Mulai API, frontend, edge dengan --no-deps lalu tunggu health check."
    Write-Host "6. Simpan application-rollback state dan history."
    Write-Host "[INFO] Rollback tidak menjalankan migrator dan tidak melakukan restore database."

    if (-not $Execute) {
        Write-Host ""
        Write-Host "=== STATUS AKHIR PLAN ==="
        Write-Host "[OK] Plan-only selesai; runtime, database, image cache, release env, dan state tidak diubah."
        Write-Host "[OK] Execute membutuhkan -Execute -AcknowledgeDatabaseCompatibility."
        exit 0
    }

    if (-not $AcknowledgeDatabaseCompatibility) {
        throw "Application rollback memerlukan -AcknowledgeDatabaseCompatibility karena schema database tidak diturunkan."
    }

    Write-Host ""
    Write-Host "=== PULL ROLLBACK RUNTIME IMAGES ==="
    $stage = "pull-images"

    foreach ($pair in @(
        @($targetRuntimeImages.SIPACUL_API_IMAGE, "API"),
        @($targetRuntimeImages.SIPACUL_FRONTEND_IMAGE, "Frontend"),
        @($targetRuntimeImages.SIPACUL_EDGE_IMAGE, "Edge")
    )) {
        Write-Host "[INFO] Pull $($pair[1]): $($pair[0])"
        Assert-SiPaculImageRevision `
            -Image ([string]$pair[0]) `
            -ExpectedReleaseSha $targetSha
        Write-Host "[OK] $($pair[1]) image revision cocok."
    }

    Write-Host ""
    Write-Host "=== PRE-ROLLBACK BACKUP ==="
    $stage = "pre-rollback-backup"

    Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Arguments @("up", "--detach", "--no-build", "postgres") | Out-Null

    Wait-SiPaculComposeServiceHealthy `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Service "postgres" `
        -TimeoutSeconds $HealthTimeoutSeconds

    $backupFile = Invoke-SiPaculPreDeployBackup `
        -RepositoryRoot $root `
        -EnvironmentFile $EnvironmentFile `
        -ComposeProject $ComposeProject `
        -BackupOutputDirectory $BackupOutputDirectory

    Write-Host "[OK] Backup pre-rollback: $backupFile"

    $pending = [ordered]@{
        schemaVersion = 1
        application = "SiPacul"
        operation = "application-rollback"
        status = "in-progress"
        stage = "prepared"
        startedAtUtc = [DateTime]::UtcNow.ToString("o")
        databaseReleaseSha = $databaseSha
        fromRuntimeReleaseSha = $runtimeSha
        targetRuntimeReleaseSha = $targetSha
        backupFile = $backupFile
        composeProject = $ComposeProject
        registryOwner = [string]$state.registryOwner
    }

    Write-SiPaculJsonFile -Path $pendingFile -Value $pending

    Write-Host ""
    Write-Host "=== SWITCH RUNTIME ONLY ==="
    $stage = "stop-runtime"

    Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Arguments @("stop", "edge", "frontend", "api") | Out-Null

    Write-SiPaculReleaseEnvironment `
        -Path $releaseEnvironmentFile `
        -ImageMap $targetRuntimeImages `
        -DatabaseReleaseSha $databaseSha `
        -RuntimeReleaseSha $targetSha

    Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Arguments @("config", "--quiet") | Out-Null

    Write-Host "[OK] Release environment rollback aktif; migrator tetap $databaseSha."

    foreach ($service in @("api", "frontend", "edge")) {
        $stage = "start-$service"

        Invoke-SiPaculCompose `
            -EnvironmentFile $EnvironmentFile `
            -ReleaseEnvironmentFile $releaseEnvironmentFile `
            -ComposeFile $composeFile `
            -ComposeProject $ComposeProject `
            -Arguments @("up", "--detach", "--no-build", "--no-deps", $service) | Out-Null

        Wait-SiPaculComposeServiceHealthy `
            -EnvironmentFile $EnvironmentFile `
            -ReleaseEnvironmentFile $releaseEnvironmentFile `
            -ComposeFile $composeFile `
            -ComposeProject $ComposeProject `
            -Service $service `
            -TimeoutSeconds $HealthTimeoutSeconds

        Write-Host "[OK] $service sehat pada runtime rollback."
    }

    $stage = "finalize"

    $newState = [ordered]@{
        schemaVersion = 1
        application = "SiPacul"
        status = "application-rollback"
        databaseReleaseSha = $databaseSha
        runtimeReleaseSha = $targetSha
        previousRuntimeReleaseSha = $runtimeSha
        registryOwner = [string]$state.registryOwner
        composeProject = $ComposeProject
        environmentFile = $EnvironmentFile
        releaseEnvironmentFile = $releaseEnvironmentFile
        backupFile = $backupFile
        deployedAtUtc = [DateTime]::UtcNow.ToString("o")
    }

    Write-SiPaculJsonFile -Path $currentFile -Value $newState
    $historyPath = Write-SiPaculDeploymentHistory `
        -StateDirectory $StateDirectory `
        -State $newState `
        -Operation "application-rollback"

    Remove-Item -LiteralPath $pendingFile -Force

    Write-Host ""
    Write-Host "=== STATUS AKHIR APPLICATION ROLLBACK ==="
    Write-Host "[OK] Database release tetap: $databaseSha"
    Write-Host "[OK] Runtime release sekarang: $targetSha"
    Write-Host "[OK] Previous runtime sekarang: $runtimeSha"
    Write-Host "[OK] Backup pre-rollback dipertahankan: $backupFile"
    Write-Host "[OK] Rollback history: $historyPath"
    Write-Host "[OK] Migrator tidak dijalankan; database tidak direstore atau didowngrade."
}
catch {
    $message = $_.Exception.Message

    if ($null -ne $pendingFile -and (Test-Path -LiteralPath $pendingFile -PathType Leaf)) {
        try {
            $pendingState = [IO.File]::ReadAllText($pendingFile) | ConvertFrom-Json
            $pendingState.status = "failed"
            $pendingState.stage = $stage
            $pendingState | Add-Member -NotePropertyName failedAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString("o")) -Force
            $pendingState | Add-Member -NotePropertyName failureMessage -NotePropertyValue $message -Force
            Write-SiPaculJsonFile -Path $pendingFile -Value $pendingState
        }
        catch {
        }
    }

    Write-Host ""
    Write-Host "[GAGAL] $message" -ForegroundColor Red
    Write-Host "[AMAN] Application rollback tidak pernah menjalankan migrator atau restore database." -ForegroundColor Yellow
    if ($null -ne $pendingFile) {
        Write-Host "[INFO] Pending operation: $pendingFile"
    }
    exit 1
}
