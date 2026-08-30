#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ReleaseSha,

    [string]$RepositoryRoot = "",
    [string]$EnvironmentFile = "",
    [string]$ComposeProject = "sipacul-production",
    [string]$RegistryOwner = "rizkyerapee-bit",
    [string]$StateDirectory = "",
    [string]$BackupOutputDirectory = "",
    [int]$HealthTimeoutSeconds = 180,

    [switch]$Execute,
    [switch]$AllowInitialDeploymentWithoutBackup
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Revision = "Sprint 20D2G2 Deployment Contract 1 - PowerShell 5.1"
$modulePath = Join-Path $PSScriptRoot "SiPaculDeployment.psm1"
$pendingFile = $null
$stage = "preflight"

try {
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
        throw "Deployment module tidak ditemukan: $modulePath"
    }

    Import-Module -Name $modulePath -Force -ErrorAction Stop

    Write-Host "=== PREFLIGHT DEPLOYMENT SIPACUL ==="

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

    $targetSha = Normalize-SiPaculReleaseSha -ReleaseSha $ReleaseSha
    $targetImages = Get-SiPaculReleaseImageMap `
        -ReleaseSha $targetSha `
        -RegistryOwner $RegistryOwner

    $currentFile = Join-Path $StateDirectory "current-deployment.json"
    $pendingFile = Join-Path $StateDirectory "pending-operation.json"
    $releaseEnvironmentFile = Join-Path $StateDirectory "current-release.env"

    if (Test-Path -LiteralPath $pendingFile -PathType Leaf) {
        throw "Pending deployment operation ditemukan: $pendingFile. Selesaikan investigasi sebelum deployment baru."
    }

    $state = Get-SiPaculDeploymentState -StateFile $currentFile
    $containers = @(Get-SiPaculProjectContainerIds -ComposeProject $ComposeProject)
    $initial = ($null -eq $state)

    if ($initial -and $containers.Count -gt 0) {
        throw "Container project $ComposeProject ada tetapi deployment state belum tersedia. Adopsi unmanaged stack ditolak."
    }

    if (-not $initial) {
        if ([string]$state.composeProject -cne $ComposeProject) {
            throw "ComposeProject tidak cocok dengan deployment state."
        }

        if ([string]$state.registryOwner -cne $RegistryOwner.ToLowerInvariant()) {
            throw "RegistryOwner tidak cocok dengan deployment state."
        }

        Assert-SiPaculReleaseEnvironmentMatchesState `
            -ReleaseEnvironmentFile $releaseEnvironmentFile `
            -State $state

        if ([string]$state.databaseReleaseSha -eq $targetSha -and
            [string]$state.runtimeReleaseSha -eq $targetSha) {
            throw "Target release $targetSha sudah aktif sebagai database dan runtime release."
        }
    }

    Invoke-SiPaculComposeConfigWithImageMap `
        -EnvironmentFile $EnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -ImageMap $targetImages

    Write-Host "[OK] Repository: $root"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Target full SHA: $targetSha"
    Write-Host "[OK] Compose config target release valid."
    Write-Host "[OK] State directory: $StateDirectory"
    Write-Host "[OK] Backup directory: $BackupOutputDirectory"

    if ($initial) {
        Write-Host "[INFO] Deployment state belum ada; ini adalah initial managed deployment."
    }
    else {
        Write-Host "[INFO] Database release saat ini: $($state.databaseReleaseSha)"
        Write-Host "[INFO] Runtime release saat ini: $($state.runtimeReleaseSha)"
    }

    Write-Host ""
    Write-Host "=== DEPLOYMENT PLAN ==="
    Write-Host "1. Pull dan verifikasi revision label empat immutable GHCR image."
    if ($initial) {
        Write-Host "2. Initial deployment tidak memiliki database existing untuk dibackup."
    }
    else {
        Write-Host "2. Pastikan PostgreSQL sehat lalu buat backup dengan Backup-SiPaculPostgres-PS51.ps1."
    }
    Write-Host "3. Tulis pending operation dan release environment di luar repository."
    Write-Host "4. Hentikan edge/frontend/API; PostgreSQL dan volume tetap dipertahankan."
    Write-Host "5. Jalankan migrator target sebagai migration gate."
    Write-Host "6. Mulai API, frontend, lalu edge dan tunggu health check."
    Write-Host "7. Simpan current deployment state dan history; hapus pending operation."
    Write-Host "[INFO] Tidak ada restore database atau rollback otomatis pada jalur deployment."

    if (-not $Execute) {
        Write-Host ""
        Write-Host "=== STATUS AKHIR PLAN ==="
        Write-Host "[OK] Plan-only selesai; Docker runtime, image cache, database, env release, dan state tidak diubah."
        Write-Host "[OK] Jalankan ulang dengan -Execute setelah plan disetujui."
        exit 0
    }

    if ($initial -and -not $AllowInitialDeploymentWithoutBackup) {
        throw "Initial deployment tidak memiliki backup sumber. Gunakan -AllowInitialDeploymentWithoutBackup hanya setelah memastikan ini benar-benar instalasi baru."
    }

    Write-Host ""
    Write-Host "=== PULL IMMUTABLE RELEASE IMAGES ==="
    $stage = "pull-images"

    foreach ($pair in @(
        @($targetImages.SIPACUL_MIGRATOR_IMAGE, "Migrator"),
        @($targetImages.SIPACUL_API_IMAGE, "API"),
        @($targetImages.SIPACUL_FRONTEND_IMAGE, "Frontend"),
        @($targetImages.SIPACUL_EDGE_IMAGE, "Edge")
    )) {
        Write-Host "[INFO] Pull $($pair[1]): $($pair[0])"
        Assert-SiPaculImageRevision `
            -Image ([string]$pair[0]) `
            -ExpectedReleaseSha $targetSha
        Write-Host "[OK] $($pair[1]) image revision cocok."
    }

    if (-not (Test-Path -LiteralPath $StateDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $StateDirectory -Force | Out-Null
    }

    $backupFile = $null

    if (-not $initial) {
        Write-Host ""
        Write-Host "=== PRE-DEPLOY BACKUP ==="
        $stage = "pre-deploy-backup"

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

        Write-Host "[OK] PostgreSQL sehat."

        $backupFile = Invoke-SiPaculPreDeployBackup `
            -RepositoryRoot $root `
            -EnvironmentFile $EnvironmentFile `
            -ComposeProject $ComposeProject `
            -BackupOutputDirectory $BackupOutputDirectory

        Write-Host "[OK] Backup pre-deploy: $backupFile"
    }

    $previousRuntimeSha = if ($initial) { $null } else { [string]$state.runtimeReleaseSha }

    $pending = [ordered]@{
        schemaVersion = 1
        application = "SiPacul"
        operation = "deploy"
        status = "in-progress"
        stage = "prepared"
        startedAtUtc = [DateTime]::UtcNow.ToString("o")
        targetReleaseSha = $targetSha
        previousDatabaseReleaseSha = if ($initial) { $null } else { [string]$state.databaseReleaseSha }
        previousRuntimeReleaseSha = $previousRuntimeSha
        backupFile = $backupFile
        composeProject = $ComposeProject
        registryOwner = $RegistryOwner.ToLowerInvariant()
    }

    Write-SiPaculJsonFile -Path $pendingFile -Value $pending

    Write-SiPaculReleaseEnvironment `
        -Path $releaseEnvironmentFile `
        -ImageMap $targetImages `
        -DatabaseReleaseSha $targetSha `
        -RuntimeReleaseSha $targetSha

    Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Arguments @("config", "--quiet") | Out-Null

    Write-Host "[OK] Release environment target aktif dan Compose config valid."

    Write-Host ""
    Write-Host "=== POSTGRESQL ==="
    $stage = "postgres"

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

    Write-Host "[OK] PostgreSQL siap; volume database tetap dipertahankan."

    Write-Host ""
    Write-Host "=== MAINTENANCE / MIGRATION GATE ==="
    $stage = "stop-runtime"

    Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Arguments @("stop", "edge", "frontend", "api") `
        -AllowFailure | Out-Null

    Write-Host "[OK] Runtime application dihentikan; PostgreSQL tetap berjalan."

    $stage = "migration"

    Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Arguments @("rm", "--force", "--stop", "migrator") `
        -AllowFailure | Out-Null

    Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $releaseEnvironmentFile `
        -ComposeFile $composeFile `
        -ComposeProject $ComposeProject `
        -Arguments @(
            "up",
            "--no-deps",
            "--no-build",
            "--abort-on-container-exit",
            "--exit-code-from", "migrator",
            "migrator"
        ) | Out-Null

    Write-Host "[OK] Migration gate selesai dengan exit code 0."

    Write-Host ""
    Write-Host "=== START TARGET RUNTIME ==="

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

        Write-Host "[OK] $service sehat."
    }

    $stage = "finalize"

    $newState = [ordered]@{
        schemaVersion = 1
        application = "SiPacul"
        status = "deployed"
        databaseReleaseSha = $targetSha
        runtimeReleaseSha = $targetSha
        previousRuntimeReleaseSha = $previousRuntimeSha
        registryOwner = $RegistryOwner.ToLowerInvariant()
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
        -Operation "deploy"

    Remove-Item -LiteralPath $pendingFile -Force

    Write-Host ""
    Write-Host "=== STATUS AKHIR DEPLOYMENT ==="
    Write-Host "[OK] Database release: $targetSha"
    Write-Host "[OK] Runtime release: $targetSha"
    Write-Host "[OK] Deployment state: $currentFile"
    Write-Host "[OK] Deployment history: $historyPath"
    if ($null -ne $backupFile) {
        Write-Host "[OK] Backup pre-deploy dipertahankan: $backupFile"
    }
    Write-Host "[OK] Tidak ada database restore, volume deletion, atau rollback otomatis."
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
    Write-Host "[AMAN] Tidak ada restore database atau schema downgrade otomatis." -ForegroundColor Yellow
    Write-Host "[AMAN] Jika migration sudah dimulai, jangan menjalankan rollback aplikasi sebelum kompatibilitas schema dikonfirmasi." -ForegroundColor Yellow
    if ($null -ne $pendingFile) {
        Write-Host "[INFO] Pending operation: $pendingFile"
    }
    exit 1
}
