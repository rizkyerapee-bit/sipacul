param(
    [string]$RepositoryRoot = "",
    [string]$EnvironmentFile = ".env.production",
    [string]$ComposeFile = "compose.production.yml",
    [string]$ComposeProject = "",
    [string]$OutputDirectory = "",
    [string]$LogFile = "",
    [int]$RetentionDays = 30,
    [int]$MinimumBackups = 7,
    [double]$FreshnessHours = 26,
    [switch]$ApplyRetention,
    [switch]$VerifyAllHashes
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C2A Backup Cycle 1 - PowerShell 5.1"
$mutex = $null
$lockTaken = $false
$logPath = $null
$failureMessage = $null
$completed = $false

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

function Get-OneLine([string[]]$Output, [string]$Label) {
    $values = @($Output |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($values.Count -ne 1) { Fail "$Label harus menghasilkan tepat satu nilai." }
    return [string]$values[0]
}

function Test-IsInsideRepository([string]$CandidatePath, [string]$RootPath) {
    $trimCharacters = [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $candidate = [IO.Path]::GetFullPath($CandidatePath).TrimEnd($trimCharacters)
    $root = [IO.Path]::GetFullPath($RootPath).TrimEnd($trimCharacters)
    if ($candidate.Equals($root, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    return $candidate.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Add-LogLine([string]$Message) {
    if ([string]::IsNullOrWhiteSpace($script:logPath)) { return }
    $line = "{0} {1}`r`n" -f [DateTime]::UtcNow.ToString("o"), $Message
    [IO.File]::AppendAllText($script:logPath, $line, (New-Object Text.UTF8Encoding($false)))
}

function Invoke-LoggedChild([string]$Stage, [string[]]$Arguments) {
    Add-LogLine "stage=$Stage event=start"
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    foreach ($item in $output) {
        $line = [string]$item
        Write-Host $line
        Add-LogLine ("stage=$Stage output=" + $line)
    }
    if ($exitCode -ne 0) {
        Add-LogLine "stage=$Stage event=failed exitCode=$exitCode"
        Fail "$Stage gagal dengan exit code $exitCode."
    }
    Add-LogLine "stage=$Stage event=completed exitCode=0"
}

try {
    Write-Host "=== PREFLIGHT SIKLUS BACKUP SIPACUL ==="
    foreach ($command in @("git.exe", "powershell.exe")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { Fail "$command tidak ditemukan." }
    }
    if ($RetentionDays -lt 0 -or $RetentionDays -gt 3650) { Fail "RetentionDays tidak valid." }
    if ($MinimumBackups -lt 1 -or $MinimumBackups -gt 10000) { Fail "MinimumBackups tidak valid." }
    if ($FreshnessHours -le 0 -or $FreshnessHours -gt 8760) { Fail "FreshnessHours tidak valid." }

    if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        $repoRoot = Get-OneLine (Run-Git @("rev-parse", "--show-toplevel")) "Repository root"
    }
    else {
        $repoRoot = Get-OneLine (Run-Git @("-C", $RepositoryRoot, "rev-parse", "--show-toplevel")) "Repository root"
    }
    $repoRoot = [IO.Path]::GetFullPath($repoRoot)
    $gitHeadBefore = Get-OneLine (Run-Git @("-C", $repoRoot, "rev-parse", "HEAD")) "Git HEAD"
    $gitStatusBefore = (Run-Git @("-C", $repoRoot, "status", "--porcelain=v1", "--untracked-files=all")) -join "`n"

    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
        if ([string]::IsNullOrWhiteSpace($env:USERPROFILE)) { Fail "USERPROFILE tidak tersedia." }
        $OutputDirectory = Join-Path $env:USERPROFILE "SiPaculBackups"
    }
    $outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
    if (Test-IsInsideRepository $outputRoot $repoRoot) {
        Fail "OutputDirectory harus berada di luar repository."
    }
    $trimCharacters = [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if ($outputRoot.TrimEnd($trimCharacters).Equals(
        [IO.Path]::GetPathRoot($outputRoot).TrimEnd($trimCharacters),
        [StringComparison]::OrdinalIgnoreCase
    )) {
        Fail "OutputDirectory tidak boleh berupa root volume."
    }
    [IO.Directory]::CreateDirectory($outputRoot) | Out-Null

    if ([string]::IsNullOrWhiteSpace($LogFile)) {
        $logPath = Join-Path (Join-Path $outputRoot "logs") "backup-cycle.log"
    }
    else {
        $logPath = [IO.Path]::GetFullPath($LogFile)
    }
    if (Test-IsInsideRepository $logPath $repoRoot) { Fail "LogFile harus berada di luar repository." }
    $logParent = Split-Path -Parent $logPath
    [IO.Directory]::CreateDirectory($logParent) | Out-Null

    $backupScript = Join-Path $repoRoot "operations\Backup-SiPaculPostgres-PS51.ps1"
    $retentionScript = Join-Path $repoRoot "operations\Invoke-SiPaculBackupRetention-PS51.ps1"
    $freshnessScript = Join-Path $repoRoot "operations\Test-SiPaculBackupFreshness-PS51.ps1"
    foreach ($path in @($backupScript, $retentionScript, $freshnessScript)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Skrip siklus tidak ditemukan: $path" }
    }

    $mutexMaterial = [Text.Encoding]::UTF8.GetBytes($repoRoot + "|" + $outputRoot)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $mutexHash = ([BitConverter]::ToString($sha.ComputeHash($mutexMaterial))).Replace("-", "") }
    finally { $sha.Dispose() }
    $mutexName = "Local\SiPaculBackupCycle-" + $mutexHash.Substring(0, 24)
    $mutex = New-Object Threading.Mutex($false, $mutexName)
    try { $lockTaken = $mutex.WaitOne(0) }
    catch [Threading.AbandonedMutexException] { $lockTaken = $true }
    if (-not $lockTaken) { Fail "Siklus backup lain sedang berjalan untuk repository dan folder ini." }

    Add-LogLine "cycle=start revision=$Revision head=$gitHeadBefore"
    Write-Host "[OK] Repository: $repoRoot"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Lock eksklusif diperoleh; output dan log berada di luar repository."
    Write-Host "[OK] Retensi: $RetentionDays hari / minimum $MinimumBackups / apply=$([bool]$ApplyRetention)."

    Write-Host ""
    Write-Host "=== MEMBUAT BACKUP TERJADWAL ==="
    $beforeCount = @(Get-ChildItem -LiteralPath $outputRoot -File -Filter "sipacul-postgres-*.dump").Count
    $backupArguments = @(
        "-File", $backupScript,
        "-RepositoryRoot", $repoRoot,
        "-EnvironmentFile", $EnvironmentFile,
        "-ComposeFile", $ComposeFile
    )
    if (-not [string]::IsNullOrWhiteSpace($ComposeProject)) {
        $backupArguments += @("-ComposeProject", $ComposeProject)
    }
    $backupArguments += @("-OutputDirectory", $outputRoot)
    Invoke-LoggedChild -Stage "backup" -Arguments $backupArguments
    $afterBackupCount = @(Get-ChildItem -LiteralPath $outputRoot -File -Filter "sipacul-postgres-*.dump").Count
    if ($afterBackupCount -ne ($beforeCount + 1)) { Fail "Siklus tidak menambahkan tepat satu archive backup." }

    Write-Host ""
    Write-Host "=== MENJALANKAN RETENSI ==="
    $retentionArguments = @(
        "-File", $retentionScript,
        "-BackupDirectory", $outputRoot,
        "-RetentionDays", [string]$RetentionDays,
        "-MinimumBackups", [string]$MinimumBackups
    )
    if ($ApplyRetention) { $retentionArguments += "-Apply" }
    Invoke-LoggedChild -Stage "retention" -Arguments $retentionArguments

    Write-Host ""
    Write-Host "=== MEMERIKSA FRESHNESS ==="
    $freshnessArguments = @(
        "-File", $freshnessScript,
        "-BackupDirectory", $outputRoot,
        "-MaxAgeHours", [string]$FreshnessHours,
        "-MinimumValidBackups", "1"
    )
    if ($VerifyAllHashes) { $freshnessArguments += "-VerifyAllHashes" }
    Invoke-LoggedChild -Stage "freshness" -Arguments $freshnessArguments

    $gitHeadAfter = Get-OneLine (Run-Git @("-C", $repoRoot, "rev-parse", "HEAD")) "Git HEAD akhir"
    $gitStatusAfter = (Run-Git @("-C", $repoRoot, "status", "--porcelain=v1", "--untracked-files=all")) -join "`n"
    if ($gitHeadAfter -ne $gitHeadBefore -or $gitStatusAfter -ne $gitStatusBefore) {
        Fail "State Git berubah selama siklus backup."
    }
    Add-LogLine "cycle=completed head=$gitHeadAfter"
    $completed = $true
}
catch {
    $failureMessage = $_.Exception.Message
    try { Add-LogLine ("cycle=failed message=" + $failureMessage) }
    catch { }
}
finally {
    if ($lockTaken -and $null -ne $mutex) {
        try { $mutex.ReleaseMutex() }
        catch { }
    }
    if ($null -ne $mutex) { $mutex.Dispose() }
}

if (-not $completed) {
    Write-Host ""
    Write-Host "[GAGAL] $failureMessage" -ForegroundColor Red
    if (-not [string]::IsNullOrWhiteSpace($logPath)) { Write-Host "Log: $logPath" }
    exit 1
}

Write-Host ""
Write-Host "=== STATUS AKHIR SIKLUS BACKUP ==="
Write-Host "[OK] Backup, retensi, freshness, lock, dan log operasional lulus."
Write-Host "[OK] Log: $logPath"
Write-Host "[OK] HEAD dan working tree tidak berubah."
