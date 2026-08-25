Set-StrictMode -Version 2.0

$script:SiPaculTaskPath = "\"
$script:SiPaculTaskDescriptionPrefix = "SiPacul.BackupTask/v1"

function Invoke-SiPaculGit {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& git.exe @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) {
        throw ("git.exe gagal: " + (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Get-SiPaculOneLine {
    param(
        [Parameter(Mandatory = $true)][string[]]$Output,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $values = @($Output |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($values.Count -ne 1) { throw "$Label harus menghasilkan tepat satu nilai." }
    return [string]$values[0]
}

function Assert-SiPaculSingleLineValue {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Label,
        [switch]$AllowEmpty
    )

    if (-not $AllowEmpty -and [string]::IsNullOrWhiteSpace($Value)) {
        throw "$Label tidak boleh kosong."
    }
    if ($Value.IndexOf([char]0) -ge 0 -or $Value.IndexOf("`r") -ge 0 -or $Value.IndexOf("`n") -ge 0) {
        throw "$Label harus berupa satu baris."
    }
    if ($Value.IndexOf('"') -ge 0) { throw "$Label tidak boleh memuat tanda kutip ganda." }
}

function Assert-SiPaculTaskName {
    param([Parameter(Mandatory = $true)][string]$TaskName)

    Assert-SiPaculSingleLineValue -Value $TaskName -Label "TaskName"
    if ($TaskName.Length -gt 120) { throw "TaskName terlalu panjang." }
    if ($TaskName -match '[\\/:*?"<>|]' -or $TaskName.Trim() -ne $TaskName) {
        throw "TaskName memuat karakter yang tidak diizinkan atau spasi terminal."
    }
    if ($TaskName -eq "." -or $TaskName -eq "..") { throw "TaskName tidak valid." }
}

function Test-SiPaculPathInsideRepository {
    param(
        [Parameter(Mandatory = $true)][string]$CandidatePath,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot
    )

    $trimCharacters = [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $candidate = [IO.Path]::GetFullPath($CandidatePath).TrimEnd($trimCharacters)
    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd($trimCharacters)
    if ($candidate.Equals($root, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    return $candidate.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Resolve-SiPaculRepositoryRoot {
    param([AllowEmptyString()][string]$RepositoryRoot = "")

    if (-not (Get-Command git.exe -ErrorAction SilentlyContinue)) { throw "git.exe tidak ditemukan." }
    if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        $root = Get-SiPaculOneLine -Output (Invoke-SiPaculGit @("rev-parse", "--show-toplevel")) -Label "Repository root"
    }
    else {
        $root = Get-SiPaculOneLine -Output (Invoke-SiPaculGit @("-C", $RepositoryRoot, "rev-parse", "--show-toplevel")) -Label "Repository root"
    }
    return [IO.Path]::GetFullPath($root)
}

function Get-SiPaculSha256Text {
    param([Parameter(Mandatory = $true)][string]$Value)

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace("-", "")
    }
    finally { $sha.Dispose() }
}

function Get-SiPaculBackupTaskMarker {
    param([AllowEmptyString()][string]$RepositoryRoot = "")

    $root = Resolve-SiPaculRepositoryRoot $RepositoryRoot
    $normalized = $root.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)).ToUpperInvariant()
    return $script:SiPaculTaskDescriptionPrefix + "; RepositorySha256=" + (Get-SiPaculSha256Text $normalized)
}

function ConvertTo-SiPaculQuotedTaskArgument {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Assert-SiPaculSingleLineValue -Value $Value -Label $Label -AllowEmpty
    return '"' + $Value + '"'
}

function New-SiPaculBackupTaskContract {
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
        [switch]$Disabled
    )

    Assert-SiPaculTaskName $TaskName
    foreach ($item in @(
        @{ Value = $EnvironmentFile; Label = "EnvironmentFile"; Empty = $false },
        @{ Value = $ComposeFile; Label = "ComposeFile"; Empty = $false },
        @{ Value = $ComposeProject; Label = "ComposeProject"; Empty = $true },
        @{ Value = $StartTime; Label = "StartTime"; Empty = $false }
    )) {
        Assert-SiPaculSingleLineValue -Value ([string]$item.Value) -Label ([string]$item.Label) -AllowEmpty:([bool]$item.Empty)
    }
    if ($RetentionDays -lt 0 -or $RetentionDays -gt 3650) { throw "RetentionDays harus berada pada rentang 0-3650." }
    if ($MinimumBackups -lt 1 -or $MinimumBackups -gt 10000) { throw "MinimumBackups harus berada pada rentang 1-10000." }
    if ($FreshnessHours -le 0 -or $FreshnessHours -gt 8760) { throw "FreshnessHours harus berada pada rentang lebih dari 0 hingga 8760." }
    if (-not [string]::IsNullOrWhiteSpace($ComposeProject) -and $ComposeProject -notmatch '^[a-z0-9][a-z0-9_-]*$') {
        throw "ComposeProject hanya boleh memuat huruf kecil, angka, underscore, dan hyphen."
    }

    $parsedTime = [DateTime]::MinValue
    if (-not [DateTime]::TryParseExact(
        $StartTime,
        "HH:mm",
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::None,
        [ref]$parsedTime
    )) {
        throw "StartTime harus berformat HH:mm, misalnya 02:00."
    }

    $root = Resolve-SiPaculRepositoryRoot $RepositoryRoot
    $cycleScript = Join-Path $root "operations\Invoke-SiPaculBackupCycle-PS51.ps1"
    if (-not (Test-Path -LiteralPath $cycleScript -PathType Leaf)) {
        throw "Skrip siklus backup tidak ditemukan: $cycleScript"
    }
    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
        if ([string]::IsNullOrWhiteSpace($env:USERPROFILE)) { throw "USERPROFILE tidak tersedia." }
        $OutputDirectory = Join-Path $env:USERPROFILE "SiPaculBackups"
    }
    $outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
    if (Test-SiPaculPathInsideRepository -CandidatePath $outputRoot -RepositoryRoot $root) {
        throw "OutputDirectory harus berada di luar repository."
    }
    $trimCharacters = [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $volumeRoot = [IO.Path]::GetPathRoot($outputRoot).TrimEnd($trimCharacters)
    if ($outputRoot.TrimEnd($trimCharacters).Equals($volumeRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "OutputDirectory tidak boleh berupa root volume."
    }
    $outputRoot = $outputRoot.TrimEnd($trimCharacters)

    if ([string]::IsNullOrWhiteSpace($LogFile)) {
        $logPath = Join-Path (Join-Path $outputRoot "logs") "backup-cycle.log"
    }
    else {
        $logPath = [IO.Path]::GetFullPath($LogFile)
    }
    if (Test-SiPaculPathInsideRepository -CandidatePath $logPath -RepositoryRoot $root) {
        throw "LogFile harus berada di luar repository."
    }
    $logParent = Split-Path -Parent $logPath
    if ([string]::IsNullOrWhiteSpace($logParent)) { throw "Parent LogFile tidak valid." }

    foreach ($item in @(
        @{ Value = $root; Label = "RepositoryRoot" },
        @{ Value = $cycleScript; Label = "CycleScript" },
        @{ Value = $outputRoot; Label = "OutputDirectory" },
        @{ Value = $logPath; Label = "LogFile" }
    )) {
        Assert-SiPaculSingleLineValue -Value ([string]$item.Value) -Label ([string]$item.Label)
    }

    $powerShellCommand = Get-Command powershell.exe -ErrorAction SilentlyContinue
    if ($null -eq $powerShellCommand -or [string]::IsNullOrWhiteSpace($powerShellCommand.Source)) {
        throw "powershell.exe tidak ditemukan."
    }
    $powerShellPath = [IO.Path]::GetFullPath($powerShellCommand.Source)
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $parts = @(
        "-NoProfile",
        "-NonInteractive",
        "-ExecutionPolicy", "Bypass",
        "-File", (ConvertTo-SiPaculQuotedTaskArgument $cycleScript "CycleScript"),
        "-RepositoryRoot", (ConvertTo-SiPaculQuotedTaskArgument $root "RepositoryRoot"),
        "-EnvironmentFile", (ConvertTo-SiPaculQuotedTaskArgument $EnvironmentFile "EnvironmentFile"),
        "-ComposeFile", (ConvertTo-SiPaculQuotedTaskArgument $ComposeFile "ComposeFile")
    )
    if (-not [string]::IsNullOrWhiteSpace($ComposeProject)) {
        $parts += @("-ComposeProject", (ConvertTo-SiPaculQuotedTaskArgument $ComposeProject "ComposeProject"))
    }
    $parts += @(
        "-OutputDirectory", (ConvertTo-SiPaculQuotedTaskArgument $outputRoot "OutputDirectory"),
        "-LogFile", (ConvertTo-SiPaculQuotedTaskArgument $logPath "LogFile"),
        "-RetentionDays", $RetentionDays.ToString($culture),
        "-MinimumBackups", $MinimumBackups.ToString($culture),
        "-FreshnessHours", $FreshnessHours.ToString("0.###############", $culture)
    )
    if ($ApplyRetention) { $parts += "-ApplyRetention" }
    if ($VerifyAllHashes) { $parts += "-VerifyAllHashes" }

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    if ($null -eq $identity -or $null -eq $identity.User -or [string]::IsNullOrWhiteSpace($identity.Name)) {
        throw "Identitas Windows aktif tidak dapat ditentukan."
    }

    return [PSCustomObject]@{
        PSTypeName = "SiPacul.BackupTaskContract"
        TaskName = $TaskName
        TaskPath = $script:SiPaculTaskPath
        RepositoryRoot = $root
        CycleScript = $cycleScript
        OutputDirectory = $outputRoot
        LogFile = $logPath
        StartAt = [DateTime]::Today.Add($parsedTime.TimeOfDay)
        PowerShellPath = $powerShellPath
        Arguments = ($parts -join " ")
        Description = Get-SiPaculBackupTaskMarker $root
        UserName = $identity.Name
        UserSid = $identity.User.Value
        Disabled = [bool]$Disabled
        RetentionDays = $RetentionDays
        MinimumBackups = $MinimumBackups
        FreshnessHours = $FreshnessHours
        ApplyRetention = [bool]$ApplyRetention
        VerifyAllHashes = [bool]$VerifyAllHashes
    }
}

function Get-SiPaculScheduledTask {
    param([Parameter(Mandatory = $true)][string]$TaskName)

    Assert-SiPaculTaskName $TaskName
    $tasks = @(Get-ScheduledTask -TaskPath $script:SiPaculTaskPath -TaskName $TaskName -ErrorAction SilentlyContinue)
    if ($tasks.Count -gt 1) { throw "TaskName menghasilkan lebih dari satu task pada root Task Scheduler." }
    if ($tasks.Count -eq 0) { return $null }
    return $tasks[0]
}

function Get-SiPaculXmlNodeText {
    param(
        [Parameter(Mandatory = $true)][Xml.XmlDocument]$Document,
        [Parameter(Mandatory = $true)][Xml.XmlNamespaceManager]$NamespaceManager,
        [Parameter(Mandatory = $true)][string]$XPath
    )

    $node = $Document.SelectSingleNode($XPath, $NamespaceManager)
    if ($null -eq $node) { return $null }
    return [string]$node.InnerText
}

function Test-SiPaculBackupTaskContract {
    param([Parameter(Mandatory = $true)]$Contract)

    $errors = New-Object Collections.Generic.List[string]
    $task = Get-SiPaculScheduledTask $Contract.TaskName
    if ($null -eq $task) {
        $errors.Add("Task tidak ditemukan pada root Task Scheduler.")
        return @($errors)
    }

    $xmlText = Export-ScheduledTask -TaskPath $Contract.TaskPath -TaskName $Contract.TaskName -ErrorAction Stop
    $document = New-Object Xml.XmlDocument
    $document.PreserveWhitespace = $true
    $document.LoadXml($xmlText)
    $namespace = New-Object Xml.XmlNamespaceManager($document.NameTable)
    $namespace.AddNamespace("t", $document.DocumentElement.NamespaceURI)

    $description = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:RegistrationInfo/t:Description"
    if ($description -cne $Contract.Description) { $errors.Add("Description/ownership marker tidak cocok.") }

    $actions = @($document.SelectNodes("/t:Task/t:Actions/t:Exec", $namespace))
    if ($actions.Count -ne 1) {
        $errors.Add("Task harus memiliki tepat satu action Exec.")
    }
    else {
        $command = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Actions/t:Exec/t:Command"
        $arguments = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Actions/t:Exec/t:Arguments"
        $workingDirectory = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Actions/t:Exec/t:WorkingDirectory"
        if ([string]::IsNullOrWhiteSpace($command) -or
            -not ([IO.Path]::GetFullPath($command).Equals($Contract.PowerShellPath, [StringComparison]::OrdinalIgnoreCase))) {
            $errors.Add("Executable PowerShell tidak cocok.")
        }
        if ($arguments -cne $Contract.Arguments) { $errors.Add("Command line backup tidak cocok.") }
        if ([string]::IsNullOrWhiteSpace($workingDirectory) -or
            -not ([IO.Path]::GetFullPath($workingDirectory).Equals($Contract.RepositoryRoot, [StringComparison]::OrdinalIgnoreCase))) {
            $errors.Add("WorkingDirectory tidak cocok.")
        }
    }

    $triggers = @($document.SelectNodes("/t:Task/t:Triggers/t:CalendarTrigger", $namespace))
    if ($triggers.Count -ne 1) {
        $errors.Add("Task harus memiliki tepat satu CalendarTrigger.")
    }
    else {
        $startBoundary = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Triggers/t:CalendarTrigger/t:StartBoundary"
        $daysInterval = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Triggers/t:CalendarTrigger/t:ScheduleByDay/t:DaysInterval"
        $parsedBoundary = [DateTime]::MinValue
        if ([string]::IsNullOrWhiteSpace($startBoundary) -or -not [DateTime]::TryParse($startBoundary, [ref]$parsedBoundary)) {
            $errors.Add("StartBoundary tidak valid.")
        }
        elseif ([Math]::Abs(($parsedBoundary.TimeOfDay - $Contract.StartAt.TimeOfDay).TotalSeconds) -gt 1) {
            $errors.Add("Waktu trigger harian tidak cocok.")
        }
        if ($daysInterval -ne "1") { $errors.Add("Trigger bukan interval harian satu hari.") }
    }

    $userId = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Principals/t:Principal/t:UserId"
    $logonType = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Principals/t:Principal/t:LogonType"
    $runLevel = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Principals/t:Principal/t:RunLevel"
    $principalRunLevel = [string]$task.Principal.RunLevel
    if (-not ($userId -ieq $Contract.UserName) -and -not ($userId -ieq $Contract.UserSid)) {
        $errors.Add("Principal task bukan akun Windows aktif.")
    }
    if ($logonType -notin @("Interactive", "InteractiveToken")) { $errors.Add("LogonType bukan interactive token.") }
    $effectiveRunLevel = if ([string]::IsNullOrWhiteSpace($runLevel)) { $principalRunLevel } else { $runLevel }
    if ($effectiveRunLevel -notin @("0", "Limited", "LeastPrivilege")) {
        $errors.Add("RunLevel bukan limited/least privilege (XML='$runLevel'; principal='$principalRunLevel').")
    }

    $multipleInstances = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:MultipleInstancesPolicy"
    $startWhenAvailable = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:StartWhenAvailable"
    $disallowOnBatteries = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:DisallowStartIfOnBatteries"
    $stopOnBatteries = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:StopIfGoingOnBatteries"
    $executionLimit = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:ExecutionTimeLimit"
    $restartInterval = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:RestartOnFailure/t:Interval"
    $restartCount = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:RestartOnFailure/t:Count"
    $enabled = Get-SiPaculXmlNodeText $document $namespace "/t:Task/t:Settings/t:Enabled"
    if ($multipleInstances -ne "IgnoreNew") { $errors.Add("MultipleInstancesPolicy bukan IgnoreNew.") }
    if ($startWhenAvailable -ne "true") { $errors.Add("StartWhenAvailable tidak aktif.") }
    if ($disallowOnBatteries -ne "false" -or $stopOnBatteries -ne "false") {
        $errors.Add("Pengaturan baterai tidak cocok.")
    }
    try {
        if ([Xml.XmlConvert]::ToTimeSpan($executionLimit) -ne (New-TimeSpan -Hours 4)) {
            $errors.Add("ExecutionTimeLimit bukan empat jam.")
        }
    }
    catch { $errors.Add("ExecutionTimeLimit tidak valid.") }
    try {
        if ([Xml.XmlConvert]::ToTimeSpan($restartInterval) -ne (New-TimeSpan -Minutes 10)) {
            $errors.Add("RestartInterval bukan sepuluh menit.")
        }
    }
    catch { $errors.Add("RestartInterval tidak valid.") }
    if ($restartCount -ne "2") { $errors.Add("RestartCount bukan dua.") }

    if ($Contract.Disabled) {
        if ($enabled -ne "false" -or [string]$task.State -ne "Disabled") {
            $errors.Add("Task seharusnya disabled.")
        }
    }
    else {
        # State CIM adalah sumber kebenaran; node Enabled dapat hilang atau tertinggal setelah transisi.
        if ([string]$task.State -eq "Disabled") {
            $errors.Add("Task seharusnya enabled.")
        }
    }

    return @($errors)
}

Export-ModuleMember -Function @(
    "Resolve-SiPaculRepositoryRoot",
    "Get-SiPaculBackupTaskMarker",
    "New-SiPaculBackupTaskContract",
    "Get-SiPaculScheduledTask",
    "Test-SiPaculBackupTaskContract"
)
