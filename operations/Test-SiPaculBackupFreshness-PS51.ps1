param(
    [Parameter(Mandatory = $true)]
    [string]$BackupDirectory,
    [double]$MaxAgeHours = 26,
    [int]$MinimumValidBackups = 1,
    [switch]$VerifyAllHashes
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C2A Backup Freshness 1 - PowerShell 5.1"

function Fail([string]$Message) { throw $Message }

try {
    Write-Host "=== PREFLIGHT BACKUP FRESHNESS SIPACUL ==="
    if ($MaxAgeHours -le 0 -or $MaxAgeHours -gt 8760) {
        Fail "MaxAgeHours harus lebih dari 0 dan paling besar 8760."
    }
    if ($MinimumValidBackups -lt 1 -or $MinimumValidBackups -gt 10000) {
        Fail "MinimumValidBackups harus berada pada rentang 1-10000."
    }

    $modulePath = Join-Path $PSScriptRoot "SiPaculBackupSet.psm1"
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) { Fail "Module backup set tidak ditemukan." }
    Import-Module -Name $modulePath -Force -ErrorAction Stop
    $root = Resolve-SiPaculBackupDirectory $BackupDirectory
    $backupSets = @(Get-SiPaculBackupSet $root)
    if ($backupSets.Count -lt $MinimumValidBackups) {
        Fail "Backup valid hanya $($backupSets.Count); minimum $MinimumValidBackups."
    }

    $now = [DateTime]::UtcNow
    $newest = $backupSets[0]
    $ageHours = ($now - $newest.CreatedAtUtc).TotalHours
    if ($ageHours -lt (-5.0 / 60.0)) { Fail "Backup terbaru memiliki waktu lebih dari 5 menit di masa depan." }
    if ($ageHours -gt $MaxAgeHours) {
        Fail ("Backup terbaru berusia {0:N2} jam; batas {1:N2} jam." -f $ageHours, $MaxAgeHours)
    }

    if ($VerifyAllHashes) {
        foreach ($backupSet in $backupSets) { Test-SiPaculBackupHash $backupSet | Out-Null }
        $hashScope = "seluruh $($backupSets.Count) archive"
    }
    else {
        Test-SiPaculBackupHash $newest | Out-Null
        $hashScope = "archive terbaru"
    }

    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Folder backup: $root"
    Write-Host "[OK] $($backupSets.Count) backup valid; SHA256 $hashScope cocok."

    Write-Host ""
    Write-Host "=== STATUS BACKUP FRESHNESS ==="
    Write-Host ("[OK] Backup terbaru berusia {0:N2} jam: {1}" -f ([Math]::Max(0, $ageHours)), $newest.BackupFile)
    Write-Host "[OK] Migration terbaru pada manifest: $($newest.LatestMigration)"
    Write-Host "[OK] Freshness berada di bawah batas $MaxAgeHours jam."
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
