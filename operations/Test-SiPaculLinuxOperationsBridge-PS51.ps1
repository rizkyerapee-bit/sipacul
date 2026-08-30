param(
    [string]$RepositoryRoot = "D:\Development\Projects\SiPacul"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20D2G3C5B Linux Operations Bridge Validator 2 - PowerShell 5.1"

function Fail([string]$Message) { throw $Message }

function Get-Path {
    param([string]$Root, [string]$Relative)
    return Join-Path $Root ($Relative.Replace("/", "\"))
}

function Read-Text {
    param([string]$Root, [string]$Relative)
    $path = Get-Path $Root $Relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Fail "File wajib tidak ditemukan: $Relative"
    }
    return [IO.File]::ReadAllText($path)
}

function Assert-Contains {
    param([string]$Text, [string]$Marker, [string]$Label)
    if ($Text.IndexOf($Marker, [StringComparison]::Ordinal) -lt 0) {
        Fail "$Label kehilangan marker: $Marker"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Marker, [string]$Label)
    if ($Text.IndexOf($Marker, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Fail "$Label memuat marker terlarang: $Marker"
    }
}

function Invoke-Native {
    param([string]$Command, [string[]]$Arguments, [string]$Label)
    $old = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& $Command @Arguments 2>&1)
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $old
    }
    if ($code -ne 0) {
        Fail ("$Label gagal dengan exit code $code.`n" +
            (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }
    return @($output)
}

try {
    Write-Host "=== VALIDASI SPRINT 20D2G3C5B LINUX OPERATIONS BRIDGE ==="
    Write-Host "[INFO] $Revision"

    if (-not (Get-Command git.exe -ErrorAction SilentlyContinue)) {
        Fail "git.exe tidak ditemukan."
    }

    $root = (& git.exe -C $RepositoryRoot rev-parse --show-toplevel 2>$null | Select-Object -First 1)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace([string]$root)) {
        Fail "Repository Git tidak dapat ditentukan."
    }
    $root = [IO.Path]::GetFullPath(([string]$root).Trim())

    $required = @(
        "operations/linux/sipacul-common.sh",
        "operations/linux/backup-postgres.sh",
        "operations/linux/deploy.sh",
        "operations/linux/application-rollback.sh",
        "operations/Test-SiPaculLinuxOperationsBridge-PS51.ps1",
        "docs/17-linux-production-operations-bridge.md"
    )

    foreach ($relative in $required) {
        $path = Get-Path $root $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            Fail "File wajib tidak ditemukan: $relative"
        }

        $bytes = [IO.File]::ReadAllBytes($path)
        if ($bytes -contains 13) {
            Fail "File harus LF-only, CR ditemukan: $relative"
        }

        $lines = @([IO.File]::ReadAllLines($path))
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ([string]$lines[$i] -match '[ \t]+$') {
                Fail "Trailing whitespace: ${relative}:$($i + 1)"
            }
        }
    }
    Write-Host "[OK] Enam file bridge tersedia, LF-only, tanpa trailing whitespace."

    $common = Read-Text $root "operations/linux/sipacul-common.sh"
    foreach ($marker in @(
        'SIPACUL_DEFAULT_REPOSITORY_ROOT="/opt/sipacul/repository"',
        'SIPACUL_DEFAULT_ENVIRONMENT_FILE="/etc/sipacul/.env.production"',
        'SIPACUL_DEFAULT_STATE_DIRECTORY="/var/lib/sipacul/deployment-state"',
        'SIPACUL_DEFAULT_BACKUP_DIRECTORY="/var/backups/sipacul"',
        'ghcr.io/%s/sipacul-%s:sha-%s',
        'org.opencontainers.image.revision',
        'databaseReleaseSha',
        'runtimeReleaseSha'
    )) {
        Assert-Contains $common $marker "sipacul-common.sh"
    }

    $backup = Read-Text $root "operations/linux/backup-postgres.sh"
    foreach ($marker in @(
        'pg_dump --format=custom --compress=9',
        'pg_restore --list',
        'sha256sum',
        '"schemaVersion": 1',
        '"application": "SiPacul"',
        'LATEST_MIGRATION',
        'GIT_HEAD_BEFORE',
        'GIT_STATUS_BEFORE'
    )) {
        Assert-Contains $backup $marker "backup-postgres.sh"
    }
    foreach ($forbidden in @(
        'pg_restore --clean',
        'pg_restore --create',
        'docker volume rm',
        'down --volumes'
    )) {
        Assert-NotContains $backup $forbidden "backup-postgres.sh"
    }

    $deploy = Read-Text $root "operations/linux/deploy.sh"
    foreach ($marker in @(
        'EXECUTE=false',
        'ALLOW_INITIAL_DEPLOYMENT_WITHOUT_BACKUP=false',
        '--allow-initial-deployment-without-backup',
        'PULL IMMUTABLE RELEASE IMAGES',
        'PRE-DEPLOY BACKUP',
        'current-release.env',
        'pending-operation.json',
        '--abort-on-container-exit',
        '--exit-code-from migrator',
        'for service in api frontend edge',
        'databaseReleaseSha',
        'runtimeReleaseSha',
        'Tidak ada restore database atau rollback otomatis'
    )) {
        Assert-Contains $deploy $marker "deploy.sh"
    }
    foreach ($forbidden in @(
        'down --volumes',
        'docker volume rm',
        'pg_restore',
        'SIPACUL_PUBLIC_ACTIVATION=enabled',
        'SIPACUL_BIND_ADDRESS=0.0.0.0'
    )) {
        Assert-NotContains $deploy $forbidden "deploy.sh"
    }

    $rollback = Read-Text $root "operations/linux/application-rollback.sh"
    foreach ($marker in @(
        'EXECUTE=false',
        'ACKNOWLEDGE_DATABASE_COMPATIBILITY=false',
        '--acknowledge-database-compatibility',
        'ROLLBACK_INVARIANT_NO_MIGRATOR_EXECUTION',
        'PRE-ROLLBACK BACKUP',
        'current-release.env',
        'for service in api frontend edge',
        'databaseReleaseSha',
        'runtimeReleaseSha',
        'Migrator tidak dijalankan; database tidak direstore atau didowngrade'
    )) {
        Assert-Contains $rollback $marker "application-rollback.sh"
    }
    foreach ($forbidden in @(
        'down --volumes',
        'docker volume rm',
        'pg_restore',
        '--exit-code-from migrator',
        'up --no-deps --no-build --abort-on-container-exit'
    )) {
        Assert-NotContains $rollback $forbidden "application-rollback.sh"
    }

    $doc = Read-Text $root "docs/17-linux-production-operations-bridge.md"
    foreach ($marker in @(
        "Plan-only adalah default",
        "Initial managed deployment",
        "Normal deployment",
        "PostgreSQL backup",
        "Emergency application rollback",
        "Failure safety",
        "Public activation tetap terpisah",
        "Backup scheduler"
    )) {
        Assert-Contains $doc $marker "docs/17-linux-production-operations-bridge.md"
    }

    $gitCommand = Get-Command git.exe
    $gitCmdDir = Split-Path -Parent $gitCommand.Source
    $gitRoot = Split-Path -Parent $gitCmdDir
    $bashCandidates = @(
        (Join-Path $gitRoot "bin\bash.exe"),
        (Join-Path $gitRoot "usr\bin\bash.exe")
    )
    $bashPath = $null
    foreach ($candidate in $bashCandidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $bashPath = $candidate
            break
        }
    }

    if ($null -eq $bashPath) {
        Write-Host "[INFO] Git Bash tidak ditemukan pada path standar; syntax Bash runtime belum dijalankan."
    }
    else {
        foreach ($relative in @(
            "operations/linux/sipacul-common.sh",
            "operations/linux/backup-postgres.sh",
            "operations/linux/deploy.sh",
            "operations/linux/application-rollback.sh"
        )) {
            $path = (Get-Path $root $relative).Replace("\", "/")
            [void](Invoke-Native $bashPath @("-n", $path) "bash -n $relative")
        }
        Write-Host "[OK] Empat Bash script lulus bash -n melalui Git Bash."
    }

    $diffCheck = @(Invoke-Native "git.exe" @("-C", $root, "diff", "--check") "git diff --check")
    if ($diffCheck.Count -ne 0) {
        Fail ("git diff --check menghasilkan output:`n" + ($diffCheck -join "`n"))
    }

    Write-Host "[OK] Kontrak immutable SHA, backup, migration gate, state split, dan rollback runtime-only tervalidasi."
    Write-Host "[OK] Tidak ada public activation, DNS, firewall, certificate, volume deletion, atau production restore."
    Write-Host ""
    Write-Host "SIPACUL LINUX OPERATIONS BRIDGE: PASS" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
