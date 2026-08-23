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
    [switch]$Disabled,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20C2B Backup Task Registration 1 - PowerShell 5.1"
$modulePath = Join-Path $PSScriptRoot "SiPaculBackupTask.psm1"
$contract = $null
$existingXml = $null
$registrationAttempted = $false
$registrationChanged = $false
$failureMessage = $null

function Fail([string]$Message) { throw $Message }

function Get-ContractArguments {
    $arguments = @{
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
        Disabled = $Disabled
    }
    return $arguments
}

try {
    Write-Host "=== PREFLIGHT REGISTRASI TASK BACKUP SIPACUL ==="
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) { Fail "Modul task tidak ditemukan: $modulePath" }
    Import-Module -Name $modulePath -Force -ErrorAction Stop
    foreach ($command in @(
        "New-ScheduledTaskAction", "New-ScheduledTaskTrigger", "New-ScheduledTaskPrincipal",
        "New-ScheduledTaskSettingsSet", "New-ScheduledTask", "Register-ScheduledTask",
        "Get-ScheduledTask", "Export-ScheduledTask", "Enable-ScheduledTask", "Disable-ScheduledTask",
        "Unregister-ScheduledTask"
    )) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { Fail "$command tidak tersedia." }
    }

    $contractArguments = Get-ContractArguments
    $contract = New-SiPaculBackupTaskContract @contractArguments
    $desiredState = if ($contract.Disabled) { "Disabled" } else { "Enabled" }
    $existing = Get-SiPaculScheduledTask $contract.TaskName
    if ($null -ne $existing) {
        if ([string]$existing.Description -cne $contract.Description) {
            Fail "TaskName sudah dipakai task yang tidak dikelola SiPacul untuk repository ini."
        }
        $existingErrors = @(Test-SiPaculBackupTaskContract $contract)
        if ($existingErrors.Count -eq 0) {
            Write-Host "[OK] Repository: $($contract.RepositoryRoot)"
            Write-Host "[OK] Script revision: $Revision"
            Write-Host "[OK] Task $($contract.TaskName) sudah terdaftar dengan kontrak identik; tidak ada perubahan."
            Write-Host ""
            Write-Host "=== STATUS AKHIR REGISTRASI TASK ==="
            Write-Host "[OK] Task tetap $($desiredState.ToLowerInvariant())."
            Write-Host "[OK] Command line, principal, trigger, settings, dan ownership marker tervalidasi."
            exit 0
        }
        if (-not $Force) {
            Fail ("Task sudah terdaftar tetapi kontraknya berbeda; gunakan -Force setelah memeriksa konfigurasi. Detail: " + ($existingErrors -join "; "))
        }
        $existingXml = Export-ScheduledTask -TaskPath $contract.TaskPath -TaskName $contract.TaskName -ErrorAction Stop
    }

    [IO.Directory]::CreateDirectory($contract.OutputDirectory) | Out-Null
    [IO.Directory]::CreateDirectory((Split-Path -Parent $contract.LogFile)) | Out-Null

    Write-Host "[OK] Repository: $($contract.RepositoryRoot)"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Task berjalan sebagai $($contract.UserName), interactive token, dan least privilege."
    Write-Host "[OK] Output/log berada di luar repository; tidak ada kata sandi yang disimpan."

    Write-Host ""
    Write-Host "=== MENDAFTARKAN TASK BACKUP ==="
    $action = New-ScheduledTaskAction `
        -Execute $contract.PowerShellPath `
        -Argument $contract.Arguments `
        -WorkingDirectory $contract.RepositoryRoot
    $trigger = New-ScheduledTaskTrigger -Daily -At $contract.StartAt -DaysInterval 1
    $principal = New-ScheduledTaskPrincipal `
        -UserId $contract.UserName `
        -LogonType Interactive `
        -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet `
        -StartWhenAvailable `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -ExecutionTimeLimit (New-TimeSpan -Hours 4) `
        -MultipleInstances IgnoreNew `
        -RestartCount 2 `
        -RestartInterval (New-TimeSpan -Minutes 10)
    $definition = New-ScheduledTask `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Description $contract.Description

    $registrationAttempted = $true
    if ($null -eq $existing) {
        Register-ScheduledTask `
            -TaskPath $contract.TaskPath `
            -TaskName $contract.TaskName `
            -InputObject $definition | Out-Null
    }
    else {
        Register-ScheduledTask `
            -TaskPath $contract.TaskPath `
            -TaskName $contract.TaskName `
            -InputObject $definition `
            -Force | Out-Null
    }
    $registrationChanged = $true
    if ($contract.Disabled) {
        Disable-ScheduledTask -TaskPath $contract.TaskPath -TaskName $contract.TaskName | Out-Null
    }
    else {
        Enable-ScheduledTask -TaskPath $contract.TaskPath -TaskName $contract.TaskName | Out-Null
    }

    $errors = @(Test-SiPaculBackupTaskContract $contract)
    if ($errors.Count -ne 0) { Fail ("Kontrak task hasil registrasi tidak cocok: " + ($errors -join "; ")) }
    Write-Host "[OK] Task $($contract.TaskName) terdaftar pada jadwal harian $StartTime."
    Write-Host "[OK] State: $desiredState."

    Write-Host ""
    Write-Host "=== STATUS AKHIR REGISTRASI TASK ==="
    Write-Host "[OK] Command line, principal, trigger, settings, dan ownership marker tervalidasi."
    Write-Host "[OK] MultipleInstances=IgnoreNew; restart 2x/10 menit; execution limit 4 jam."
    Write-Host "[OK] Repository, source, staging, database, dan container tidak diubah."
}
catch {
    $failureMessage = $_.Exception.Message
    if (($registrationAttempted -or $registrationChanged) -and $null -ne $contract) {
        $previous = $ErrorActionPreference
        try {
            $ErrorActionPreference = "Continue"
            if (-not [string]::IsNullOrWhiteSpace($existingXml)) {
                Register-ScheduledTask `
                    -TaskPath $contract.TaskPath `
                    -TaskName $contract.TaskName `
                    -Xml $existingXml `
                    -Force | Out-Null
                $rollbackExit = if ($?) { 0 } else { 1 }
            }
            else {
                $registered = Get-SiPaculScheduledTask $contract.TaskName
                if ($null -eq $registered) {
                    $rollbackExit = 0
                }
                elseif ([string]$registered.Description -ceq $contract.Description) {
                    Unregister-ScheduledTask `
                        -TaskPath $contract.TaskPath `
                        -TaskName $contract.TaskName `
                        -Confirm:$false
                    $remaining = Get-SiPaculScheduledTask $contract.TaskName
                    $rollbackExit = if ($null -eq $remaining) { 0 } else { 1 }
                }
                else {
                    $rollbackExit = 1
                }
            }
        }
        catch { $rollbackExit = 1 }
        finally { $ErrorActionPreference = $previous }
        Write-Host ""
        if ($rollbackExit -eq 0) {
            Write-Host "[ROLLBACK] Registrasi task dipulihkan ke state sebelumnya." -ForegroundColor Yellow
        }
        else {
            Write-Host "[PERINGATAN] Registrasi task gagal dipulihkan otomatis." -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "[GAGAL] $failureMessage" -ForegroundColor Red
    exit 1
}
