param(
    [string]$TaskName = "SiPacul-PostgreSQL-Backup",
    [string]$RepositoryRoot = "",
    [string]$EnvironmentFile = ".env.production",
    [string]$ComposeFile = "compose.production.yml",
    [string]$ComposeProject = "",
    [string]$OutputDirectory = "",
    [string]$LogFile = "",
    [string]$StartTime = "02:00",
    [int]$RetentionDays = 30,
    [int]$MinimumBackups = 7,
    [double]$FreshnessHours = 26,
    [switch]$ApplyRetention,
    [switch]$VerifyAllHashes,
    [switch]$ExpectedDisabled,
    [switch]$RequireLastRunSuccess,
    [double]$MaxLastRunAgeHours = 26
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C2B Backup Task Audit 1 - PowerShell 5.1"
$modulePath = Join-Path $PSScriptRoot "SiPaculBackupTask.psm1"

function Fail([string]$Message) { throw $Message }

try {
    Write-Host "=== AUDIT TASK BACKUP SIPACUL ==="
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) { Fail "Modul task tidak ditemukan: $modulePath" }
    Import-Module -Name $modulePath -Force -ErrorAction Stop
    foreach ($command in @("Get-ScheduledTask", "Export-ScheduledTask", "Get-ScheduledTaskInfo")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { Fail "$command tidak tersedia." }
    }
    if ($MaxLastRunAgeHours -le 0 -or $MaxLastRunAgeHours -gt 8760) {
        Fail "MaxLastRunAgeHours harus berada pada rentang lebih dari 0 hingga 8760."
    }

    $contractArguments = @{
        TaskName = $TaskName
        RepositoryRoot = $RepositoryRoot
        EnvironmentFile = $EnvironmentFile
        ComposeFile = $ComposeFile
        ComposeProject = $ComposeProject
        OutputDirectory = $OutputDirectory
        LogFile = $LogFile
        StartTime = $StartTime
        RetentionDays = $RetentionDays
        MinimumBackups = $MinimumBackups
        FreshnessHours = $FreshnessHours
        ApplyRetention = $ApplyRetention
        VerifyAllHashes = $VerifyAllHashes
        Disabled = $ExpectedDisabled
    }
    $contract = New-SiPaculBackupTaskContract @contractArguments
    $errors = @(Test-SiPaculBackupTaskContract $contract)
    if ($errors.Count -ne 0) { Fail ("Kontrak task tidak cocok: " + ($errors -join "; ")) }

    $task = Get-SiPaculScheduledTask $contract.TaskName
    $info = Get-ScheduledTaskInfo -InputObject $task -ErrorAction Stop
    if ($RequireLastRunSuccess) {
        if ($info.LastRunTime -eq [DateTime]::MinValue -or $info.LastRunTime.Year -lt 2000) {
            Fail "Task belum memiliki riwayat eksekusi."
        }
        if ([int64]$info.LastTaskResult -ne 0) {
            Fail "Eksekusi terakhir gagal dengan LastTaskResult $($info.LastTaskResult)."
        }
        $lastRunAge = [DateTime]::Now - $info.LastRunTime
        if ($lastRunAge.TotalHours -lt 0 -or $lastRunAge.TotalHours -gt $MaxLastRunAgeHours) {
            Fail ("Eksekusi terakhir berusia {0:N2} jam dan melewati batas {1:N2} jam." -f $lastRunAge.TotalHours, $MaxLastRunAgeHours)
        }
    }

    $state = [string]$task.State
    $lastRunText = if ($info.LastRunTime -eq [DateTime]::MinValue -or $info.LastRunTime.Year -lt 2000) {
        "belum pernah"
    }
    else {
        $info.LastRunTime.ToString("yyyy-MM-dd HH:mm:ss")
    }
    $nextRunText = if ($info.NextRunTime -eq [DateTime]::MinValue -or $info.NextRunTime.Year -lt 2000) {
        "tidak dijadwalkan"
    }
    else {
        $info.NextRunTime.ToString("yyyy-MM-dd HH:mm:ss")
    }

    Write-Host "[OK] Repository: $($contract.RepositoryRoot)"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Task: $($contract.TaskName); state: $state; jadwal harian: $StartTime."
    Write-Host "[OK] LastRunTime: $lastRunText; LastTaskResult: $($info.LastTaskResult); NextRunTime: $nextRunText."

    Write-Host ""
    Write-Host "=== STATUS AKHIR AUDIT TASK ==="
    Write-Host "[OK] Ownership, command line, working directory, principal, trigger, dan settings cocok."
    if ($RequireLastRunSuccess) {
        Write-Host "[OK] Eksekusi terakhir sukses dan berada di bawah batas $MaxLastRunAgeHours jam."
    }
    else {
        Write-Host "[OK] Riwayat runtime hanya dilaporkan; keberhasilan last run tidak diwajibkan."
    }
    Write-Host "[OK] Audit read-only; task, repository, database, dan container tidak diubah."
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
