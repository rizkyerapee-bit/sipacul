param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$ExpectedLatestMigration = "",
    [string]$PostgresImage = "postgres:17-alpine",
    [string]$ContainerName = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C1 PostgreSQL Restore Drill 1 - PowerShell 5.1"
$DevContainer = "sipacul-postgres-dev"
$containerCreated = $false
$completed = $false
$failureMessage = $null
$cleanupMessage = $null
$temporaryVolumeNames = @()

function Fail([string]$Message) { throw $Message }

function Run-Docker([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& docker.exe @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) {
        Fail ("docker.exe gagal: " + (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Invoke-Docker([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & docker.exe @Arguments
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) {
        Fail ("docker.exe " + ($Arguments -join " ") + " gagal dengan exit code $exitCode.")
    }
}

function Run-DockerWithInput([string]$InputText, [string[]]$Arguments) {
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @($InputText | & docker.exe @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) {
        Fail ("docker.exe gagal: " + (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Get-OneLine([string[]]$Output, [string]$Label) {
    $values = @($Output |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($values.Count -ne 1) { Fail "$Label harus menghasilkan tepat satu nilai." }
    return [string]$values[0]
}

function Test-ExactContainer([string]$Name) {
    $names = @(Run-Docker @("ps", "--all", "--filter", "name=^/$Name$", "--format", "{{.Names}}"))
    return ($names -contains $Name)
}

function Get-ContainerSignature([string]$Name) {
    if (-not (Test-ExactContainer $Name)) { return "<absent>" }
    return Get-OneLine (Run-Docker @(
        "inspect", "--format", "{{.Id}}|{{.State.Status}}|{{.State.StartedAt}}", $Name
    )) "Signature container $Name"
}

function Wait-PostgresReady([string]$Name, [string]$User, [string]$Database, [int]$Seconds) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        $previous = $ErrorActionPreference
        try {
            $ErrorActionPreference = "Continue"
            & docker.exe exec $Name pg_isready -U $User -d $Database 1>$null 2>$null
            $readyExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $previous }
        if ($readyExit -eq 0) { return }

        $state = Get-OneLine (Run-Docker @("inspect", "--format", "{{.State.Status}}|{{.State.ExitCode}}", $Name)) "State restore container"
        if ($state -match "^(exited|dead)\|") { Fail "PostgreSQL restore drill berhenti: $state" }
        Start-Sleep -Seconds 1
    } while ([DateTime]::UtcNow -lt $deadline)
    Fail "PostgreSQL restore drill tidak siap dalam $Seconds detik."
}

function Run-PsqlScalar([string]$Sql, [string]$Name, [string]$User, [string]$Database, [string]$Label) {
    return Get-OneLine (Run-DockerWithInput $Sql @(
        "exec", "--interactive", $Name,
        "psql", "-X", "-v", "ON_ERROR_STOP=1",
        "-U", $User, "-d", $Database, "-tA"
    )) $Label
}

try {
    Write-Host "=== PREFLIGHT RESTORE DRILL SIPACUL ==="
    if (-not (Get-Command docker.exe -ErrorAction SilentlyContinue)) { Fail "docker.exe tidak ditemukan." }
    Invoke-Docker @("info") 1>$null

    $backupPath = [IO.Path]::GetFullPath($BackupFile)
    $checksumPath = $backupPath + ".sha256"
    $manifestPath = $backupPath + ".json"
    if (-not (Test-Path -LiteralPath $backupPath -PathType Leaf)) { Fail "Backup tidak ditemukan: $backupPath" }
    if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { Fail "Checksum tidak ditemukan: $checksumPath" }
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { Fail "Manifest tidak ditemukan: $manifestPath" }

    $checksumLine = [IO.File]::ReadAllText($checksumPath).Trim()
    if ($checksumLine -notmatch '^([0-9A-Fa-f]{64})\s+\*?(.+)$') { Fail "Format file SHA256 tidak valid." }
    $sidecarHash = $Matches[1].ToUpperInvariant()
    $sidecarName = $Matches[2].Trim()
    $actualName = Split-Path -Leaf $backupPath
    if ($sidecarName -ne $actualName) { Fail "Nama file pada SHA256 tidak cocok." }

    try { $manifest = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json }
    catch { Fail "Manifest JSON tidak valid: $($_.Exception.Message)" }
    $properties = @($manifest.PSObject.Properties.Name)
    foreach ($property in @(
        "schemaVersion", "application", "createdAtUtc", "database", "latestMigration",
        "postgresImage", "pgDumpVersion", "backupFile", "sizeBytes", "sha256"
    )) {
        if ($properties -notcontains $property) { Fail "Manifest tidak memiliki properti $property." }
    }
    if ([int]$manifest.schemaVersion -ne 1 -or [string]$manifest.application -ne "SiPacul") {
        Fail "Identitas manifest backup tidak didukung."
    }
    if ([string]$manifest.backupFile -ne $actualName) { Fail "Nama backup pada manifest tidak cocok." }

    $actualSize = [int64](Get-Item -LiteralPath $backupPath).Length
    if ($actualSize -le 0 -or $actualSize -ne [int64]$manifest.sizeBytes) { Fail "Ukuran backup tidak cocok dengan manifest." }
    $actualHash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualHash -ne $sidecarHash -or $actualHash -ne ([string]$manifest.sha256).ToUpperInvariant()) {
        Fail "SHA256 backup tidak cocok dengan sidecar atau manifest."
    }

    if ([string]::IsNullOrWhiteSpace($ExpectedLatestMigration)) {
        $ExpectedLatestMigration = [string]$manifest.latestMigration
    }
    if ([string]::IsNullOrWhiteSpace($ExpectedLatestMigration)) { Fail "Migration yang diharapkan tidak tersedia." }
    if ([string]::IsNullOrWhiteSpace($ContainerName)) {
        $ContainerName = "sipacul-restore-drill-" + [Guid]::NewGuid().ToString("N")
    }
    if ($ContainerName -notmatch '^sipacul-restore-drill-[a-z0-9][a-z0-9_.-]*$') {
        Fail "ContainerName harus memakai prefix sipacul-restore-drill- dan karakter aman."
    }
    if (Test-ExactContainer $ContainerName) { Fail "Container restore drill sudah ada: $ContainerName" }

    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & docker.exe image inspect $PostgresImage 1>$null 2>$null
        $imageExists = ($LASTEXITCODE -eq 0)
    }
    finally { $ErrorActionPreference = $previous }
    if (-not $imageExists) { Fail "Image lokal tidak ditemukan: $PostgresImage" }

    $devSignatureBefore = Get-ContainerSignature $DevContainer
    $token = [Guid]::NewGuid().ToString("N")
    $databaseName = "sipacul_restore_$($token.Substring(0, 12))"
    $databaseUser = "sipacul_restore"
    $databasePassword = "Restore-$token"
    $containerBackupPath = "/tmp/sipacul-restore.dump"

    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Backup, SHA256, manifest, ukuran, dan migration tervalidasi."
    Write-Host "[OK] Restore target terisolasi tanpa port host, jaringan, atau bind mount: $ContainerName"
    if ($devSignatureBefore -eq "<absent>") {
        Write-Host "[OK] Container pengembangan $DevContainer tidak ada dan tidak akan dibuat."
    }
    else {
        Write-Host "[OK] Container pengembangan $DevContainer terdeteksi dan tidak akan disentuh."
    }

    Write-Host ""
    Write-Host "=== RESTORE KE POSTGRESQL TERISOLASI ==="
    $createdId = Get-OneLine (Run-Docker @(
        "run", "--detach",
        "--name", $ContainerName,
        "--network", "none",
        "--env", "POSTGRES_DB=$databaseName",
        "--env", "POSTGRES_USER=$databaseUser",
        "--env", "POSTGRES_PASSWORD=$databasePassword",
        $PostgresImage
    )) "Container restore drill"
    $containerCreated = $true
    Wait-PostgresReady $ContainerName $databaseUser $databaseName 90
    Invoke-Docker @("cp", $backupPath, ($ContainerName + ":" + $containerBackupPath)) 1>$null
    Invoke-Docker @("exec", $ContainerName, "pg_restore", "--list", $containerBackupPath) 1>$null
    Invoke-Docker @(
        "exec", $ContainerName,
        "pg_restore", "--exit-on-error", "--no-owner", "--no-privileges",
        "--username", $databaseUser,
        "--dbname", $databaseName,
        $containerBackupPath
    )
    Write-Host "[OK] Archive dipulihkan ke database sementara."

    Write-Host ""
    Write-Host "=== VERIFIKASI HASIL RESTORE ==="
    $migrationSql = 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
    $latestMigration = Run-PsqlScalar $migrationSql $ContainerName $databaseUser $databaseName "Migration hasil restore"
    if ($latestMigration -ne $ExpectedLatestMigration) {
        Fail "Migration hasil restore bukan $ExpectedLatestMigration; aktual $latestMigration."
    }

    $migrationCountText = Run-PsqlScalar 'SELECT count(*) FROM "__EFMigrationsHistory";' $ContainerName $databaseUser $databaseName "Jumlah migration"
    $tableCountText = Run-PsqlScalar "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';" $ContainerName $databaseUser $databaseName "Jumlah tabel"
    $seasonReviewsText = Run-PsqlScalar "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'SeasonReviews';" $ContainerName $databaseUser $databaseName "Tabel SeasonReviews"
    $migrationCount = 0
    $tableCount = 0
    $seasonReviewsCount = 0
    if (-not [int]::TryParse($migrationCountText, [ref]$migrationCount) -or $migrationCount -le 0) {
        Fail "Riwayat migration hasil restore kosong atau tidak valid."
    }
    if (-not [int]::TryParse($tableCountText, [ref]$tableCount) -or $tableCount -le 0) {
        Fail "Tabel public hasil restore kosong atau tidak valid."
    }
    if (-not [int]::TryParse($seasonReviewsText, [ref]$seasonReviewsCount) -or $seasonReviewsCount -ne 1) {
        Fail "Tabel SeasonReviews tidak ditemukan tepat satu kali."
    }

    $portBindings = Get-OneLine (Run-Docker @("inspect", "--format", "{{json .HostConfig.PortBindings}}", $ContainerName)) "Port binding restore"
    $mounts = @(Run-Docker @("inspect", "--format", "{{range .Mounts}}{{json .}}{{println}}{{end}}", $ContainerName) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($portBindings -ne "{}" -and $portBindings -ne "null") { Fail "Restore drill memublikasikan port host." }
    if ($mounts.Count -gt 1) { Fail "Restore drill memiliki mount yang tidak diharapkan." }
    if ($mounts.Count -eq 1) {
        try { $mount = ([string]$mounts[0]) | ConvertFrom-Json }
        catch { Fail "Metadata mount restore drill tidak valid." }
        if ([string]$mount.Type -ne "volume" -or
            [string]::IsNullOrWhiteSpace([string]$mount.Name) -or
            [string]$mount.Destination -ne "/var/lib/postgresql/data") {
            Fail "Restore drill hanya boleh memakai volume data anonim sementara."
        }
        $temporaryVolumeNames = @([string]$mount.Name)
    }

    Write-Host "[OK] Migration terakhir: $latestMigration ($migrationCount migration)."
    Write-Host "[OK] $tableCount tabel public dan tabel SeasonReviews terverifikasi."
    Write-Host "[OK] Container tidak memiliki port host, bind mount, atau akses jaringan."
    $completed = $true
}
catch {
    $failureMessage = $_.Exception.Message
}
finally {
    if ($containerCreated) {
        $previous = $ErrorActionPreference
        try {
            $ErrorActionPreference = "Continue"
            & docker.exe rm --force --volumes $ContainerName 1>$null 2>$null
            if ($LASTEXITCODE -ne 0) { $cleanupMessage = "Container restore drill gagal dihapus: $ContainerName" }
        }
        finally { $ErrorActionPreference = $previous }
    }
    foreach ($volumeName in $temporaryVolumeNames) {
        $previous = $ErrorActionPreference
        try {
            $ErrorActionPreference = "Continue"
            & docker.exe volume inspect $volumeName 1>$null 2>$null
            if ($LASTEXITCODE -eq 0) {
                $cleanupMessage = "Volume restore drill masih tersisa: $volumeName"
                $completed = $false
            }
        }
        finally { $ErrorActionPreference = $previous }
    }
    if (-not [string]::IsNullOrWhiteSpace($cleanupMessage)) {
        $failureMessage = $cleanupMessage
        $completed = $false
    }
    if ($null -ne (Get-Variable -Name devSignatureBefore -ErrorAction SilentlyContinue)) {
        try {
            $devSignatureAfter = Get-ContainerSignature $DevContainer
            if ($devSignatureAfter -ne $devSignatureBefore) {
                $failureMessage = "State container pengembangan berubah selama restore drill."
                $completed = $false
            }
        }
        catch {
            $failureMessage = "Verifikasi akhir container pengembangan gagal: $($_.Exception.Message)"
            $completed = $false
        }
    }
}

if (-not $completed -or -not [string]::IsNullOrWhiteSpace($failureMessage)) {
    Write-Host ""
    Write-Host "[GAGAL] $failureMessage" -ForegroundColor Red
    Write-Host "Target produksi dan container pengembangan tidak pernah dipilih oleh skrip ini."
    exit 1
}

Write-Host ""
Write-Host "=== STATUS AKHIR ==="
Write-Host "[OK] Restore drill lulus dan container sementara sudah dihapus."
Write-Host "[OK] SHA256, archive, migration, tabel, isolasi port/network, dan cleanup volume tervalidasi."
Write-Host "[OK] Container pengembangan tetap identik; backup sumber tidak diubah."
