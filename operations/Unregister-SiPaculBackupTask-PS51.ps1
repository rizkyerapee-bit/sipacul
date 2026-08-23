param(
    [string]$TaskName = "SiPacul-PostgreSQL-Backup",
    [string]$RepositoryRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C2B Backup Task Unregister 1 - PowerShell 5.1"
$modulePath = Join-Path $PSScriptRoot "SiPaculBackupTask.psm1"

function Fail([string]$Message) { throw $Message }

try {
    Write-Host "=== UNREGISTER TASK BACKUP SIPACUL ==="
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) { Fail "Modul task tidak ditemukan: $modulePath" }
    Import-Module -Name $modulePath -Force -ErrorAction Stop
    foreach ($command in @("Get-ScheduledTask", "Unregister-ScheduledTask")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { Fail "$command tidak tersedia." }
    }

    $root = Resolve-SiPaculRepositoryRoot $RepositoryRoot
    $expectedMarker = Get-SiPaculBackupTaskMarker $root
    $task = Get-SiPaculScheduledTask $TaskName
    Write-Host "[OK] Repository: $root"
    Write-Host "[OK] Script revision: $Revision"
    if ($null -eq $task) {
        Write-Host "[OK] Task $TaskName sudah tidak terdaftar; tidak ada perubahan."
        Write-Host ""
        Write-Host "=== STATUS AKHIR UNREGISTER TASK ==="
        Write-Host "[OK] State akhir idempotent: task tidak ada."
        exit 0
    }
    if ([string]$task.Description -cne $expectedMarker) {
        Fail "Task tidak memiliki ownership marker SiPacul untuk repository ini; penghapusan ditolak."
    }
    if ([string]$task.State -eq "Running") {
        Fail "Task sedang berjalan; tunggu siklus selesai sebelum unregister."
    }

    Write-Host "[OK] Ownership marker cocok dan task tidak sedang berjalan."
    Unregister-ScheduledTask -TaskPath "\" -TaskName $TaskName -Confirm:$false
    if ($null -ne (Get-SiPaculScheduledTask $TaskName)) { Fail "Task masih terdaftar setelah unregister." }

    Write-Host ""
    Write-Host "=== STATUS AKHIR UNREGISTER TASK ==="
    Write-Host "[OK] Task $TaskName dihapus dari root Task Scheduler."
    Write-Host "[OK] Backup, log, repository, database, dan container tidak dihapus atau diubah."
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
