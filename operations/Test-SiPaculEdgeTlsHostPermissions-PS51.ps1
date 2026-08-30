param(
    [string]$RepositoryRoot = "D:\Development\Projects\SiPacul"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20D2G3C7B1 Edge TLS Host Permissions Validator 3 - PowerShell 5.1"

function Fail([string]$Message) {
    throw $Message
}

function Get-RepoPath {
    param([string]$Root, [string]$Relative)
    return Join-Path $Root ($Relative.Replace("/", "\"))
}

function Read-LfText {
    param(
        [string]$Path,
        [string]$Label,
        [bool]$CheckTrailingWhitespace = $false
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Fail "$Label tidak ditemukan: $Path"
    }

    $bytes = [IO.File]::ReadAllBytes($Path)
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -eq 13) {
            Fail "$Label harus LF-only."
        }
    }

    $text = [Text.Encoding]::UTF8.GetString($bytes)

    if ($CheckTrailingWhitespace) {
        foreach ($line in ($text -split "`n")) {
            if ($line -match '[ \x09]+$') {
                Fail "$Label memiliki trailing whitespace."
            }
        }
    }

    return $text
}

try {
    Write-Host "=== VALIDASI SPRINT 20D2G3C7B1 EDGE TLS HOST PERMISSIONS ==="
    Write-Host "[INFO] $Revision"

    $rootResult = @(& git.exe -C $RepositoryRoot rev-parse --show-toplevel 2>&1)
    if ($LASTEXITCODE -ne 0 -or $rootResult.Count -ne 1) {
        Fail "Repository root tidak dapat dibaca."
    }

    $root = [IO.Path]::GetFullPath(([string]$rootResult[0]).Trim())

    $composePath = Get-RepoPath $root "compose.production.yml"
    $envPath = Get-RepoPath $root "production.env.example"
    $dockerfilePath = Get-RepoPath $root "edge/Dockerfile"
    $docPath = Get-RepoPath $root "docs/19-edge-tls-host-permissions.md"

    $compose = Read-LfText $composePath "compose.production.yml" $false
    $envTemplate = Read-LfText $envPath "production.env.example" $false
    $dockerfile = Read-LfText $dockerfilePath "edge/Dockerfile" $false
    $doc = Read-LfText $docPath "documentation" $true

    $edgePattern = '(?ms)^  edge:\n(?:(?!^  [A-Za-z0-9_-]+:).)*?^    image: "\$\{SIPACUL_EDGE_IMAGE:-sipacul-edge:local\}"\n    user: "101:0"\n'
    if ([regex]::Matches($compose, $edgePattern).Count -ne 1) {
        Fail 'Compose edge service harus memiliki tepat satu user: "101:0" setelah image.'
    }

    if ([regex]::Matches($compose, '(?m)^    user: "101:0"$').Count -ne 1) {
        Fail 'Compose harus memiliki tepat satu explicit edge runtime identity 101:0.'
    }

    if ($dockerfile -notmatch '(?m)^FROM nginxinc/nginx-unprivileged:1\.30\.4-alpine$') {
        Fail "Edge Dockerfile base image contract berubah."
    }

    if ([regex]::Matches($dockerfile, '(?m)^USER 101$').Count -ne 1) {
        Fail "Edge Dockerfile harus tetap berakhir pada unprivileged UID 101."
    }

    $requiredEnvMarkers = @(
        '# Private key host tetap root:root; edge membacanya sebagai UID 101:GID 0.',
        'SIPACUL_PUBLIC_ACTIVATION=disabled',
        'SIPACUL_PUBLIC_HOSTNAME=_',
        'SIPACUL_HSTS_ENABLED=false',
        'SIPACUL_BIND_ADDRESS=127.0.0.1',
        'SIPACUL_HTTPS_PORT=8443'
    )

    foreach ($marker in $requiredEnvMarkers) {
        if ($envTemplate.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
            Fail "production.env.example kehilangan marker: $marker"
        }
    }

    $requiredDocMarkers = @(
        'user: "101:0"',
        'certificate: root:root 0644',
        'private key: root:root 0640',
        'owned by host UID 101',
        'group-owned by host GID 101',
        '/etc/sipacul/.env.production',
        'SIPACUL_PUBLIC_ACTIVATION=disabled',
        'SIPACUL_BIND_ADDRESS=127.0.0.1'
    )

    foreach ($marker in $requiredDocMarkers) {
        if ($doc.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
            Fail "documentation kehilangan marker: $marker"
        }
    }

    if ($doc -match '(?i)private key.*(?:0644|0664|0666|0777)') {
        Fail "Documentation tidak boleh menyarankan private key world-readable."
    }

    if ($doc -match '(?i)chown\s+.*(?:101[:.]|[:.]101).*tls') {
        Fail "Documentation tidak boleh menyarankan chown TLS ke host UID/GID 101."
    }

    $diffCheck = @(& git.exe -C $root diff --check 2>&1)
    if ($LASTEXITCODE -ne 0) {
        Fail ("git diff --check gagal:`n" + ($diffCheck -join "`n"))
    }

    Write-Host "[OK] Existing tracked files divalidasi LF-only; regex trailing whitespace menggunakan SPACE/TAB aktual dan changed-lines tetap melalui git diff --check."
    Write-Host "[OK] Edge Compose runtime identity eksplisit UID 101:GID 0."
    Write-Host "[OK] Edge image tetap unprivileged USER 101."
    Write-Host "[OK] Host TLS private-key contract root:root 0640 terdokumentasi."
    Write-Host "[OK] Public activation tetap disabled/loopback dan HSTS false."
    Write-Host "[OK] Tidak ada guidance chown TLS ke host UID/GID 101 atau private-key world-readability."
    Write-Host ""
    Write-Host "SIPACUL EDGE TLS HOST PERMISSIONS: PASS"
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
