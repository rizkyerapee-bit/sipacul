[CmdletBinding()]
param(
    [ValidateRange(120, 900)]
    [int]$StartupTimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20D2F Container Release Gate Rev10 - Linux curl and TLS cleanup portability"
$repoRoot = $null
$gitCommand = $null
$dockerCommand = $null
$curlCommand = $null
$composeFile = $null
$runtimeRoot = $null
$envFile = $null
$composeProject = $null
$dockerTlsRoot = $null
$edgeImage = $null
$imageTags = @()
$gitHeadBefore = $null
$gitStatusBefore = @()
$productionBefore = @()
$cleanupFailure = $null
$cleanupAttempted = $false

function Fail([string]$Message) {
    throw $Message
}

function Resolve-ExternalCommand([string[]]$Candidates) {
    foreach ($candidate in $Candidates) {
        if (Get-Command $candidate -ErrorAction SilentlyContinue) {
            return $candidate
        }
    }
    Fail ("Command tidak ditemukan: " + ($Candidates -join " atau "))
}

function Invoke-External(
    [string]$Stage,
    [string]$Command,
    [string[]]$Arguments
) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & $Command @Arguments
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0) {
        Fail "$Stage gagal dengan exit code $exitCode."
    }
}

function Get-ExternalOutput(
    [string]$Stage,
    [string]$Command,
    [string[]]$Arguments
) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& $Command @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0) {
        Fail ("$Stage gagal: " + (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Run-Git([string[]]$Arguments) {
    return @(Get-ExternalOutput "git" $script:gitCommand $Arguments)
}

function Invoke-Docker([string]$Stage, [string[]]$Arguments) {
    Invoke-External $Stage $script:dockerCommand $Arguments
}

function Get-DockerOutput([string]$Stage, [string[]]$Arguments) {
    return @(Get-ExternalOutput $Stage $script:dockerCommand $Arguments)
}

function Get-ComposeArguments([string[]]$Arguments) {
    return @(
        "compose",
        "--project-name", $script:composeProject,
        "--env-file", $script:envFile,
        "--file", $script:composeFile
    ) + $Arguments
}

function Invoke-Compose([string]$Stage, [string[]]$Arguments) {
    Invoke-Docker $Stage (Get-ComposeArguments $Arguments)
}

function Get-ComposeOutput([string]$Stage, [string[]]$Arguments) {
    return @(Get-DockerOutput $Stage (Get-ComposeArguments $Arguments))
}

function Get-ComposeStdinOutput(
    [string]$Stage,
    [string]$InputText,
    [string[]]$Arguments
) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $composeArguments = Get-ComposeArguments $Arguments
        $output = @($InputText | & $script:dockerCommand @composeArguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0) {
        Fail ("$Stage gagal: " + (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Get-FreeLoopbackPort {
    $listener = New-Object `
        System.Net.Sockets.TcpListener `
        -ArgumentList ([Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Get-TestHttpsStatusCode([string]$Uri) {
    $discardPath = if ($env:OS -eq "Windows_NT") { "NUL" } else { "/dev/null" }
    $output = @(Get-ExternalOutput `
        "HTTPS loopback probe" `
        $script:curlCommand `
        @(
            "--silent",
            "--show-error",
            "--fail",
            "--insecure",
            "--noproxy", "127.0.0.1,localhost",
            "--connect-timeout", "10",
            "--max-time", "20",
            "--output", $discardPath,
            "--write-out", "%{http_code}",
            $Uri
        ))
    $statusText = (($output -join "`n").Trim())
    if ($statusText -notmatch '^\d{3}$') {
        Fail "Status HTTPS loopback tidak valid: $statusText"
    }
    return [int]$statusText
}

function Convert-Ipv4ToNumber([string]$Address) {
    $parsed = $null
    if (-not [Net.IPAddress]::TryParse($Address, [ref]$parsed) -or
        $parsed.AddressFamily -ne
            [Net.Sockets.AddressFamily]::InterNetwork) {
        Fail "Alamat IPv4 tidak valid: $Address"
    }

    $bytes = $parsed.GetAddressBytes()
    return [uint64]$bytes[0] * 16777216 +
        [uint64]$bytes[1] * 65536 +
        [uint64]$bytes[2] * 256 +
        [uint64]$bytes[3]
}

function Get-Ipv4CidrRange([string]$Cidr) {
    if ($Cidr -notmatch '^([^/]+)/(\d{1,2})$') {
        return $null
    }

    $prefix = [int]$Matches[2]
    if ($prefix -lt 0 -or $prefix -gt 32) {
        return $null
    }

    try {
        $address = Convert-Ipv4ToNumber $Matches[1]
    }
    catch {
        return $null
    }

    $size = [uint64][Math]::Pow(2, 32 - $prefix)
    $start = [uint64]([Math]::Floor($address / $size) * $size)
    return [pscustomobject]@{
        Start = $start
        End = $start + $size - 1
    }
}

function Get-FreeApplicationNetwork {
    $networkIds = @(Get-DockerOutput "Inventaris Docker network" @(
        "network", "ls", "--quiet"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $existingRanges = New-Object System.Collections.Generic.List[object]

    foreach ($networkId in $networkIds) {
        $subnets = @(Get-DockerOutput "Membaca subnet Docker" @(
            "network", "inspect", "--format",
            '{{range .IPAM.Config}}{{println .Subnet}}{{end}}',
            $networkId.Trim()
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

        foreach ($subnet in $subnets) {
            $range = Get-Ipv4CidrRange $subnet.Trim()
            if ($null -ne $range) {
                $existingRanges.Add($range)
            }
        }
    }

    for ($attempt = 0; $attempt -lt 128; $attempt++) {
        $second = Get-Random -Minimum 160 -Maximum 224
        $third = Get-Random -Minimum 1 -Maximum 255
        $subnet = "10.$second.$third.0/24"
        $candidate = Get-Ipv4CidrRange $subnet
        $overlaps = $false

        foreach ($existing in $existingRanges) {
            if ($candidate.Start -le $existing.End -and
                $existing.Start -le $candidate.End) {
                $overlaps = $true
                break
            }
        }

        if (-not $overlaps) {
            return [pscustomobject]@{
                Subnet = $subnet
                EdgeIp = "10.$second.$third.10"
            }
        }
    }

    Fail "Tidak menemukan subnet private /24 yang bebas untuk container gate."
}

function Get-ProductionContainerFingerprint {
    $lines = @(Get-DockerOutput "Inventaris container produksi" @(
        "ps", "--all", "--no-trunc",
        "--format", "{{.ID}}|{{.Names}}|{{.Image}}|{{.State}}"
    ))
    return @($lines |
        Where-Object { ($_ -split '\|')[1] -like "sipacul-production-*" } |
        Sort-Object)
}

function Get-ServiceContainerId([string]$Service) {
    $ids = @(Get-ComposeOutput "Membaca container $Service" @(
        "ps", "--all", "--quiet", $Service
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($ids.Count -eq 0) {
        return $null
    }
    if ($ids.Count -ne 1) {
        Fail "Service $Service memiliki lebih dari satu container."
    }
    return $ids[0].Trim()
}

function Get-ContainerState([string]$ContainerId) {
    $format = '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}|{{.State.ExitCode}}'
    $line = (Get-DockerOutput "Membaca state container" @(
        "inspect", "--format", $format, $ContainerId
    ) | Select-Object -First 1)
    $parts = $line -split '\|'
    if ($parts.Count -ne 3) {
        Fail "Format state container tidak dikenal: $line"
    }
    return [pscustomobject]@{
        Status = $parts[0]
        Health = $parts[1]
        ExitCode = [int]$parts[2]
    }
}

function Get-ContainerNetworks([string]$ContainerId) {
    $format = '{{json .NetworkSettings.Networks}}'
    $json = (Get-DockerOutput "Membaca network container" @(
        "inspect", "--format", $format, $ContainerId
    ) | Select-Object -First 1).Trim()
    try {
        $networks = $json | ConvertFrom-Json
    }
    catch {
        Fail "JSON network container tidak valid: $($_.Exception.Message)"
    }
    if ($null -eq $networks) {
        return @()
    }
    return @($networks.PSObject.Properties |
        ForEach-Object { $_.Name } |
        Sort-Object)
}

function Assert-Networks(
    [string]$Service,
    [string]$ContainerId,
    [string[]]$ExpectedNetworks
) {
    $actual = @(Get-ContainerNetworks $ContainerId)
    $expected = @($ExpectedNetworks | Sort-Object)
    if (($actual -join "`n") -cne ($expected -join "`n")) {
        Fail ("Network $Service tidak cocok. Aktual: " + ($actual -join ", "))
    }
}

function Assert-NoPublishedPort([string]$Service, [string]$ContainerId) {
    $ports = @(Get-DockerOutput "Membaca port $Service" @(
        "port", $ContainerId
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($ports.Count -ne 0) {
        Fail "$Service tidak boleh memublikasikan port host."
    }
}

function Assert-HeaderValue(
    [object]$Headers,
    [string]$Name,
    [string]$ExpectedValue,
    [string]$Route
) {
    $property = $Headers.PSObject.Properties[$Name.ToLowerInvariant()]
    $actualValue = if ($null -eq $property) {
        ""
    }
    else {
        [string]$property.Value
    }
    if ($actualValue -cne $ExpectedValue) {
        Fail "Header $Name pada $Route harus '$ExpectedValue'; aktual '$actualValue'."
    }
}

function Test-ImageExists([string]$ImageName) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & $script:dockerCommand image inspect $ImageName *> $null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Write-FailureDiagnostics {
    Write-Host ""
    Write-Host "=== DIAGNOSTIK STACK SEMENTARA ===" -ForegroundColor Yellow
    try {
        foreach ($line in @(Get-ComposeOutput "Compose ps" @("ps", "--all"))) {
            Write-Host $line
        }
    }
    catch {
        Write-Host "[INFO] Compose ps tidak tersedia: $($_.Exception.Message)"
    }
    try {
        foreach ($line in @(Get-ComposeOutput "Compose logs" @(
            "logs", "--no-color", "--tail", "100"
        ))) {
            Write-Host $line
        }
    }
    catch {
        Write-Host "[INFO] Compose logs tidak tersedia: $($_.Exception.Message)"
    }
}

function Remove-TemporaryResources {
    $errors = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($script:composeProject) -and
        -not [string]::IsNullOrWhiteSpace($script:envFile) -and
        -not [string]::IsNullOrWhiteSpace($script:composeFile) -and
        (Test-Path -LiteralPath $script:envFile -PathType Leaf)) {
        try {
            Invoke-Compose "Cleanup Compose" @(
                "down", "--volumes", "--remove-orphans", "--timeout", "20"
            ) | Out-Host
        }
        catch {
            $errors.Add($_.Exception.Message)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($script:dockerTlsRoot) -and
        -not [string]::IsNullOrWhiteSpace($script:edgeImage) -and
        (Test-Path -LiteralPath $script:dockerTlsRoot -PathType Container)) {
        try {
            if (Test-ImageExists $script:edgeImage) {
                $tlsMount = (
                    "type=bind,source=$($script:dockerTlsRoot),target=/tls"
                )
                Invoke-Docker "Menghapus artefak TLS sementara" @(
                    "run", "--rm", "--user", "0:0",
                    "--entrypoint", "/bin/sh",
                    "--mount", $tlsMount,
                    $script:edgeImage,
                    "-c", "rm -f /tls/tls.crt /tls/tls.key"
                ) | Out-Host
            }
        }
        catch {
            $errors.Add($_.Exception.Message)
        }
    }

    foreach ($imageTag in @($script:imageTags)) {
        try {
            if (Test-ImageExists $imageTag) {
                Invoke-Docker "Menghapus image sementara $imageTag" @(
                    "image", "rm", "--force", $imageTag
                ) | Out-Host
            }
        }
        catch {
            $errors.Add($_.Exception.Message)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($script:runtimeRoot) -and
        (Test-Path -LiteralPath $script:runtimeRoot -PathType Container)) {
        try {
            $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd(
                [IO.Path]::DirectorySeparatorChar,
                [IO.Path]::AltDirectorySeparatorChar)
            $runtimeFull = [IO.Path]::GetFullPath($script:runtimeRoot)
            $runtimeParent = [IO.Path]::GetDirectoryName($runtimeFull).TrimEnd(
                [IO.Path]::DirectorySeparatorChar,
                [IO.Path]::AltDirectorySeparatorChar)
            $runtimeLeaf = [IO.Path]::GetFileName($runtimeFull)
            if ($runtimeParent -cne $tempRoot -or
                -not $runtimeLeaf.StartsWith("SiPacul-20D2F-", [StringComparison]::Ordinal)) {
                $errors.Add("Folder runtime tidak memenuhi kontrak cleanup aman: $runtimeFull")
            }
            else {
                Remove-Item -LiteralPath $runtimeFull -Recurse -Force
            }
        }
        catch {
            $errors.Add($_.Exception.Message)
        }
    }

    if ($errors.Count -gt 0) {
        return ($errors -join " | ")
    }
    return $null
}

try {
    Write-Host "=== PREFLIGHT SPRINT 20D2F CONTAINER RELEASE GATE ==="

    $gitCommand = Resolve-ExternalCommand @("git.exe", "git")
    $dockerCommand = Resolve-ExternalCommand @("docker.exe", "docker")
    $curlCommand = Resolve-ExternalCommand @("curl.exe", "curl")

    $repoRoot = (Run-Git @("rev-parse", "--show-toplevel") | Select-Object -First 1).Trim()
    $repoRoot = [IO.Path]::GetFullPath($repoRoot)
    Set-Location -LiteralPath $repoRoot

    $gitHeadBefore = (Run-Git @("rev-parse", "HEAD") | Select-Object -First 1).Trim()
    $gitStatusBefore = @(Run-Git @(
        "status", "--porcelain=v1", "--untracked-files=all"
    ))
    $productionBefore = @(Get-ProductionContainerFingerprint)

    $composeFile = Join-Path $repoRoot "compose.production.yml"
    $backendDockerfile = Join-Path (Join-Path $repoRoot "backend") "Dockerfile"
    $frontendDockerfile = Join-Path (Join-Path $repoRoot "frontend") "Dockerfile"
    $edgeDockerfile = Join-Path (Join-Path $repoRoot "edge") "Dockerfile"
    $edgeConfiguration = Join-Path (Join-Path $repoRoot "edge") "default.conf"
    $infrastructureRoot = Join-Path `
        (Join-Path (Join-Path $repoRoot "backend") "src") `
        "SiPacul.Infrastructure"
    $migrationRoot = Join-Path (Join-Path $infrastructureRoot "Data") "Migrations"
    foreach ($path in @(
        $composeFile,
        $backendDockerfile,
        $frontendDockerfile,
        $edgeDockerfile,
        $edgeConfiguration,
        $migrationRoot
    )) {
        if (-not (Test-Path -LiteralPath $path)) {
            Fail "Input container gate tidak ditemukan: $path"
        }
    }

    $migrationFiles = @(Get-ChildItem -LiteralPath $migrationRoot -File |
        Where-Object {
            $_.Name -match '^\d+_.+\.cs$' -and
            $_.Name -notlike '*.Designer.cs'
        } |
        Sort-Object -Property BaseName)
    if ($migrationFiles.Count -eq 0) {
        Fail "Migration EF Core tidak ditemukan."
    }
    $expectedMigration = $migrationFiles[-1].BaseName

    $dockerVersion = (Get-DockerOutput "Docker version" @(
        "version", "--format", "{{.Server.Version}}"
    ) | Select-Object -First 1).Trim()
    $composeVersion = (Get-DockerOutput "Docker Compose version" @(
        "compose", "version", "--short"
    ) | Select-Object -First 1).Trim()

    $suffix = [Guid]::NewGuid().ToString("N").Substring(0, 12)
    $shortHead = $gitHeadBefore.Substring(0, 7).ToLowerInvariant()
    $composeProject = "sipacul20d2f$suffix"
    $runtimeRoot = Join-Path ([IO.Path]::GetTempPath()) "SiPacul-20D2F-$suffix"
    [IO.Directory]::CreateDirectory($runtimeRoot) | Out-Null
    $envFile = Join-Path $runtimeRoot "release-gate.env"
    $tlsRoot = Join-Path $runtimeRoot "tls"
    [IO.Directory]::CreateDirectory($tlsRoot) | Out-Null
    $tlsCertificatePath = Join-Path $tlsRoot "tls.crt"
    $tlsPrivateKeyPath = Join-Path $tlsRoot "tls.key"
    $dockerTlsRoot = [IO.Path]::GetFullPath($tlsRoot).Replace("\", "/")
    $dockerTlsCertificatePath = [IO.Path]::GetFullPath(
        $tlsCertificatePath).Replace("\", "/")
    $dockerTlsPrivateKeyPath = [IO.Path]::GetFullPath(
        $tlsPrivateKeyPath).Replace("\", "/")
    $httpsPort = Get-FreeLoopbackPort
    $applicationNetworkConfiguration = Get-FreeApplicationNetwork
    $applicationSubnet = $applicationNetworkConfiguration.Subnet
    $edgeIp = $applicationNetworkConfiguration.EdgeIp

    $migratorImage = "sipacul-migrator:20d2f-$shortHead-$suffix"
    $apiImage = "sipacul-api:20d2f-$shortHead-$suffix"
    $frontendImage = "sipacul-frontend:20d2f-$shortHead-$suffix"
    $edgeImage = "sipacul-edge:20d2f-$shortHead-$suffix"
    $imageTags = @($migratorImage, $apiImage, $frontendImage, $edgeImage)

    $databasePassword = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
    $bootstrapToken = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
    $envLines = @(
        "POSTGRES_DB=sipacul_rc",
        "POSTGRES_USER=sipacul_rc",
        "POSTGRES_PASSWORD=$databasePassword",
        "SIPACUL_BOOTSTRAP_OWNER_TOKEN=$bootstrapToken",
        "SIPACUL_BIND_ADDRESS=127.0.0.1",
        "SIPACUL_HTTPS_PORT=$httpsPort",
        "SIPACUL_APPLICATION_SUBNET=$applicationSubnet",
        "SIPACUL_EDGE_IP=$edgeIp",
        "SIPACUL_TLS_CERTIFICATE_PATH=$dockerTlsCertificatePath",
        "SIPACUL_TLS_PRIVATE_KEY_PATH=$dockerTlsPrivateKeyPath",
        "SIPACUL_MIGRATOR_IMAGE=$migratorImage",
        "SIPACUL_API_IMAGE=$apiImage",
        "SIPACUL_FRONTEND_IMAGE=$frontendImage",
        "SIPACUL_EDGE_IMAGE=$edgeImage"
    )
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($envFile, (($envLines -join "`n") + "`n"), $utf8NoBom)

    Write-Host "[OK] Repository/HEAD: $repoRoot / $shortHead"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Docker $dockerVersion; Compose $composeVersion; migration terakhir $expectedMigration."
    Write-Host "[OK] Stack sementara: $composeProject pada HTTPS loopback port $httpsPort."
    Write-Host "[OK] Network application sementara: $applicationSubnet; edge eksak: $edgeIp."
    Write-Host "[OK] Rahasia dan path TLS runtime dibuat di folder sementara dan tidak dicetak."

    $mainFailure = $null
    try {
        Write-Host ""
        Write-Host "=== VALIDASI DAN BUILD IMAGE ==="
        Invoke-Compose "Validasi Compose" @("config", "--quiet")
        $configuredImages = @(Get-ComposeOutput "Inventaris image Compose" @(
            "config", "--images"
        ) | Sort-Object -Unique)
        $expectedImages = @(
            "postgres:17-alpine",
            $migratorImage,
            $apiImage,
            $frontendImage,
            $edgeImage
        ) | Sort-Object
        if (($configuredImages -join "`n") -cne ($expectedImages -join "`n")) {
            Fail ("Image efektif Compose tidak cocok:`n" + ($configuredImages -join "`n"))
        }
        Invoke-Compose "Build image produksi" @("build")
        foreach ($imageTag in $imageTags) {
            if (-not (Test-ImageExists $imageTag)) {
                Fail "Image hasil build tidak ditemukan: $imageTag"
            }
        }
        $tlsMount = "type=bind,source=$dockerTlsRoot,target=/tls"
        Invoke-Docker "Membuat sertifikat TLS sementara" @(
            "run", "--rm", "--user", "0:0",
            "--entrypoint", "/bin/sh",
            "--mount", $tlsMount,
            $edgeImage,
            "-c",
            "umask 022; openssl req -x509 -newkey rsa:2048 -sha256 -nodes -days 1 -subj /CN=localhost -addext subjectAltName=DNS:localhost,IP:127.0.0.1 -keyout /tls/tls.key -out /tls/tls.crt >/dev/null 2>&1; chmod 0444 /tls/tls.key /tls/tls.crt"
        )
        foreach ($tlsPath in @($tlsCertificatePath, $tlsPrivateKeyPath)) {
            if (-not (Test-Path -LiteralPath $tlsPath -PathType Leaf)) {
                Fail "Artefak TLS sementara tidak terbentuk: $tlsPath"
            }
        }
        Write-Host "[OK] Empat image produksi dibangun; sertifikat TLS sementara siap."

        Write-Host ""
        Write-Host "=== STARTUP STACK SEMENTARA ==="
        Invoke-Compose "Startup stack sementara" @("up", "--detach", "--no-build")

        $deadline = [DateTimeOffset]::UtcNow.AddSeconds($StartupTimeoutSeconds)
        $serviceIds = @{}
        $ready = $false
        while ([DateTimeOffset]::UtcNow -lt $deadline) {
            foreach ($service in @("postgres", "migrator", "api", "frontend", "edge")) {
                if (-not $serviceIds.ContainsKey($service)) {
                    $id = Get-ServiceContainerId $service
                    if (-not [string]::IsNullOrWhiteSpace($id)) {
                        $serviceIds[$service] = $id
                    }
                }
            }

            if ($serviceIds.Count -eq 5) {
                $postgresState = Get-ContainerState $serviceIds["postgres"]
                $migratorState = Get-ContainerState $serviceIds["migrator"]
                $apiState = Get-ContainerState $serviceIds["api"]
                $frontendState = Get-ContainerState $serviceIds["frontend"]
                $edgeState = Get-ContainerState $serviceIds["edge"]

                if ($migratorState.Status -eq "exited" -and $migratorState.ExitCode -ne 0) {
                    Fail "Migrator berhenti dengan exit code $($migratorState.ExitCode)."
                }
                foreach ($runtimeService in @(
                    [pscustomobject]@{ Name = "api"; State = $apiState },
                    [pscustomobject]@{ Name = "frontend"; State = $frontendState },
                    [pscustomobject]@{ Name = "edge"; State = $edgeState }
                )) {
                    if ($runtimeService.State.Status -eq "exited") {
                        Fail "$($runtimeService.Name) berhenti dengan exit code $($runtimeService.State.ExitCode)."
                    }
                }

                $ready = (
                    $postgresState.Status -eq "running" -and
                    $postgresState.Health -eq "healthy" -and
                    $migratorState.Status -eq "exited" -and
                    $migratorState.ExitCode -eq 0 -and
                    $apiState.Status -eq "running" -and
                    $apiState.Health -eq "healthy" -and
                    $frontendState.Status -eq "running" -and
                    $frontendState.Health -eq "healthy" -and
                    $edgeState.Status -eq "running" -and
                    $edgeState.Health -eq "healthy")
                if ($ready) {
                    break
                }
            }
            Start-Sleep -Seconds 2
        }
        if (-not $ready) {
            Fail "Stack belum sehat dalam $StartupTimeoutSeconds detik."
        }
        Write-Host "[OK] PostgreSQL sehat, migrator sukses, API, frontend, dan edge TLS sehat."

        Write-Host ""
        Write-Host "=== VERIFIKASI RUNTIME ==="
        $hostLoginStatus = Get-TestHttpsStatusCode `
            "https://127.0.0.1:$httpsPort/login"
        if ($hostLoginStatus -ne 200) {
            Fail ("HTTPS loopback /login harus 200; aktual " +
                $hostLoginStatus + ".")
        }
        $runtimeProbeScript = @'
const baseUrl = "https://edge:8443";
const securityHeaderNames = [
  "content-security-policy",
  "referrer-policy",
  "x-content-type-options",
  "x-frame-options",
];

function selectedHeaders(response) {
  return Object.fromEntries(
    securityHeaderNames.map((name) => [name, response.headers.get(name) ?? ""]),
  );
}

async function consume(response) {
  await response.arrayBuffer();
  return response;
}

async function malformedLogin(forwardedFor) {
  const response = await fetch(`${baseUrl}/api/v1/auth/login`, {
    method: "POST",
    redirect: "manual",
    headers: {
      "content-type": "application/json",
      "x-forwarded-for": forwardedFor,
      "x-forwarded-proto": "http",
    },
    body: "{",
  });
  await response.arrayBuffer();
  return response.status;
}

(async () => {
  const loginPage = await consume(await fetch(`${baseUrl}/login`));
  const bootstrap = await consume(
    await fetch(`${baseUrl}/api/v1/bootstrap/status`),
  );
  const csrfResponse = await fetch(`${baseUrl}/api/v1/auth/csrf`, {
    headers: {
      "x-forwarded-for": "198.51.100.200",
      "x-forwarded-proto": "http",
    },
  });
  const csrfBody = await csrfResponse.json();
  const setCookies = typeof csrfResponse.headers.getSetCookie === "function"
    ? csrfResponse.headers.getSetCookie()
    : [csrfResponse.headers.get("set-cookie")].filter(Boolean);
  if (setCookies.length === 0) {
    throw new Error("Antiforgery cookie tidak ditemukan.");
  }
  const cookieHeader = setCookies
    .map((value) => value.split(";", 1)[0])
    .join("; ");
  const loginResponse = await fetch(`${baseUrl}/api/v1/auth/login`, {
    method: "POST",
    redirect: "manual",
    headers: {
      "content-type": "application/json",
      "cookie": cookieHeader,
      "x-forwarded-for": "198.51.100.200",
      "x-forwarded-proto": "http",
      [csrfBody.headerName]: csrfBody.requestToken,
    },
    body: JSON.stringify({
      email: "missing@example.invalid",
      password: "InvalidPassword!123",
      rememberMe: false,
    }),
  });
  await loginResponse.arrayBuffer();

  const spoofStatuses = [];
  for (let index = 0; index < 9; index += 1) {
    spoofStatuses.push(await malformedLogin("198.51.100.25"));
  }
  spoofStatuses.push(await malformedLogin("203.0.113.25"));

  process.stdout.write(JSON.stringify({
    loginPageStatus: loginPage.status,
    loginPageHeaders: selectedHeaders(loginPage),
    bootstrapStatus: bootstrap.status,
    bootstrapHeaders: selectedHeaders(bootstrap),
    csrfStatus: csrfResponse.status,
    csrfCookie: setCookies.join("\n"),
    loginStatus: loginResponse.status,
    spoofStatuses,
  }));
})().catch((error) => {
  console.error(error instanceof Error ? error.stack : String(error));
  process.exit(1);
});
'@
        $runtimeProbeOutput = @(
            Get-ComposeStdinOutput `
                "Menguji jalur TLS edge" `
                $runtimeProbeScript `
                @(
                    "exec", "--no-TTY",
                    "--env", "NODE_TLS_REJECT_UNAUTHORIZED=0",
                    "--env", "NODE_NO_WARNINGS=1",
                    "frontend", "node", "-"
                ) |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        $runtimeProbeJson =
            ($runtimeProbeOutput | Select-Object -Last 1).Trim()
        try {
            $runtimeProbe = $runtimeProbeJson | ConvertFrom-Json
        }
        catch {
            Fail "Output probe TLS edge bukan JSON valid: $($_.Exception.Message)"
        }

        foreach ($statusProbe in @(
            [pscustomobject]@{
                Name = "/login"
                Actual = [int]$runtimeProbe.loginPageStatus
                Expected = 200
            },
            [pscustomobject]@{
                Name = "/api/v1/bootstrap/status"
                Actual = [int]$runtimeProbe.bootstrapStatus
                Expected = 200
            },
            [pscustomobject]@{
                Name = "/api/v1/auth/csrf"
                Actual = [int]$runtimeProbe.csrfStatus
                Expected = 200
            },
            [pscustomobject]@{
                Name = "login dengan antiforgery valid"
                Actual = [int]$runtimeProbe.loginStatus
                Expected = 401
            }
        )) {
            if ($statusProbe.Actual -ne $statusProbe.Expected) {
                Fail ("Status " + $statusProbe.Name + " harus " +
                    $statusProbe.Expected + "; aktual " + $statusProbe.Actual + ".")
            }
        }

        $csrfCookie = [string]$runtimeProbe.csrfCookie
        foreach ($cookieMarker in @("Secure", "HttpOnly", "SameSite=Lax")) {
            if ($csrfCookie -notmatch
                ("(?i)(^|;\s*)" + [regex]::Escape($cookieMarker) + "(;|$)")) {
                Fail "Cookie antiforgery harus memuat atribut $cookieMarker."
            }
        }

        $forwardedHeaderStatuses = @(
            $runtimeProbe.spoofStatuses |
                ForEach-Object { [int]$_ }
        )
        if ($forwardedHeaderStatuses.Count -ne 10) {
            Fail "Probe forwarded headers harus menghasilkan tepat 10 status."
        }
        for ($index = 0; $index -lt 9; $index++) {
            if ([int]$forwardedHeaderStatuses[$index] -ne 400) {
                Fail ("Percobaan malformed login " + ($index + 1) +
                    " harus 400; aktual " + $forwardedHeaderStatuses[$index] + ".")
            }
        }
        if ([int]$forwardedHeaderStatuses[9] -ne 429) {
            Fail ("Spoofed IP kedua harus tetap pada partisi edge yang dibatasi; " +
                "status aktual " + $forwardedHeaderStatuses[9] + ".")
        }

        $expectedSecurityHeaders = [ordered]@{
            "Content-Security-Policy" = "base-uri 'self'; frame-ancestors 'none'; object-src 'none'"
            "Referrer-Policy" = "strict-origin-when-cross-origin"
            "X-Content-Type-Options" = "nosniff"
            "X-Frame-Options" = "DENY"
        }
        foreach ($routeProbe in @(
            [pscustomobject]@{
                Route = "/login"
                Headers = $runtimeProbe.loginPageHeaders
            },
            [pscustomobject]@{
                Route = "/api/v1/bootstrap/status"
                Headers = $runtimeProbe.bootstrapHeaders
            }
        )) {
            foreach ($headerName in $expectedSecurityHeaders.Keys) {
                Assert-HeaderValue `
                    -Headers $routeProbe.Headers `
                    -Name $headerName `
                    -ExpectedValue ([string]$expectedSecurityHeaders[$headerName]) `
                    -Route $routeProbe.Route
            }
        }

        $apiEnvironmentJson = (Get-DockerOutput "Membaca environment API" @(
            "inspect", "--format", "{{json .Config.Env}}", $serviceIds["api"]
        ) | Select-Object -First 1).Trim()
        try {
            $parsedApiEnvironment = $apiEnvironmentJson | ConvertFrom-Json
            $apiEnvironment = New-Object System.Collections.Generic.List[string]
            foreach ($entry in $parsedApiEnvironment) {
                $apiEnvironment.Add([string]$entry)
            }
        }
        catch {
            Fail "Environment API bukan JSON valid: $($_.Exception.Message)"
        }
        $automaticForwardingEntries = @($apiEnvironment | Where-Object {
            ([string]$_).StartsWith(
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED=",
                [StringComparison]::OrdinalIgnoreCase)
        })
        if ($automaticForwardingEntries.Count -ne 0) {
            Fail "API tidak boleh memakai ASPNETCORE_FORWARDEDHEADERS_ENABLED."
        }
        $knownProxyEntries = @($apiEnvironment | Where-Object {
            ([string]$_).StartsWith(
                "ForwardedHeaders__KnownProxies__0=",
                [StringComparison]::Ordinal)
        })
        if ($knownProxyEntries.Count -ne 1 -or
            [string]$knownProxyEntries[0] -cne
                "ForwardedHeaders__KnownProxies__0=$edgeIp") {
            Fail "API harus memercayai tepat IP edge sementara $edgeIp."
        }

        $applicationNetwork = "${composeProject}_application"
        $edgeNetworkJson = (Get-DockerOutput "Membaca IP edge" @(
            "inspect", "--format", "{{json .NetworkSettings.Networks}}",
            $serviceIds["edge"]
        ) | Select-Object -First 1).Trim()
        try {
            $edgeNetworks = $edgeNetworkJson | ConvertFrom-Json
            $edgeNetworkProperty =
                $edgeNetworks.PSObject.Properties[$applicationNetwork]
            if ($null -eq $edgeNetworkProperty) {
                Fail "Edge tidak terhubung ke network application."
            }
            $actualEdgeIp = [string]$edgeNetworkProperty.Value.IPAddress
        }
        catch {
            Fail "Network edge bukan JSON valid: $($_.Exception.Message)"
        }
        if ($actualEdgeIp -cne $edgeIp) {
            Fail "IP edge harus $edgeIp; aktual $actualEdgeIp."
        }

        $migrationQuery = 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
        $actualMigration = (Get-ComposeStdinOutput "Membaca migration database" $migrationQuery @(
            "exec", "--no-TTY", "postgres",
            "psql", "--username", "sipacul_rc", "--dbname", "sipacul_rc",
            "--tuples-only", "--no-align", "--file=-"
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1).Trim()
        if ($actualMigration -ne $expectedMigration) {
            Fail "Migration database harus $expectedMigration; aktual $actualMigration."
        }

        Assert-NoPublishedPort "PostgreSQL" $serviceIds["postgres"]
        Assert-NoPublishedPort "Migrator" $serviceIds["migrator"]
        Assert-NoPublishedPort "API" $serviceIds["api"]
        Assert-NoPublishedPort "Frontend" $serviceIds["frontend"]
        $edgePorts = @(Get-DockerOutput "Membaca port edge" @(
            "port", $serviceIds["edge"], "8443/tcp"
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($edgePorts.Count -ne 1 -or
            $edgePorts[0].Trim() -ne "127.0.0.1:$httpsPort") {
            Fail ("Port edge bukan tepat 127.0.0.1:$httpsPort. Aktual: " +
                ($edgePorts -join ", "))
        }

        $applicationNetwork = "${composeProject}_application"
        $databaseNetwork = "${composeProject}_database"
        Assert-Networks "PostgreSQL" $serviceIds["postgres"] @($databaseNetwork)
        Assert-Networks "Migrator" $serviceIds["migrator"] @($databaseNetwork)
        Assert-Networks "API" $serviceIds["api"] @($applicationNetwork, $databaseNetwork)
        Assert-Networks "Frontend" $serviceIds["frontend"] @($applicationNetwork)
        Assert-Networks "Edge" $serviceIds["edge"] @($applicationNetwork)

        Write-Host "[OK] HTTPS edge, antiforgery/login, empat security header, sanitasi spoof, migration $actualMigration, port, dan network tervalidasi."
    }
    catch {
        $mainFailure = $_.Exception.Message
        Write-FailureDiagnostics
    }
    finally {
        Write-Host ""
        Write-Host "=== CLEANUP STACK SEMENTARA ==="
        $cleanupAttempted = $true
        $cleanupFailure = Remove-TemporaryResources
        if ([string]::IsNullOrWhiteSpace($cleanupFailure)) {
            Write-Host "[OK] Container, volume, network, empat image, sertifikat, dan rahasia sementara dibersihkan."
        }
        else {
            Write-Host "[PERINGATAN] Cleanup tidak lengkap: $cleanupFailure" -ForegroundColor Yellow
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($mainFailure)) {
        Fail $mainFailure
    }
    if (-not [string]::IsNullOrWhiteSpace($cleanupFailure)) {
        Fail $cleanupFailure
    }

    $remainingContainers = @(Get-DockerOutput "Memeriksa container sisa" @(
        "ps", "--all", "--quiet", "--filter", "name=$composeProject"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $remainingVolumes = @(Get-DockerOutput "Memeriksa volume sisa" @(
        "volume", "ls", "--quiet", "--filter", "name=$composeProject"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $remainingNetworks = @(Get-DockerOutput "Memeriksa network sisa" @(
        "network", "ls", "--quiet", "--filter", "name=$composeProject"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($remainingContainers.Count -ne 0 -or
        $remainingVolumes.Count -ne 0 -or
        $remainingNetworks.Count -ne 0) {
        Fail "Masih ada resource Docker sementara setelah cleanup."
    }
    foreach ($imageTag in $imageTags) {
        if (Test-ImageExists $imageTag) {
            Fail "Image sementara masih ada setelah cleanup: $imageTag"
        }
    }

    $productionAfter = @(Get-ProductionContainerFingerprint)
    if (($productionAfter -join "`n") -cne ($productionBefore -join "`n")) {
        Fail "Container stack produksi berubah selama release gate."
    }

    $gitHeadAfter = (Run-Git @("rev-parse", "HEAD") | Select-Object -First 1).Trim()
    $gitStatusAfter = @(Run-Git @(
        "status", "--porcelain=v1", "--untracked-files=all"
    ))
    if ($gitHeadAfter -ne $gitHeadBefore) {
        Fail "HEAD berubah selama container release gate."
    }
    if (($gitStatusAfter -join "`n") -cne ($gitStatusBefore -join "`n")) {
        Fail "Status Git berubah selama container release gate."
    }

    Write-Host ""
    Write-Host "=== STATUS AKHIR CONTAINER RELEASE GATE ==="
    Write-Host "[OK] Empat image produksi, migration gate, TLS edge, antiforgery/login, security headers, trust boundary, port, dan network lulus."
    Write-Host "[OK] Resource sementara dibersihkan; stack produksi yang sudah ada tetap identik."
    Write-Host "[OK] HEAD dan status Git tidak berubah; tidak ada database produksi atau pengembangan yang disentuh."
}
catch {
    if (-not $cleanupAttempted -and
        -not [string]::IsNullOrWhiteSpace($runtimeRoot)) {
        $cleanupAttempted = $true
        $cleanupFailure = Remove-TemporaryResources
    }
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    if (-not [string]::IsNullOrWhiteSpace($cleanupFailure)) {
        Write-Host "[PERINGATAN] Periksa resource dengan prefix $composeProject sebelum melanjutkan." -ForegroundColor Yellow
    }
    Write-Host "Kirim seluruh output; jangan melakukan cleanup broad atau perubahan repository secara manual."
    exit 1
}
