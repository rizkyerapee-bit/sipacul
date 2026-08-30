#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$Hostname,

    [string[]]$ExpectedIpAddress = @(),

    [int]$HttpsPort = 443,

    [string]$LoginPath = "/login",

    [string]$ApiPath = "/api/v1/bootstrap/status",

    [switch]$RequireHsts
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Revision = "Sprint 20D2G3B Public Endpoint Probe Rev1 - PowerShell 5.1"

function Fail([string]$Message) {
    throw $Message
}

function Get-HeaderValue {
    param(
        [Parameter(Mandatory=$true)]
        [System.Net.WebHeaderCollection]$Headers,
        [Parameter(Mandatory=$true)]
        [string]$Name
    )

    $value = $Headers[$Name]
    if ($null -eq $value) {
        return ""
    }

    return [string]$value
}

function Invoke-HttpsProbe {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Uri
    )

    $request = [Net.HttpWebRequest]::Create($Uri)
    $request.Method = "GET"
    $request.AllowAutoRedirect = $false
    $request.Timeout = 15000
    $request.ReadWriteTimeout = 15000
    $request.UserAgent = "SiPacul-PublicEndpoint-Probe/20D2G3B"

    try {
        $response = [Net.HttpWebResponse]$request.GetResponse()
    }
    catch [Net.WebException] {
        if ($null -ne $_.Exception.Response) {
            $response = [Net.HttpWebResponse]$_.Exception.Response
        }
        else {
            throw
        }
    }

    try {
        return New-Object PSObject -Property @{
            StatusCode = [int]$response.StatusCode
            Headers = $response.Headers
        }
    }
    finally {
        $response.Close()
    }
}

function Test-SecurityHeaders {
    param(
        [Parameter(Mandatory=$true)]
        [System.Net.WebHeaderCollection]$Headers,
        [Parameter(Mandatory=$true)]
        [string]$Label
    )

    $required = @(
        "Content-Security-Policy",
        "Referrer-Policy",
        "X-Content-Type-Options",
        "X-Frame-Options"
    )

    foreach ($name in $required) {
        $value = Get-HeaderValue -Headers $Headers -Name $name
        if ([string]::IsNullOrWhiteSpace($value)) {
            Fail "$Label kehilangan security header: $name"
        }
    }
}

try {
    if ([string]::IsNullOrWhiteSpace($Hostname)) {
        Fail "Hostname wajib diisi."
    }

    if ($Hostname -eq "localhost" -or $Hostname -eq "_") {
        Fail "Hostname harus merupakan hostname publik."
    }

    if ($HttpsPort -lt 1 -or $HttpsPort -gt 65535) {
        Fail "HttpsPort tidak valid."
    }

    Write-Host "=== PUBLIC ENDPOINT READINESS PROBE ==="
    Write-Host "[INFO] Revision: $Revision"
    Write-Host "[INFO] Hostname: $Hostname"
    Write-Host "[INFO] HTTPS port: $HttpsPort"

    Write-Host ""
    Write-Host "=== DNS ==="

    $addresses = @(
        [Net.Dns]::GetHostAddresses($Hostname) |
        ForEach-Object { $_.ToString() } |
        Sort-Object -Unique
    )

    if ($addresses.Count -eq 0) {
        Fail "DNS tidak mengembalikan IP address untuk $Hostname."
    }

    foreach ($address in $addresses) {
        Write-Host "[OK] DNS: $Hostname -> $address"
    }

    if ($ExpectedIpAddress.Count -gt 0) {
        foreach ($expected in $ExpectedIpAddress) {
            if ($addresses -notcontains $expected) {
                Fail "Expected IP $expected tidak ditemukan pada DNS $Hostname."
            }
        }

        Write-Host "[OK] Seluruh expected IP ditemukan pada DNS."
    }

    Write-Host ""
    Write-Host "=== TCP / TLS ==="

    $tcp = New-Object Net.Sockets.TcpClient
    try {
        $async = $tcp.BeginConnect($Hostname, $HttpsPort, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne(10000)) {
            Fail "TCP connect timeout ke ${Hostname}:$HttpsPort."
        }

        $tcp.EndConnect($async)
        Write-Host "[OK] TCP ${Hostname}:$HttpsPort terhubung."

        $ssl = New-Object Net.Security.SslStream(
            $tcp.GetStream(),
            $false
        )

        try {
            $ssl.AuthenticateAsClient($Hostname)
            $remoteCertificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2(
                $ssl.RemoteCertificate
            )

            if ($remoteCertificate.NotAfter.ToUniversalTime() -le [DateTime]::UtcNow) {
                Fail "Certificate sudah kedaluwarsa."
            }

            $daysRemaining = [Math]::Floor(
                ($remoteCertificate.NotAfter.ToUniversalTime() - [DateTime]::UtcNow).TotalDays
            )

            Write-Host "[OK] TLS hostname/certificate validation lulus."
            Write-Host "[OK] Certificate subject: $($remoteCertificate.Subject)"
            Write-Host "[OK] Certificate issuer: $($remoteCertificate.Issuer)"
            Write-Host "[OK] Certificate expires UTC: $($remoteCertificate.NotAfter.ToUniversalTime().ToString('u'))"
            Write-Host "[OK] Certificate days remaining: $daysRemaining"

            if ($daysRemaining -lt 14) {
                Fail "Certificate tersisa kurang dari 14 hari."
            }
        }
        finally {
            $ssl.Dispose()
        }
    }
    finally {
        $tcp.Close()
    }

    Write-Host ""
    Write-Host "=== HTTPS LOGIN ==="

    $baseUri = "https://${Hostname}:$HttpsPort"
    $loginUri = $baseUri + $LoginPath
    $login = Invoke-HttpsProbe -Uri $loginUri

    if ($login.StatusCode -lt 200 -or $login.StatusCode -ge 400) {
        Fail "Login endpoint status tidak diterima: $($login.StatusCode)"
    }

    Test-SecurityHeaders -Headers $login.Headers -Label "Login response"

    Write-Host "[OK] Login endpoint status: $($login.StatusCode)"
    Write-Host "[OK] Login security headers tersedia."

    $hsts = Get-HeaderValue `
        -Headers $login.Headers `
        -Name "Strict-Transport-Security"

    if ($RequireHsts) {
        if ([string]::IsNullOrWhiteSpace($hsts)) {
            Fail "HSTS diwajibkan tetapi Strict-Transport-Security tidak tersedia."
        }

        if ($hsts -notmatch '(?i)max-age=[0-9]+') {
            Fail "HSTS header tidak memiliki max-age valid."
        }

        Write-Host "[OK] HSTS tersedia: $hsts"
    }
    else {
        if ([string]::IsNullOrWhiteSpace($hsts)) {
            Write-Host "[OK] HSTS belum aktif; diterima karena -RequireHsts tidak digunakan."
        }
        else {
            Write-Host "[INFO] HSTS sudah tersedia: $hsts"
        }
    }

    Write-Host ""
    Write-Host "=== HTTPS API ==="

    $apiUri = $baseUri + $ApiPath
    $api = Invoke-HttpsProbe -Uri $apiUri

    if ($api.StatusCode -lt 200 -or $api.StatusCode -ge 500) {
        Fail "API probe status tidak diterima: $($api.StatusCode)"
    }

    Test-SecurityHeaders -Headers $api.Headers -Label "API response"

    Write-Host "[OK] API probe status: $($api.StatusCode)"
    Write-Host "[OK] API security headers tersedia."

    Write-Host ""
    Write-Host "=== STATUS AKHIR ==="
    Write-Host "[OK] DNS, TCP 443, TLS certificate, login, API, dan security headers lulus."
    if ($RequireHsts) {
        Write-Host "[OK] HSTS gate lulus."
    }
    Write-Host "[OK] Probe read-only; tidak mengubah DNS, certificate, firewall, container, database, atau repository."
    Write-Host ""
    Write-Host "SIPACUL PUBLIC ENDPOINT READINESS: PASS" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
