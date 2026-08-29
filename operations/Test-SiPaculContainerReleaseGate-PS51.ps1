[CmdletBinding()]
param(
    [ValidateRange(120, 900)]
    [int]$StartupTimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20D2D Container Release Gate Rev4 - public security headers"
$repoRoot = $null
$gitCommand = $null
$dockerCommand = $null
$composeFile = $null
$runtimeRoot = $null
$envFile = $null
$composeProject = $null
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

function Assert-ResponseHeader(
    [object]$Response,
    [string]$Name,
    [string]$ExpectedValue,
    [string]$Route
) {
    $actualValue = [string]$Response.Headers[$Name]
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
                -not $runtimeLeaf.StartsWith("SiPacul-20D2A-", [StringComparison]::Ordinal)) {
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
    Write-Host "=== PREFLIGHT SPRINT 20D2A CONTAINER RELEASE GATE ==="

    $gitCommand = Resolve-ExternalCommand @("git.exe", "git")
    $dockerCommand = Resolve-ExternalCommand @("docker.exe", "docker")

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
    $infrastructureRoot = Join-Path `
        (Join-Path (Join-Path $repoRoot "backend") "src") `
        "SiPacul.Infrastructure"
    $migrationRoot = Join-Path (Join-Path $infrastructureRoot "Data") "Migrations"
    foreach ($path in @($composeFile, $backendDockerfile, $frontendDockerfile, $migrationRoot)) {
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
    $composeProject = "sipacul20d2a$suffix"
    $runtimeRoot = Join-Path ([IO.Path]::GetTempPath()) "SiPacul-20D2A-$suffix"
    [IO.Directory]::CreateDirectory($runtimeRoot) | Out-Null
    $envFile = Join-Path $runtimeRoot "release-gate.env"
    $httpPort = Get-FreeLoopbackPort

    $migratorImage = "sipacul-migrator:20d2a-$shortHead-$suffix"
    $apiImage = "sipacul-api:20d2a-$shortHead-$suffix"
    $frontendImage = "sipacul-frontend:20d2a-$shortHead-$suffix"
    $imageTags = @($migratorImage, $apiImage, $frontendImage)

    $databasePassword = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
    $bootstrapToken = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
    $envLines = @(
        "POSTGRES_DB=sipacul_rc",
        "POSTGRES_USER=sipacul_rc",
        "POSTGRES_PASSWORD=$databasePassword",
        "SIPACUL_BOOTSTRAP_OWNER_TOKEN=$bootstrapToken",
        "SIPACUL_BIND_ADDRESS=127.0.0.1",
        "SIPACUL_HTTP_PORT=$httpPort",
        "SIPACUL_MIGRATOR_IMAGE=$migratorImage",
        "SIPACUL_API_IMAGE=$apiImage",
        "SIPACUL_FRONTEND_IMAGE=$frontendImage"
    )
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($envFile, (($envLines -join "`n") + "`n"), $utf8NoBom)

    Write-Host "[OK] Repository/HEAD: $repoRoot / $shortHead"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Docker $dockerVersion; Compose $composeVersion; migration terakhir $expectedMigration."
    Write-Host "[OK] Stack sementara: $composeProject pada loopback port $httpPort."
    Write-Host "[OK] Rahasia runtime dibuat di folder sementara dan tidak dicetak."

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
            $frontendImage
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
        Write-Host "[OK] Image migrator, API, dan frontend dibangun dengan tag terisolasi."

        Write-Host ""
        Write-Host "=== STARTUP STACK SEMENTARA ==="
        Invoke-Compose "Startup stack sementara" @("up", "--detach", "--no-build")

        $deadline = [DateTimeOffset]::UtcNow.AddSeconds($StartupTimeoutSeconds)
        $serviceIds = @{}
        $ready = $false
        while ([DateTimeOffset]::UtcNow -lt $deadline) {
            foreach ($service in @("postgres", "migrator", "api", "frontend")) {
                if (-not $serviceIds.ContainsKey($service)) {
                    $id = Get-ServiceContainerId $service
                    if (-not [string]::IsNullOrWhiteSpace($id)) {
                        $serviceIds[$service] = $id
                    }
                }
            }

            if ($serviceIds.Count -eq 4) {
                $postgresState = Get-ContainerState $serviceIds["postgres"]
                $migratorState = Get-ContainerState $serviceIds["migrator"]
                $apiState = Get-ContainerState $serviceIds["api"]
                $frontendState = Get-ContainerState $serviceIds["frontend"]

                if ($migratorState.Status -eq "exited" -and $migratorState.ExitCode -ne 0) {
                    Fail "Migrator berhenti dengan exit code $($migratorState.ExitCode)."
                }
                foreach ($runtimeService in @(
                    [pscustomobject]@{ Name = "api"; State = $apiState },
                    [pscustomobject]@{ Name = "frontend"; State = $frontendState }
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
                    $frontendState.Health -eq "healthy")
                if ($ready) {
                    break
                }
            }
            Start-Sleep -Seconds 2
        }
        if (-not $ready) {
            Fail "Stack belum sehat dalam $StartupTimeoutSeconds detik."
        }
        Write-Host "[OK] PostgreSQL sehat, migrator sukses, API dan frontend sehat."

        Write-Host ""
        Write-Host "=== VERIFIKASI RUNTIME ==="
        $loginResponse = Invoke-WebRequest `
            -Uri "http://127.0.0.1:$httpPort/login" `
            -Method Get `
            -TimeoutSec 20 `
            -UseBasicParsing
        if ([int]$loginResponse.StatusCode -ne 200) {
            Fail "Route /login mengembalikan status $($loginResponse.StatusCode)."
        }
        $bootstrapResponse = Invoke-WebRequest `
            -Uri "http://127.0.0.1:$httpPort/api/v1/bootstrap/status" `
            -Method Get `
            -TimeoutSec 20 `
            -UseBasicParsing
        if ([int]$bootstrapResponse.StatusCode -ne 200) {
            Fail "Route bootstrap mengembalikan status $($bootstrapResponse.StatusCode)."
        }

        $expectedSecurityHeaders = [ordered]@{
            "Content-Security-Policy" = "base-uri 'self'; frame-ancestors 'none'; object-src 'none'"
            "Referrer-Policy" = "strict-origin-when-cross-origin"
            "X-Content-Type-Options" = "nosniff"
            "X-Frame-Options" = "DENY"
        }
        foreach ($routeResponse in @(
            [pscustomobject]@{ Route = "/login"; Response = $loginResponse },
            [pscustomobject]@{ Route = "/api/v1/bootstrap/status"; Response = $bootstrapResponse }
        )) {
            foreach ($headerName in $expectedSecurityHeaders.Keys) {
                Assert-ResponseHeader `
                    -Response $routeResponse.Response `
                    -Name $headerName `
                    -ExpectedValue ([string]$expectedSecurityHeaders[$headerName]) `
                    -Route $routeResponse.Route
            }
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
        $frontendPorts = @(Get-DockerOutput "Membaca port frontend" @(
            "port", $serviceIds["frontend"], "3000/tcp"
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($frontendPorts.Count -ne 1 -or
            $frontendPorts[0].Trim() -ne "127.0.0.1:$httpPort") {
            Fail ("Port frontend bukan tepat 127.0.0.1:$httpPort. Aktual: " + ($frontendPorts -join ", "))
        }

        $applicationNetwork = "${composeProject}_application"
        $databaseNetwork = "${composeProject}_database"
        Assert-Networks "PostgreSQL" $serviceIds["postgres"] @($databaseNetwork)
        Assert-Networks "Migrator" $serviceIds["migrator"] @($databaseNetwork)
        Assert-Networks "API" $serviceIds["api"] @($applicationNetwork, $databaseNetwork)
        Assert-Networks "Frontend" $serviceIds["frontend"] @($applicationNetwork)

        Write-Host "[OK] HTTP loopback, empat security header, migration $actualMigration, port, dan isolasi network tervalidasi."
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
            Write-Host "[OK] Container, volume, network, tiga image bertag, dan rahasia sementara dibersihkan."
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
    Write-Host "[OK] Tiga image produksi, migration gate, health, HTTP security headers, port, dan network lulus."
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
