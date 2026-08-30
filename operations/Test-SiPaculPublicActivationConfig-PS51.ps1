#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$EnvironmentFile = "",
    [switch]$RequirePublicActivation,
    [switch]$RequireHsts
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Revision = "Sprint 20D2G3A Public Activation Config Test 1 - PowerShell 5.1"

function Fail([string]$Message) {
    throw $Message
}

try {
    if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        $RepositoryRoot = Split-Path -Parent $PSScriptRoot
    }

    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd("\")
    $modulePath = Join-Path $root "operations\SiPaculDeployment.psm1"

    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
        Fail "Deployment module tidak ditemukan: $modulePath"
    }

    Import-Module -Name $modulePath -Force -ErrorAction Stop

    if ([string]::IsNullOrWhiteSpace($EnvironmentFile)) {
        $EnvironmentFile = Join-Path $root ".env.production"
    }

    $EnvironmentFile = Resolve-SiPaculFilePath `
        -Path $EnvironmentFile `
        -BaseDirectory $root `
        -Label "Production environment"

    [void](Assert-SiPaculProductionEnvironment -EnvironmentFile $EnvironmentFile)
    $envMap = Read-SiPaculEnvironmentFile -Path $EnvironmentFile

    foreach ($name in @(
        "SIPACUL_PUBLIC_ACTIVATION",
        "SIPACUL_PUBLIC_HOSTNAME",
        "SIPACUL_HSTS_ENABLED",
        "SIPACUL_HSTS_MAX_AGE",
        "SIPACUL_BIND_ADDRESS",
        "SIPACUL_HTTPS_PORT"
    )) {
        if (-not $envMap.ContainsKey($name) -or
            [string]::IsNullOrWhiteSpace([string]$envMap[$name])) {
            Fail "Public activation environment wajib tidak tersedia: $name"
        }
    }

    $activation = ([string]$envMap["SIPACUL_PUBLIC_ACTIVATION"]).Trim()
    $hostname = ([string]$envMap["SIPACUL_PUBLIC_HOSTNAME"]).Trim()
    $hsts = ([string]$envMap["SIPACUL_HSTS_ENABLED"]).Trim()
    $maxAgeText = ([string]$envMap["SIPACUL_HSTS_MAX_AGE"]).Trim()
    $bindAddress = ([string]$envMap["SIPACUL_BIND_ADDRESS"]).Trim()
    $httpsPort = ([string]$envMap["SIPACUL_HTTPS_PORT"]).Trim()

    if ($activation -ne "disabled" -and $activation -ne "enabled") {
        Fail "SIPACUL_PUBLIC_ACTIVATION harus disabled atau enabled."
    }

    if ($hsts -ne "false" -and $hsts -ne "true") {
        Fail "SIPACUL_HSTS_ENABLED harus false atau true."
    }

    $maxAge = 0
    if (-not [int]::TryParse($maxAgeText, [ref]$maxAge) -or
        $maxAge -lt 300 -or
        $maxAge -gt 63072000) {
        Fail "SIPACUL_HSTS_MAX_AGE harus integer 300 sampai 63072000."
    }

    if ($activation -eq "disabled") {
        if ($hostname -ne "_") {
            Fail "Activation disabled mewajibkan SIPACUL_PUBLIC_HOSTNAME=_."
        }
        if ($hsts -ne "false") {
            Fail "Activation disabled mewajibkan HSTS=false."
        }
        if ($bindAddress -ne "127.0.0.1") {
            Fail "Activation disabled mewajibkan bind 127.0.0.1."
        }
    }
    else {
        if ($hostname -eq "_" -or $hostname -eq "localhost") {
            Fail "Activation enabled memerlukan hostname publik."
        }

        if ($hostname -notmatch '^[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?(\.[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$') {
            Fail "SIPACUL_PUBLIC_HOSTNAME bukan hostname DNS yang valid."
        }

        $parsedIp = $null
        if ([Net.IPAddress]::TryParse($hostname, [ref]$parsedIp)) {
            Fail "SIPACUL_PUBLIC_HOSTNAME tidak boleh berupa IP address."
        }

        if ($bindAddress -eq "127.0.0.1") {
            Fail "Activation enabled memerlukan bind non-loopback."
        }
        if ($httpsPort -ne "443") {
            Fail "Activation enabled memerlukan SIPACUL_HTTPS_PORT=443."
        }
    }

    if ($RequirePublicActivation -and $activation -ne "enabled") {
        Fail "Public activation diwajibkan oleh parameter tetapi environment masih disabled."
    }

    if ($RequireHsts) {
        if ($activation -ne "enabled") {
            Fail "HSTS tidak boleh diwajibkan sebelum public activation enabled."
        }
        if ($hsts -ne "true") {
            Fail "HSTS diwajibkan oleh parameter tetapi environment masih false."
        }
    }

    Write-Host "=== PUBLIC ACTIVATION CONFIG TEST ==="
    Write-Host "[OK] Repository: $root"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Environment: $EnvironmentFile"
    Write-Host "[OK] Activation: $activation"
    Write-Host "[OK] Hostname: $hostname"
    Write-Host "[OK] Bind/HTTPS port: $bindAddress/$httpsPort"
    Write-Host "[OK] HSTS: $hsts; max-age=$maxAge"
    Write-Host "[OK] TLS certificate/private-key path sudah melewati deployment environment validation."
    Write-Host ""
    Write-Host "=== STATUS AKHIR ==="
    Write-Host "[OK] Public activation configuration contract valid."
    Write-Host "[OK] Read-only; DNS, firewall, certificate, environment file, container, dan repository tidak diubah."
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    Remove-Module SiPaculDeployment -Force -ErrorAction SilentlyContinue
}
