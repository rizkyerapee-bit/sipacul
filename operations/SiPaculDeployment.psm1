Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Fail-SiPaculDeployment {
    param([Parameter(Mandatory=$true)][string]$Message)
    throw $Message
}

function Resolve-SiPaculRepositoryRoot {
    param([string]$RepositoryRoot = "")

    if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        $RepositoryRoot = Split-Path -Parent $PSScriptRoot
    }

    $full = [IO.Path]::GetFullPath($RepositoryRoot)
    if (-not (Test-Path -LiteralPath $full -PathType Container)) {
        Fail-SiPaculDeployment "Repository tidak ditemukan: $full"
    }

    if (-not (Test-Path -LiteralPath (Join-Path $full "compose.production.yml") -PathType Leaf)) {
        Fail-SiPaculDeployment "compose.production.yml tidak ditemukan di repository: $full"
    }

    return $full.TrimEnd("\")
}

function Resolve-SiPaculFilePath {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$BaseDirectory,
        [Parameter(Mandatory=$true)][string]$Label
    )

    $candidate = $Path
    if (-not [IO.Path]::IsPathRooted($candidate)) {
        $candidate = Join-Path $BaseDirectory $candidate
    }

    $full = [IO.Path]::GetFullPath($candidate)
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        Fail-SiPaculDeployment "$Label tidak ditemukan: $full"
    }

    return $full
}

function Resolve-SiPaculDirectoryPath {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$BaseDirectory
    )

    $candidate = $Path
    if (-not [IO.Path]::IsPathRooted($candidate)) {
        $candidate = Join-Path $BaseDirectory $candidate
    }

    return [IO.Path]::GetFullPath($candidate).TrimEnd("\")
}

function Assert-SiPaculOutsideRepository {
    param(
        [Parameter(Mandatory=$true)][string]$RepositoryRoot,
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Label
    )

    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd("\") + "\"
    $full = [IO.Path]::GetFullPath($Path)

    if ($full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        Fail-SiPaculDeployment "$Label wajib berada di luar repository: $full"
    }
}

function Invoke-SiPaculNative {
    param(
        [Parameter(Mandatory=$true)][string]$Command,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [switch]$AllowFailure,
        [switch]$Capture
    )

    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        if ($Capture) {
            $output = @(& $Command @Arguments 2>&1)
        }
        else {
            & $Command @Arguments
            $output = @()
        }
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }

    $result = New-Object PSObject -Property @{
        ExitCode = $exitCode
        Output = @($output | ForEach-Object { [string]$_ })
    }

    if (-not $AllowFailure -and $exitCode -ne 0) {
        $details = if ($result.Output.Count -gt 0) {
            "`n" + ($result.Output -join "`n")
        }
        else {
            ""
        }
        Fail-SiPaculDeployment ("{0} {1} gagal dengan exit code {2}.{3}" -f `
            $Command,
            ($Arguments -join " "),
            $exitCode,
            $details)
    }

    return $result
}

function Invoke-SiPaculDocker {
    param(
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [switch]$AllowFailure,
        [switch]$Capture
    )

    if (-not (Get-Command docker.exe -ErrorAction SilentlyContinue)) {
        Fail-SiPaculDeployment "docker.exe tidak ditemukan."
    }

    return Invoke-SiPaculNative `
        -Command "docker.exe" `
        -Arguments $Arguments `
        -AllowFailure:$AllowFailure `
        -Capture:$Capture
}

function Get-SiPaculComposeArguments {
    param(
        [Parameter(Mandatory=$true)][string]$EnvironmentFile,
        [string]$ReleaseEnvironmentFile = "",
        [Parameter(Mandatory=$true)][string]$ComposeFile,
        [Parameter(Mandatory=$true)][string]$ComposeProject,
        [Parameter(Mandatory=$true)][string[]]$Arguments
    )

    $all = @("compose", "--env-file", $EnvironmentFile)

    if (-not [string]::IsNullOrWhiteSpace($ReleaseEnvironmentFile)) {
        $all += @("--env-file", $ReleaseEnvironmentFile)
    }

    $all += @(
        "--file", $ComposeFile,
        "--project-name", $ComposeProject
    )
    $all += $Arguments
    return $all
}

function Invoke-SiPaculCompose {
    param(
        [Parameter(Mandatory=$true)][string]$EnvironmentFile,
        [string]$ReleaseEnvironmentFile = "",
        [Parameter(Mandatory=$true)][string]$ComposeFile,
        [Parameter(Mandatory=$true)][string]$ComposeProject,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [switch]$AllowFailure,
        [switch]$Capture
    )

    $all = Get-SiPaculComposeArguments `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $ReleaseEnvironmentFile `
        -ComposeFile $ComposeFile `
        -ComposeProject $ComposeProject `
        -Arguments $Arguments

    return Invoke-SiPaculDocker `
        -Arguments $all `
        -AllowFailure:$AllowFailure `
        -Capture:$Capture
}

function Assert-SiPaculGitClean {
    param([Parameter(Mandatory=$true)][string]$RepositoryRoot)

    if (-not (Get-Command git.exe -ErrorAction SilentlyContinue)) {
        Fail-SiPaculDeployment "git.exe tidak ditemukan."
    }

    $previous = Get-Location
    try {
        Set-Location -LiteralPath $RepositoryRoot
        $status = Invoke-SiPaculNative `
            -Command "git.exe" `
            -Arguments @("status", "--porcelain=v1") `
            -Capture

        if ($status.Output.Count -ne 0) {
            Fail-SiPaculDeployment ("Working tree/staging repository tidak bersih:`n" + ($status.Output -join "`n"))
        }
    }
    finally {
        Set-Location -LiteralPath $previous
    }
}

function Read-SiPaculEnvironmentFile {
    param([Parameter(Mandatory=$true)][string]$Path)

    $result = @{}
    $lines = @(Get-Content -LiteralPath $Path)

    foreach ($rawLine in $lines) {
        $line = ([string]$rawLine).Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
            continue
        }

        if ($line -notmatch '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
            Fail-SiPaculDeployment "Baris environment tidak valid pada ${Path}: $rawLine"
        }

        $name = $Matches[1]
        $value = $Matches[2]

        if ($result.ContainsKey($name)) {
            Fail-SiPaculDeployment "Environment variable duplikat pada ${Path}: $name"
        }

        $result[$name] = $value
    }

    return $result
}

function Assert-SiPaculProductionEnvironment {
    param([Parameter(Mandatory=$true)][string]$EnvironmentFile)

    $envMap = Read-SiPaculEnvironmentFile -Path $EnvironmentFile

    foreach ($name in @(
        "POSTGRES_DB",
        "POSTGRES_USER",
        "POSTGRES_PASSWORD",
        "SIPACUL_BOOTSTRAP_OWNER_TOKEN",
        "SIPACUL_BIND_ADDRESS",
        "SIPACUL_HTTPS_PORT",
        "SIPACUL_APPLICATION_SUBNET",
        "SIPACUL_EDGE_IP",
        "SIPACUL_TLS_CERTIFICATE_PATH",
        "SIPACUL_TLS_PRIVATE_KEY_PATH"
    )) {
        if (-not $envMap.ContainsKey($name) -or
            [string]::IsNullOrWhiteSpace([string]$envMap[$name])) {
            Fail-SiPaculDeployment "Environment variable wajib tidak tersedia: $name"
        }

        if ([string]$envMap[$name] -match 'REPLACE_ME') {
            Fail-SiPaculDeployment "Environment variable masih memakai placeholder: $name"
        }
    }

    foreach ($pathName in @(
        "SIPACUL_TLS_CERTIFICATE_PATH",
        "SIPACUL_TLS_PRIVATE_KEY_PATH"
    )) {
        $tlsPath = [string]$envMap[$pathName]
        if (-not [IO.Path]::IsPathRooted($tlsPath)) {
            Fail-SiPaculDeployment "$pathName harus berupa path absolut."
        }
        if (-not (Test-Path -LiteralPath $tlsPath -PathType Leaf)) {
            Fail-SiPaculDeployment "$pathName tidak ditemukan: $tlsPath"
        }
    }

    return $envMap
}

function Normalize-SiPaculReleaseSha {
    param([Parameter(Mandatory=$true)][string]$ReleaseSha)

    $value = $ReleaseSha.Trim().ToLowerInvariant()
    if ($value -notmatch '^[0-9a-f]{40}$') {
        Fail-SiPaculDeployment "ReleaseSha harus full Git SHA 40 karakter hexadecimal."
    }

    return $value
}

function Get-SiPaculReleaseImageMap {
    param(
        [Parameter(Mandatory=$true)][string]$ReleaseSha,
        [Parameter(Mandatory=$true)][string]$RegistryOwner
    )

    $sha = Normalize-SiPaculReleaseSha -ReleaseSha $ReleaseSha
    $owner = $RegistryOwner.Trim().ToLowerInvariant()

    if ($owner -notmatch '^[a-z0-9][a-z0-9._-]*$') {
        Fail-SiPaculDeployment "RegistryOwner tidak valid: $RegistryOwner"
    }

    $prefix = "ghcr.io/$owner"

    return [ordered]@{
        SIPACUL_MIGRATOR_IMAGE = "$prefix/sipacul-migrator:sha-$sha"
        SIPACUL_API_IMAGE = "$prefix/sipacul-api:sha-$sha"
        SIPACUL_FRONTEND_IMAGE = "$prefix/sipacul-frontend:sha-$sha"
        SIPACUL_EDGE_IMAGE = "$prefix/sipacul-edge:sha-$sha"
    }
}

function Get-SiPaculSplitReleaseImageMap {
    param(
        [Parameter(Mandatory=$true)][string]$DatabaseReleaseSha,
        [Parameter(Mandatory=$true)][string]$RuntimeReleaseSha,
        [Parameter(Mandatory=$true)][string]$RegistryOwner
    )

    $db = Get-SiPaculReleaseImageMap -ReleaseSha $DatabaseReleaseSha -RegistryOwner $RegistryOwner
    $runtime = Get-SiPaculReleaseImageMap -ReleaseSha $RuntimeReleaseSha -RegistryOwner $RegistryOwner

    return [ordered]@{
        SIPACUL_MIGRATOR_IMAGE = $db.SIPACUL_MIGRATOR_IMAGE
        SIPACUL_API_IMAGE = $runtime.SIPACUL_API_IMAGE
        SIPACUL_FRONTEND_IMAGE = $runtime.SIPACUL_FRONTEND_IMAGE
        SIPACUL_EDGE_IMAGE = $runtime.SIPACUL_EDGE_IMAGE
    }
}

function Write-SiPaculReleaseEnvironment {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][System.Collections.IDictionary]$ImageMap,
        [Parameter(Mandatory=$true)][string]$DatabaseReleaseSha,
        [Parameter(Mandatory=$true)][string]$RuntimeReleaseSha
    )

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $lines = @(
        "# Managed by SiPacul deployment operations. Contains image references only.",
        "# Database release SHA: $DatabaseReleaseSha",
        "# Runtime release SHA: $RuntimeReleaseSha",
        "SIPACUL_MIGRATOR_IMAGE=$($ImageMap.SIPACUL_MIGRATOR_IMAGE)",
        "SIPACUL_API_IMAGE=$($ImageMap.SIPACUL_API_IMAGE)",
        "SIPACUL_FRONTEND_IMAGE=$($ImageMap.SIPACUL_FRONTEND_IMAGE)",
        "SIPACUL_EDGE_IMAGE=$($ImageMap.SIPACUL_EDGE_IMAGE)"
    )

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, (($lines -join "`n") + "`n"), $utf8NoBom)
}

function Assert-SiPaculReleaseEnvironmentMatchesState {
    param(
        [Parameter(Mandatory=$true)][string]$ReleaseEnvironmentFile,
        [Parameter(Mandatory=$true)]$State
    )

    if (-not (Test-Path -LiteralPath $ReleaseEnvironmentFile -PathType Leaf)) {
        Fail-SiPaculDeployment "Release environment tidak ditemukan: $ReleaseEnvironmentFile"
    }

    $map = Read-SiPaculEnvironmentFile -Path $ReleaseEnvironmentFile
    $expected = Get-SiPaculSplitReleaseImageMap `
        -DatabaseReleaseSha ([string]$State.databaseReleaseSha) `
        -RuntimeReleaseSha ([string]$State.runtimeReleaseSha) `
        -RegistryOwner ([string]$State.registryOwner)

    foreach ($name in @(
        "SIPACUL_MIGRATOR_IMAGE",
        "SIPACUL_API_IMAGE",
        "SIPACUL_FRONTEND_IMAGE",
        "SIPACUL_EDGE_IMAGE"
    )) {
        if (-not $map.ContainsKey($name) -or
            [string]$map[$name] -cne [string]$expected[$name]) {
            Fail-SiPaculDeployment "Release environment tidak cocok dengan deployment state pada $name."
        }
    }
}

function Invoke-SiPaculComposeConfigWithImageMap {
    param(
        [Parameter(Mandatory=$true)][string]$EnvironmentFile,
        [Parameter(Mandatory=$true)][string]$ComposeFile,
        [Parameter(Mandatory=$true)][string]$ComposeProject,
        [Parameter(Mandatory=$true)][System.Collections.IDictionary]$ImageMap
    )

    $names = @(
        "SIPACUL_MIGRATOR_IMAGE",
        "SIPACUL_API_IMAGE",
        "SIPACUL_FRONTEND_IMAGE",
        "SIPACUL_EDGE_IMAGE"
    )

    $saved = @{}
    foreach ($name in $names) {
        $saved[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
        [Environment]::SetEnvironmentVariable($name, [string]$ImageMap[$name], "Process")
    }

    try {
        Invoke-SiPaculCompose `
            -EnvironmentFile $EnvironmentFile `
            -ComposeFile $ComposeFile `
            -ComposeProject $ComposeProject `
            -Arguments @("config", "--quiet") | Out-Null
    }
    finally {
        foreach ($name in $names) {
            [Environment]::SetEnvironmentVariable($name, $saved[$name], "Process")
        }
    }
}

function Get-SiPaculProjectContainerIds {
    param([Parameter(Mandatory=$true)][string]$ComposeProject)

    $result = Invoke-SiPaculDocker `
        -Arguments @(
            "ps", "--all",
            "--filter", "label=com.docker.compose.project=$ComposeProject",
            "--format", "{{.ID}}"
        ) `
        -Capture

    return @(
        $result.Output |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Get-SiPaculComposeServiceContainerId {
    param(
        [Parameter(Mandatory=$true)][string]$EnvironmentFile,
        [Parameter(Mandatory=$true)][string]$ReleaseEnvironmentFile,
        [Parameter(Mandatory=$true)][string]$ComposeFile,
        [Parameter(Mandatory=$true)][string]$ComposeProject,
        [Parameter(Mandatory=$true)][string]$Service
    )

    $result = Invoke-SiPaculCompose `
        -EnvironmentFile $EnvironmentFile `
        -ReleaseEnvironmentFile $ReleaseEnvironmentFile `
        -ComposeFile $ComposeFile `
        -ComposeProject $ComposeProject `
        -Arguments @("ps", "--all", "--quiet", $Service) `
        -Capture

    $ids = @(
        $result.Output |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )

    if ($ids.Count -gt 1) {
        Fail-SiPaculDeployment "Service $Service memiliki lebih dari satu container."
    }

    if ($ids.Count -eq 0) {
        return ""
    }

    return [string]$ids[0]
}

function Wait-SiPaculComposeServiceHealthy {
    param(
        [Parameter(Mandatory=$true)][string]$EnvironmentFile,
        [Parameter(Mandatory=$true)][string]$ReleaseEnvironmentFile,
        [Parameter(Mandatory=$true)][string]$ComposeFile,
        [Parameter(Mandatory=$true)][string]$ComposeProject,
        [Parameter(Mandatory=$true)][string]$Service,
        [int]$TimeoutSeconds = 180
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)

    do {
        $containerId = Get-SiPaculComposeServiceContainerId `
            -EnvironmentFile $EnvironmentFile `
            -ReleaseEnvironmentFile $ReleaseEnvironmentFile `
            -ComposeFile $ComposeFile `
            -ComposeProject $ComposeProject `
            -Service $Service

        if (-not [string]::IsNullOrWhiteSpace($containerId)) {
            $inspect = Invoke-SiPaculDocker `
                -Arguments @(
                    "inspect",
                    "--format",
                    "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}",
                    $containerId
                ) `
                -Capture

            $state = if ($inspect.Output.Count -eq 1) {
                ([string]$inspect.Output[0]).Trim()
            }
            else {
                ""
            }

            if ($state -eq "healthy" -or $state -eq "running") {
                return
            }

            if ($state -eq "unhealthy" -or $state -eq "dead" -or $state -eq "exited") {
                Fail-SiPaculDeployment "Service $Service masuk state $state sebelum sehat."
            }
        }

        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)

    Fail-SiPaculDeployment "Service $Service tidak sehat dalam $TimeoutSeconds detik."
}

function Assert-SiPaculImageRevision {
    param(
        [Parameter(Mandatory=$true)][string]$Image,
        [Parameter(Mandatory=$true)][string]$ExpectedReleaseSha
    )

    Invoke-SiPaculDocker -Arguments @("pull", $Image) | Out-Null

    $inspect = Invoke-SiPaculDocker `
        -Arguments @(
            "image", "inspect",
            "--format",
            '{{ index .Config.Labels "org.opencontainers.image.revision" }}',
            $Image
        ) `
        -Capture

    if ($inspect.Output.Count -ne 1) {
        Fail-SiPaculDeployment "Revision label image tidak tunggal: $Image"
    }

    $actual = ([string]$inspect.Output[0]).Trim().ToLowerInvariant()
    $expected = (Normalize-SiPaculReleaseSha -ReleaseSha $ExpectedReleaseSha)

    if ($actual -ne $expected) {
        Fail-SiPaculDeployment "Image revision tidak cocok untuk $Image. Aktual=$actual; expected=$expected"
    }
}

function Get-SiPaculDeploymentState {
    param([Parameter(Mandatory=$true)][string]$StateFile)

    if (-not (Test-Path -LiteralPath $StateFile -PathType Leaf)) {
        return $null
    }

    try {
        $state = [IO.File]::ReadAllText($StateFile) | ConvertFrom-Json
    }
    catch {
        Fail-SiPaculDeployment "Deployment state JSON tidak valid: $($_.Exception.Message)"
    }

    foreach ($name in @(
        "schemaVersion",
        "application",
        "databaseReleaseSha",
        "runtimeReleaseSha",
        "registryOwner",
        "composeProject"
    )) {
        if (@($state.PSObject.Properties.Name) -notcontains $name) {
            Fail-SiPaculDeployment "Deployment state kehilangan properti: $name"
        }
    }

    if ([int]$state.schemaVersion -ne 1 -or [string]$state.application -ne "SiPacul") {
        Fail-SiPaculDeployment "Deployment state tidak didukung."
    }

    [void](Normalize-SiPaculReleaseSha -ReleaseSha ([string]$state.databaseReleaseSha))
    [void](Normalize-SiPaculReleaseSha -ReleaseSha ([string]$state.runtimeReleaseSha))

    if (-not [string]::IsNullOrWhiteSpace([string]$state.previousRuntimeReleaseSha)) {
        [void](Normalize-SiPaculReleaseSha -ReleaseSha ([string]$state.previousRuntimeReleaseSha))
    }

    return $state
}

function Write-SiPaculJsonFile {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)]$Value
    )

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $json = $Value | ConvertTo-Json -Depth 8
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, ($json.TrimEnd() + "`n"), $utf8NoBom)
}

function Write-SiPaculDeploymentHistory {
    param(
        [Parameter(Mandatory=$true)][string]$StateDirectory,
        [Parameter(Mandatory=$true)]$State,
        [Parameter(Mandatory=$true)][string]$Operation
    )

    $history = Join-Path $StateDirectory "history"
    if (-not (Test-Path -LiteralPath $history -PathType Container)) {
        New-Item -ItemType Directory -Path $history -Force | Out-Null
    }

    $stamp = [DateTime]::UtcNow.ToString("yyyyMMddTHHmmssfffZ")
    $runtimeShort = ([string]$State.runtimeReleaseSha).Substring(0, 12)
    $path = Join-Path $history "$stamp-$Operation-$runtimeShort.json"
    Write-SiPaculJsonFile -Path $path -Value $State
    return $path
}

function Invoke-SiPaculPreDeployBackup {
    param(
        [Parameter(Mandatory=$true)][string]$RepositoryRoot,
        [Parameter(Mandatory=$true)][string]$EnvironmentFile,
        [Parameter(Mandatory=$true)][string]$ComposeProject,
        [Parameter(Mandatory=$true)][string]$BackupOutputDirectory
    )

    $backupScript = Join-Path $RepositoryRoot "operations\Backup-SiPaculPostgres-PS51.ps1"
    if (-not (Test-Path -LiteralPath $backupScript -PathType Leaf)) {
        Fail-SiPaculDeployment "Backup script existing tidak ditemukan: $backupScript"
    }

    if (-not (Test-Path -LiteralPath $BackupOutputDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $BackupOutputDirectory -Force | Out-Null
    }

    $before = @{}
    foreach ($file in @(Get-ChildItem -LiteralPath $BackupOutputDirectory -Filter "*.dump" -File -ErrorAction SilentlyContinue)) {
        $before[$file.FullName] = $true
    }

    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $backupScript,
        "-EnvironmentFile", $EnvironmentFile,
        "-ComposeProject", $ComposeProject,
        "-OutputDirectory", $BackupOutputDirectory
    )

    $result = Invoke-SiPaculNative `
        -Command "powershell.exe" `
        -Arguments $arguments `
        -Capture

    foreach ($line in $result.Output) {
        Write-Host $line
    }

    $after = @(
        Get-ChildItem -LiteralPath $BackupOutputDirectory -Filter "*.dump" -File |
        Where-Object { -not $before.ContainsKey($_.FullName) } |
        Sort-Object LastWriteTimeUtc
    )

    if ($after.Count -ne 1) {
        Fail-SiPaculDeployment "Backup pre-deploy harus menghasilkan tepat satu archive baru; aktual $($after.Count)."
    }

    $backup = [string]$after[0].FullName
    foreach ($sidecar in @($backup + ".sha256", $backup + ".json")) {
        if (-not (Test-Path -LiteralPath $sidecar -PathType Leaf)) {
            Fail-SiPaculDeployment "Backup sidecar tidak ditemukan: $sidecar"
        }
    }

    return $backup
}

Export-ModuleMember -Function @(
    "Fail-SiPaculDeployment",
    "Resolve-SiPaculRepositoryRoot",
    "Resolve-SiPaculFilePath",
    "Resolve-SiPaculDirectoryPath",
    "Assert-SiPaculOutsideRepository",
    "Invoke-SiPaculNative",
    "Invoke-SiPaculDocker",
    "Invoke-SiPaculCompose",
    "Assert-SiPaculGitClean",
    "Read-SiPaculEnvironmentFile",
    "Assert-SiPaculProductionEnvironment",
    "Normalize-SiPaculReleaseSha",
    "Get-SiPaculReleaseImageMap",
    "Get-SiPaculSplitReleaseImageMap",
    "Write-SiPaculReleaseEnvironment",
    "Assert-SiPaculReleaseEnvironmentMatchesState",
    "Invoke-SiPaculComposeConfigWithImageMap",
    "Get-SiPaculProjectContainerIds",
    "Get-SiPaculComposeServiceContainerId",
    "Wait-SiPaculComposeServiceHealthy",
    "Assert-SiPaculImageRevision",
    "Get-SiPaculDeploymentState",
    "Write-SiPaculJsonFile",
    "Write-SiPaculDeploymentHistory",
    "Invoke-SiPaculPreDeployBackup"
)
