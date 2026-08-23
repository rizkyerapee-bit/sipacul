param(
    [string]$RepositoryRoot = "",
    [string]$EnvironmentFile = ".env.production",
    [string]$ComposeFile = "compose.production.yml",
    [string]$ComposeProject = "",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C1 PostgreSQL Backup 1 - PowerShell 5.1"
$containerId = ""
$containerDumpPath = ""
$partialPaths = @()
$finalPaths = @()
$completed = $false
$failureMessage = $null

function Fail([string]$Message) { throw $Message }

function Run-Git([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& git.exe @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) {
        Fail ("git.exe gagal: " + (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }
    return @($output | ForEach-Object { [string]$_ })
}

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

function Get-ComposeArguments([string[]]$Arguments) {
    $result = @("compose")
    if (-not [string]::IsNullOrWhiteSpace($ComposeProject)) {
        $result += @("--project-name", $ComposeProject)
    }
    $result += @(
        "--project-directory", $script:repoRoot,
        "--env-file", $script:environmentPath,
        "--file", $script:composePath
    )
    return @($result + $Arguments)
}

function Run-Compose([string[]]$Arguments) {
    return @(Run-Docker (Get-ComposeArguments $Arguments))
}

function Resolve-RepositoryFile([string]$PathValue) {
    if ([IO.Path]::IsPathRooted($PathValue)) {
        return [IO.Path]::GetFullPath($PathValue)
    }
    return [IO.Path]::GetFullPath((Join-Path $script:repoRoot $PathValue))
}

function Get-OneLine([string[]]$Output, [string]$Label) {
    $values = @($Output |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($values.Count -ne 1) {
        Fail "$Label harus menghasilkan tepat satu nilai."
    }
    return [string]$values[0]
}

function Wait-HealthyPostgres([string]$PostgresContainerId, [int]$Seconds) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        $state = Get-OneLine (Run-Docker @(
            "inspect", "--format",
            "{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}",
            $PostgresContainerId
        )) "State PostgreSQL"
        if ($state -eq "running|healthy") { return }
        if ($state -match "^(exited|dead)\|") { Fail "Container PostgreSQL berhenti: $state" }
        if ($state -eq "running|unhealthy") { Fail "Container PostgreSQL berstatus unhealthy." }
        Start-Sleep -Seconds 1
    } while ([DateTime]::UtcNow -lt $deadline)
    Fail "Container PostgreSQL tidak healthy dalam $Seconds detik."
}

function Test-IsInsideRepository([string]$CandidatePath, [string]$RootPath) {
    $trimCharacters = [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $candidate = [IO.Path]::GetFullPath($CandidatePath).TrimEnd($trimCharacters)
    $root = [IO.Path]::GetFullPath($RootPath).TrimEnd($trimCharacters)
    if ($candidate.Equals($root, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    $prefix = $root + [IO.Path]::DirectorySeparatorChar
    return $candidate.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}

try {
    Write-Host "=== PREFLIGHT BACKUP SIPACUL POSTGRESQL ==="
    foreach ($command in @("git.exe", "docker.exe")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
            Fail "$command tidak ditemukan."
        }
    }

    if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        $repoRoot = Get-OneLine (Run-Git @("rev-parse", "--show-toplevel")) "Repository root"
    }
    else {
        $repoRoot = Get-OneLine (Run-Git @("-C", $RepositoryRoot, "rev-parse", "--show-toplevel")) "Repository root"
    }
    $repoRoot = [IO.Path]::GetFullPath($repoRoot)
    $composePath = Resolve-RepositoryFile $ComposeFile
    $environmentPath = Resolve-RepositoryFile $EnvironmentFile
    if (-not (Test-Path -LiteralPath $composePath -PathType Leaf)) {
        Fail "Compose file tidak ditemukan: $composePath"
    }
    if (-not (Test-Path -LiteralPath $environmentPath -PathType Leaf)) {
        Fail "Environment file tidak ditemukan: $environmentPath"
    }

    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
        if ([string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
            Fail "USERPROFILE tidak tersedia; tentukan -OutputDirectory."
        }
        $OutputDirectory = Join-Path $env:USERPROFILE "SiPaculBackups"
    }
    $outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
    if (Test-IsInsideRepository $outputRoot $repoRoot) {
        Fail "OutputDirectory harus berada di luar repository agar backup tidak masuk Git."
    }

    Invoke-Docker @("info") 1>$null
    $composeVersion = Get-OneLine (Run-Docker @("compose", "version", "--short")) "Versi Docker Compose"
    $gitHeadBefore = Get-OneLine (Run-Git @("-C", $repoRoot, "rev-parse", "HEAD")) "Git HEAD"
    $gitStatusBefore = (Run-Git @("-C", $repoRoot, "status", "--porcelain=v1", "--untracked-files=all")) -join "`n"

    $containerIds = @(Run-Compose @("ps", "--all", "--quiet", "postgres"))
    $containerIds = @($containerIds |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique)
    if ($containerIds.Count -ne 1) {
        Fail "Service postgres harus memiliki tepat satu container; aktual $($containerIds.Count)."
    }
    $containerId = [string]$containerIds[0]
    Wait-HealthyPostgres $containerId 60

    $databaseName = Get-OneLine (Run-Docker @("exec", $containerId, "printenv", "POSTGRES_DB")) "POSTGRES_DB"
    $databaseUser = Get-OneLine (Run-Docker @("exec", $containerId, "printenv", "POSTGRES_USER")) "POSTGRES_USER"
    $postgresImage = Get-OneLine (Run-Docker @("inspect", "--format", "{{.Config.Image}}", $containerId)) "Image PostgreSQL"
    $pgDumpVersion = Get-OneLine (Run-Docker @("exec", $containerId, "pg_dump", "--version")) "Versi pg_dump"

    $migrationSql = 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
    $latestMigration = Get-OneLine (Run-DockerWithInput $migrationSql @(
        "exec", "--interactive", $containerId,
        "psql", "-X", "-v", "ON_ERROR_STOP=1",
        "-U", $databaseUser, "-d", $databaseName, "-tA"
    )) "Migration terakhir"

    [IO.Directory]::CreateDirectory($outputRoot) | Out-Null
    $timestamp = [DateTime]::UtcNow.ToString("yyyyMMddTHHmmssfffZ")
    $baseName = "sipacul-postgres-$timestamp.dump"
    $finalDump = Join-Path $outputRoot $baseName
    $finalChecksum = $finalDump + ".sha256"
    $finalManifest = $finalDump + ".json"
    foreach ($path in @($finalDump, $finalChecksum, $finalManifest)) {
        if (Test-Path -LiteralPath $path) { Fail "Target backup sudah ada: $path" }
    }

    $token = [Guid]::NewGuid().ToString("N")
    $partialDump = $finalDump + ".partial-$token"
    $partialChecksum = $finalChecksum + ".partial-$token"
    $partialManifest = $finalManifest + ".partial-$token"
    $partialPaths = @($partialDump, $partialChecksum, $partialManifest)
    $containerDumpPath = "/tmp/sipacul-backup-$token.dump"

    Write-Host "[OK] Repository: $repoRoot"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Docker Compose $composeVersion; PostgreSQL healthy; migration $latestMigration."
    Write-Host "[OK] Output backup di luar repository: $outputRoot"

    Write-Host ""
    Write-Host "=== MEMBUAT BACKUP ==="
    Invoke-Docker @(
        "exec", $containerId,
        "pg_dump", "--format=custom", "--compress=9",
        "--no-owner", "--no-privileges",
        "--username", $databaseUser,
        "--dbname", $databaseName,
        "--file", $containerDumpPath
    )
    Invoke-Docker @("exec", $containerId, "pg_restore", "--list", $containerDumpPath) 1>$null
    Invoke-Docker @("cp", ($containerId + ":" + $containerDumpPath), $partialDump) 1>$null
    if (-not (Test-Path -LiteralPath $partialDump -PathType Leaf)) { Fail "Archive backup tidak tersalin ke host." }
    $size = [int64](Get-Item -LiteralPath $partialDump).Length
    if ($size -le 0) { Fail "Archive backup kosong." }
    $sha256 = (Get-FileHash -LiteralPath $partialDump -Algorithm SHA256).Hash.ToUpperInvariant()

    $utf8NoBom = New-Object Text.UTF8Encoding($false)
    $checksumText = $sha256 + "  " + $baseName + "`n"
    [IO.File]::WriteAllText($partialChecksum, $checksumText, $utf8NoBom)
    $manifest = [ordered]@{
        schemaVersion = 1
        application = "SiPacul"
        createdAtUtc = [DateTime]::UtcNow.ToString("o")
        database = $databaseName
        latestMigration = $latestMigration
        postgresImage = $postgresImage
        pgDumpVersion = $pgDumpVersion
        backupFile = $baseName
        sizeBytes = $size
        sha256 = $sha256
    }
    [IO.File]::WriteAllText(
        $partialManifest,
        (($manifest | ConvertTo-Json -Depth 4) + "`n"),
        $utf8NoBom
    )

    Move-Item -LiteralPath $partialChecksum -Destination $finalChecksum
    $finalPaths += $finalChecksum
    Move-Item -LiteralPath $partialManifest -Destination $finalManifest
    $finalPaths += $finalManifest
    Move-Item -LiteralPath $partialDump -Destination $finalDump
    $finalPaths += $finalDump

    if ((Get-FileHash -LiteralPath $finalDump -Algorithm SHA256).Hash.ToUpperInvariant() -ne $sha256) {
        Fail "Hash archive berubah setelah finalisasi."
    }

    $gitHeadAfter = Get-OneLine (Run-Git @("-C", $repoRoot, "rev-parse", "HEAD")) "Git HEAD akhir"
    $gitStatusAfter = (Run-Git @("-C", $repoRoot, "status", "--porcelain=v1", "--untracked-files=all")) -join "`n"
    if ($gitHeadAfter -ne $gitHeadBefore -or $gitStatusAfter -ne $gitStatusBefore) {
        Fail "State Git berubah selama backup."
    }
    $completed = $true
}
catch {
    $failureMessage = $_.Exception.Message
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($containerId) -and
        -not [string]::IsNullOrWhiteSpace($containerDumpPath)) {
        $previous = $ErrorActionPreference
        try {
            $ErrorActionPreference = "Continue"
            & docker.exe exec $containerId rm -f $containerDumpPath 1>$null 2>$null
            $containerCleanupExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $previous }
        if ($containerCleanupExit -ne 0) {
            if ([string]::IsNullOrWhiteSpace($failureMessage)) {
                $failureMessage = "File sementara backup di container gagal dihapus."
            }
            else {
                $failureMessage += " Cleanup file sementara di container juga gagal."
            }
            $completed = $false
        }
    }
    foreach ($path in $partialPaths) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        }
    }
    if (-not $completed) {
        foreach ($path in $finalPaths) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($failureMessage)) {
    Write-Host ""
    Write-Host "[GAGAL] $failureMessage" -ForegroundColor Red
    Write-Host "Tidak ada backup parsial yang dipertahankan."
    exit 1
}

Write-Host "[OK] Archive custom tervalidasi oleh pg_restore --list."
Write-Host "[OK] SHA256: $sha256"
Write-Host "[OK] Manifest: $finalManifest"
Write-Host ""
Write-Host "=== STATUS AKHIR ==="
Write-Host "[OK] Backup selesai: $finalDump"
Write-Host "[OK] Ukuran: $size byte; migration: $latestMigration"
Write-Host "[OK] HEAD dan working tree tidak berubah; file sementara container sudah dihapus."
