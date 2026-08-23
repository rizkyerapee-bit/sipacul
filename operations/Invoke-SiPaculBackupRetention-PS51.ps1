param(
    [Parameter(Mandatory = $true)]
    [string]$BackupDirectory,
    [int]$RetentionDays = 30,
    [int]$MinimumBackups = 7,
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C2A Backup Retention 1 - PowerShell 5.1"
$trashRoot = $null

function Fail([string]$Message) { throw $Message }

try {
    Write-Host "=== PREFLIGHT RETENSI BACKUP SIPACUL ==="
    if ($RetentionDays -lt 0 -or $RetentionDays -gt 3650) {
        Fail "RetentionDays harus berada pada rentang 0-3650."
    }
    if ($MinimumBackups -lt 1 -or $MinimumBackups -gt 10000) {
        Fail "MinimumBackups harus berada pada rentang 1-10000."
    }

    $modulePath = Join-Path $PSScriptRoot "SiPaculBackupSet.psm1"
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) { Fail "Module backup set tidak ditemukan." }
    Import-Module -Name $modulePath -Force -ErrorAction Stop
    $root = Resolve-SiPaculBackupDirectory $BackupDirectory
    $backupSets = @(Get-SiPaculBackupSet $root)
    if ($backupSets.Count -eq 0) { Fail "Tidak ada backup valid untuk dievaluasi." }

    $cutoff = [DateTime]::UtcNow.AddDays(-1.0 * $RetentionDays)
    $eligibleByPosition = @($backupSets | Select-Object -Skip $MinimumBackups)
    $candidates = @($eligibleByPosition | Where-Object { $_.CreatedAtUtc -lt $cutoff })

    foreach ($candidate in $candidates) { Test-SiPaculBackupHash $candidate | Out-Null }

    $mode = if ($Apply) { "APPLY" } else { "DRY-RUN" }
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Mode: $mode; folder: $root"
    Write-Host "[OK] $($backupSets.Count) backup valid; $MinimumBackups backup terbaru selalu dilindungi."
    Write-Host "[OK] $($candidates.Count) kandidat melewati batas $RetentionDays hari dan SHA256 kandidat valid."

    Write-Host ""
    Write-Host "=== EVALUASI RETENSI ==="
    if ($candidates.Count -eq 0) {
        Write-Host "[OK] Tidak ada backup yang memenuhi syarat penghapusan."
    }
    elseif (-not $Apply) {
        foreach ($candidate in $candidates) {
            Write-Host "[DRY-RUN] Akan menghapus triplet: $($candidate.BackupFile)"
        }
        Write-Host "[OK] Dry-run selesai; tidak ada file yang diubah atau dihapus."
    }
    else {
        $trashRoot = Join-Path $root (".sipacul-retention-trash-" + [Guid]::NewGuid().ToString("N"))
        [IO.Directory]::CreateDirectory($trashRoot) | Out-Null
        $movedFiles = @()
        try {
            foreach ($candidate in $candidates) {
                foreach ($source in @($candidate.DumpPath, $candidate.ChecksumPath, $candidate.ManifestPath)) {
                    $destination = Join-Path $trashRoot (Split-Path -Leaf $source)
                    $movedFiles += [PSCustomObject]@{ Source = $source; Destination = $destination }
                    Move-Item -LiteralPath $source -Destination $destination
                }
            }
        }
        catch {
            $moveFailure = $_.Exception.Message
            $rollbackFailures = @()
            for ($index = $movedFiles.Count - 1; $index -ge 0; $index--) {
                $moved = $movedFiles[$index]
                if (Test-Path -LiteralPath $moved.Destination -PathType Leaf) {
                    try {
                        Move-Item -LiteralPath $moved.Destination -Destination $moved.Source -Force -ErrorAction Stop
                    }
                    catch { $rollbackFailures += $moved.Destination }
                }
            }
            if (Test-Path -LiteralPath $trashRoot -PathType Container) {
                $trashFiles = @(Get-ChildItem -LiteralPath $trashRoot -Force)
                if ($trashFiles.Count -eq 0) {
                    Remove-Item -LiteralPath $trashRoot -Force
                    $trashRoot = $null
                }
            }
            if ($rollbackFailures.Count -eq 0) {
                throw "Pemindahan transaksi retensi gagal dan rollback selesai: $moveFailure"
            }
            throw "Pemindahan transaksi dan rollback tidak lengkap; file dipertahankan di $trashRoot."
        }

        Remove-Item -LiteralPath $trashRoot -Recurse -Force
        $trashRoot = $null
        $remaining = @(Get-SiPaculBackupSet $root)
        if ($remaining.Count -ne ($backupSets.Count - $candidates.Count)) {
            Fail "Jumlah backup setelah retensi tidak sesuai."
        }
        if ($remaining.Count -lt [Math]::Min($MinimumBackups, $backupSets.Count)) {
            Fail "Retensi melanggar jumlah minimum backup."
        }
        foreach ($candidate in $candidates) {
            Write-Host "[HAPUS] Triplet kedaluwarsa: $($candidate.BackupFile)"
        }
        Write-Host "[OK] Retensi diterapkan melalui direktori transaksi yang terisolasi."
    }

    Write-Host ""
    Write-Host "=== STATUS AKHIR RETENSI ==="
    if ($Apply) {
        Write-Host "[OK] $($candidates.Count) backup kedaluwarsa dihapus; backup terbaru tetap dilindungi."
    }
    else {
        Write-Host "[OK] Tidak ada penghapusan; jalankan ulang dengan -Apply setelah hasil dry-run disetujui."
    }
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    if (-not [string]::IsNullOrWhiteSpace($trashRoot) -and (Test-Path -LiteralPath $trashRoot)) {
        Write-Host "[PERINGATAN] Direktori transaksi dipertahankan untuk pemulihan manual: $trashRoot" -ForegroundColor Yellow
    }
    exit 1
}
