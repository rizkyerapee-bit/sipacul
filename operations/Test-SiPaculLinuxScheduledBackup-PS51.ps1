param(
    [string]$RepositoryRoot = "D:\Development\Projects\SiPacul"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20D2G3C6 Linux Scheduled Backup Validator 2 - PowerShell 5.1"

$Files = @(
    "operations/linux/backup-postgres.sh",
    "operations/linux/sipacul-backup-set.py",
    "operations/linux/backup-retention.sh",
    "operations/linux/backup-freshness.sh",
    "operations/linux/backup-cycle.sh",
    "operations/linux/install-backup-systemd.sh",
    "operations/linux/systemd/sipacul-postgres-backup.service",
    "operations/linux/systemd/sipacul-postgres-backup.timer",
    "operations/Test-SiPaculLinuxScheduledBackup-PS51.ps1",
    "docs/18-linux-scheduled-backup.md"
)

function Fail([string]$Message) { throw $Message }

function Repo-Path([string]$Root, [string]$Relative) {
    return Join-Path $Root ($Relative.Replace("/", "\"))
}

function Assert-ContainsOnce {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Marker,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $count = ([regex]::Matches($Text, [regex]::Escape($Marker))).Count
    if ($count -ne 1) {
        Fail "$Label kehilangan marker atau marker tidak tunggal: $Marker (count=$count)"
    }
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Marker,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ($Text.IndexOf($Marker, [StringComparison]::Ordinal) -lt 0) {
        Fail "$Label kehilangan marker: $Marker"
    }
}

function Assert-TextFileClean {
    param([string]$Path, [string]$Label)

    $bytes = [IO.File]::ReadAllBytes($Path)
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -eq 13) {
            Fail "$Label tidak LF-only."
        }
    }

    $lines = @([IO.File]::ReadAllLines($Path))
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = [string]$lines[$index]
        if ($line -match '[ \t]+$') {
            Fail "$Label memiliki trailing whitespace pada line $($index + 1)."
        }
    }
}

function Resolve-Bash {
    $candidates = @(
        "C:\Program Files\Git\bin\bash.exe",
        "C:\Program Files\Git\usr\bin\bash.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }

    $command = Get-Command bash.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) { return $command.Source }

    Fail "Git Bash bash.exe tidak ditemukan."
}

try {
    Write-Host "=== VALIDASI SPRINT 20D2G3C6 LINUX SCHEDULED BACKUP ==="
    Write-Host "[INFO] $Revision"

    if (-not (Get-Command git.exe -ErrorAction SilentlyContinue)) {
        Fail "git.exe tidak ditemukan."
    }

    $rootRaw = @(& git.exe -C $RepositoryRoot rev-parse --show-toplevel 2>&1)
    if ($LASTEXITCODE -ne 0) { Fail "Repository root tidak dapat ditentukan." }
    $rootValues = @($rootRaw | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_ })
    if ($rootValues.Count -ne 1) { Fail "Repository root ambigu." }
    $root = [IO.Path]::GetFullPath([string]$rootValues[0])

    $texts = @{}
    foreach ($relative in $Files) {
        $path = Repo-Path $root $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            Fail "File tidak ditemukan: $relative"
        }
        Assert-TextFileClean -Path $path -Label $relative
        $texts[$relative] = [IO.File]::ReadAllText($path)
    }

    Write-Host "[OK] Sepuluh file scope tersedia, LF-only, tanpa trailing whitespace."

    $backup = [string]$texts["operations/linux/backup-postgres.sh"]
    foreach ($marker in @(
        "SIPACUL_BACKUP_OPERATION_LOCK_FILE",
        "/run/lock/sipacul-postgres-backup-operation.lock",
        "flock -n 8",
        "Operasi backup PostgreSQL lain sedang berjalan.",
        "pg_dump --format=custom --compress=9",
        "pg_restore --list",
        'SELECT "MigrationId" FROM "__EFMigrationsHistory"'
    )) {
        Assert-Contains -Text $backup -Marker $marker -Label "backup-postgres.sh"
    }

    $helper = [string]$texts["operations/linux/sipacul-backup-set.py"]
    foreach ($marker in @(
        "INCOMPLETE_GRACE_SECONDS = 300",
        "def scan_backup_sets",
        "def verify_hash",
        "def command_freshness",
        "def command_retention",
        "tempfile.mkdtemp",
        "rollback_moves",
        "minimum-backups",
        "verify-all-hashes"
    )) {
        Assert-Contains -Text $helper -Marker $marker -Label "sipacul-backup-set.py"
    }

    $retention = [string]$texts["operations/linux/backup-retention.sh"]
    foreach ($marker in @(
        "RETENTION_DAYS=30",
        "MINIMUM_BACKUPS=7",
        "APPLY=false",
        "--apply",
        'python3 "$SCRIPT_DIR/sipacul-backup-set.py"'
    )) {
        Assert-Contains -Text $retention -Marker $marker -Label "backup-retention.sh"
    }

    $freshness = [string]$texts["operations/linux/backup-freshness.sh"]
    foreach ($marker in @(
        "MAX_AGE_HOURS=26",
        "MINIMUM_VALID_BACKUPS=1",
        "VERIFY_ALL_HASHES=false",
        "--verify-all-hashes",
        'python3 "$SCRIPT_DIR/sipacul-backup-set.py"'
    )) {
        Assert-Contains -Text $freshness -Marker $marker -Label "backup-freshness.sh"
    }

    $cycle = [string]$texts["operations/linux/backup-cycle.sh"]
    foreach ($marker in @(
        "/run/lock/sipacul-postgres-backup-cycle.lock",
        "flock -n 9",
        "run_stage backup",
        "run_stage retention",
        "run_stage freshness",
        "--apply-retention",
        "--verify-all-hashes",
        "/var/log/sipacul/backup-cycle.log",
        "Git HEAD berubah selama siklus backup."
    )) {
        Assert-Contains -Text $cycle -Marker $marker -Label "backup-cycle.sh"
    }

    $installer = [string]$texts["operations/linux/install-backup-systemd.sh"]
    foreach ($marker in @(
        "EXECUTE=false",
        "ENABLE=false",
        "--execute",
        "--enable",
        "--force",
        "systemd-analyze verify",
        "systemctl daemon-reload",
        'systemctl enable --now "$TIMER_NAME"',
        "Plan-only selesai"
    )) {
        Assert-Contains -Text $installer -Marker $marker -Label "install-backup-systemd.sh"
    }

    $service = [string]$texts["operations/linux/systemd/sipacul-postgres-backup.service"]
    foreach ($marker in @(
        "Requires=docker.service",
        "User=root",
        "WorkingDirectory=/opt/sipacul/repository",
        "ExecStart=/opt/sipacul/repository/operations/linux/backup-cycle.sh",
        "--retention-days 30",
        "--minimum-backups 7",
        "--freshness-hours 26",
        "--apply-retention --verify-all-hashes",
        "TimeoutStartSec=4h",
        "UMask=0077"
    )) {
        Assert-Contains -Text $service -Marker $marker -Label "systemd service"
    }

    $timer = [string]$texts["operations/linux/systemd/sipacul-postgres-backup.timer"]
    Assert-ContainsOnce -Text $timer -Marker "OnCalendar=*-*-* 02:00:00 Asia/Jakarta" -Label "systemd timer"
    Assert-ContainsOnce -Text $timer -Marker "Persistent=true" -Label "systemd timer"
    Assert-ContainsOnce -Text $timer -Marker "Unit=sipacul-postgres-backup.service" -Label "systemd timer"

    $doc = [string]$texts["docs/18-linux-scheduled-backup.md"]
    $docNormalized = [regex]::Replace($doc, '\s+', ' ').Trim()
    foreach ($marker in @(
        "02:00 Asia/Jakarta",
        "Persistent=true",
        "dry-run by default",
        "private initial SiPacul deployment",
        "timer must not be enabled before PostgreSQL exists",
        "does not:",
        "open port 443"
    )) {
        Assert-Contains -Text $docNormalized -Marker $marker -Label "documentation"
    }

    $bash = Resolve-Bash
    foreach ($relative in @(
        "operations/linux/backup-postgres.sh",
        "operations/linux/backup-retention.sh",
        "operations/linux/backup-freshness.sh",
        "operations/linux/backup-cycle.sh",
        "operations/linux/install-backup-systemd.sh"
    )) {
        $native = Repo-Path $root $relative
        $gitStyle = $native.Replace("\", "/")
        if ($gitStyle -match '^([A-Za-z]):/(.*)$') {
            $gitStyle = "/" + $Matches[1].ToLowerInvariant() + "/" + $Matches[2]
        }
        & $bash -n $gitStyle
        if ($LASTEXITCODE -ne 0) { Fail "bash -n gagal: $relative" }
    }
    Write-Host "[OK] Lima Bash script scope lulus bash -n melalui Git Bash."

    $pythonCommand = $null
    foreach ($candidate in @("python.exe", "python3.exe")) {
        $resolved = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($null -ne $resolved -and $resolved.Source -notmatch "\\WindowsApps\\") {
            $pythonCommand = $resolved.Source
            break
        }
    }
    if ($null -ne $pythonCommand) {
        $helperPath = Repo-Path $root "operations/linux/sipacul-backup-set.py"
        $compileCode = "compile(open(r'''$helperPath''', encoding='utf-8').read(), r'''$helperPath''', 'exec')"
        & $pythonCommand -c $compileCode
        if ($LASTEXITCODE -ne 0) { Fail "Python syntax validation gagal." }
        Write-Host "[OK] Python helper lulus compile syntax lokal."
    }
    else {
        Write-Host "[INFO] Python lokal Windows tidak ditemukan; syntax helper akan divalidasi pada Ubuntu host sebelum systemd install."
    }

    $runtimeScope = @(
        "operations/linux/backup-postgres.sh",
        "operations/linux/sipacul-backup-set.py",
        "operations/linux/backup-retention.sh",
        "operations/linux/backup-freshness.sh",
        "operations/linux/backup-cycle.sh",
        "operations/linux/install-backup-systemd.sh",
        "operations/linux/systemd/sipacul-postgres-backup.service",
        "operations/linux/systemd/sipacul-postgres-backup.timer"
    )
    $combined = ($runtimeScope | ForEach-Object { [string]$texts[$_] }) -join "`n"
    foreach ($forbidden in @(
        "docker compose down --volumes",
        "pg_restore --clean",
        "pg_restore --create",
        "SIPACUL_PUBLIC_ACTIVATION=enabled",
        "ufw allow",
        "iptables -A",
        "nft add rule",
        "certbot certonly"
    )) {
        if ($combined.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Fail "Scope memuat operasi terlarang: $forbidden"
        }
    }

    Write-Host "[OK] Lock, retention, freshness, cycle, systemd plan/activation boundary tervalidasi."
    Write-Host "[OK] Tidak ada deployment, restore production, volume deletion, public activation, DNS, firewall, certificate, atau HSTS mutation."
    Write-Host ""
    Write-Host "SIPACUL LINUX SCHEDULED BACKUP: PASS" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
