[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$Revision = "Sprint 20D1 Local Release Gate 1 - PowerShell 5.1"

function Fail([string]$Message) {
    throw $Message
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

function Run-Git([string[]]$Arguments) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& git.exe @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0) {
        Fail ("git.exe gagal: " + (($output | ForEach-Object { [string]$_ }) -join "`n"))
    }

    return @($output | ForEach-Object { [string]$_ })
}

try {
    Write-Host "=== PREFLIGHT RELEASE GATE SIPACUL ==="

    foreach ($command in @("git.exe", "dotnet.exe", "npm.cmd")) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
            Fail "$command tidak ditemukan."
        }
    }

    $repoRoot = (Run-Git @("rev-parse", "--show-toplevel") | Select-Object -First 1).Trim()
    $repoRoot = [IO.Path]::GetFullPath($repoRoot)
    Set-Location -LiteralPath $repoRoot

    $branch = (Run-Git @("branch", "--show-current") | Select-Object -First 1).Trim()
    $head = (Run-Git @("rev-parse", "--short=7", "HEAD") | Select-Object -First 1).Trim()
    $statusBefore = @(Run-Git @("status", "--porcelain=v1", "--untracked-files=all"))

    $solutionPath = Join-Path $repoRoot "backend\SiPacul.slnx"
    $frontendRoot = Join-Path $repoRoot "frontend"
    foreach ($path in @(
        $solutionPath,
        (Join-Path $frontendRoot "package.json"),
        (Join-Path $frontendRoot "package-lock.json")
    )) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            Fail "Input release gate tidak ditemukan: $path"
        }
    }

    Write-Host "[OK] Repository/branch/HEAD: $repoRoot / $branch / $head"
    Write-Host "[OK] Script revision: $Revision"
    Write-Host "[OK] Gate bersifat read-only terhadap source, Git, Docker, dan database."

    Write-Host ""
    Write-Host "=== BACKEND RELEASE TEST ==="
    Invoke-External `
        "Backend Release test" `
        "dotnet.exe" `
        @("test", $solutionPath, "--configuration", "Release", "--nologo")

    Write-Host ""
    Write-Host "=== FRONTEND TEST ==="
    Set-Location -LiteralPath $frontendRoot
    Invoke-External "Frontend test" "npm.cmd" @("run", "test:run")

    Write-Host ""
    Write-Host "=== FRONTEND LINT ==="
    Invoke-External "Frontend lint" "npm.cmd" @("run", "lint")

    Write-Host ""
    Write-Host "=== FRONTEND PRODUCTION BUILD ==="
    $telemetryBefore = $env:NEXT_TELEMETRY_DISABLED
    try {
        $env:NEXT_TELEMETRY_DISABLED = "1"
        Invoke-External "Frontend production build" "npm.cmd" @("run", "build")
    }
    finally {
        $env:NEXT_TELEMETRY_DISABLED = $telemetryBefore
    }

    Set-Location -LiteralPath $repoRoot
    $headAfter = (Run-Git @("rev-parse", "--short=7", "HEAD") | Select-Object -First 1).Trim()
    $statusAfter = @(Run-Git @("status", "--porcelain=v1", "--untracked-files=all"))
    if ($headAfter -ne $head) {
        Fail "HEAD berubah selama release gate: $head menjadi $headAfter."
    }
    if (($statusAfter -join "`n") -cne ($statusBefore -join "`n")) {
        Fail "Status Git berubah selama release gate."
    }

    Write-Host ""
    Write-Host "=== STATUS AKHIR RELEASE GATE ==="
    Write-Host "[OK] Backend Release test, frontend test, lint, dan production build lulus."
    Write-Host "[OK] HEAD dan status Git tidak berubah."
    Write-Host "[OK] Tidak ada Docker, migration, database, staging, commit, atau push."
}
catch {
    Write-Host ""
    Write-Host "[GAGAL] $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Perbaiki kegagalan sebelum menyatakan build sebagai Release Candidate."
    exit 1
}
