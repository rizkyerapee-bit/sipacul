Set-StrictMode -Version 2.0

function Resolve-SiPaculBackupDirectory {
    param([Parameter(Mandatory = $true)][string]$BackupDirectory)

    $fullPath = [IO.Path]::GetFullPath($BackupDirectory)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        throw "Folder backup tidak ditemukan: $fullPath"
    }

    $trimCharacters = [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $normalized = $fullPath.TrimEnd($trimCharacters)
    $volumeRoot = [IO.Path]::GetPathRoot($fullPath).TrimEnd($trimCharacters)
    if ($normalized.Equals($volumeRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Folder backup tidak boleh berupa root volume."
    }
    return $fullPath
}

function Get-SiPaculBackupSet {
    param([Parameter(Mandatory = $true)][string]$BackupDirectory)

    $root = Resolve-SiPaculBackupDirectory $BackupDirectory
    $allPrefixedFiles = @(Get-ChildItem -LiteralPath $root -File |
        Where-Object { $_.Name -like "sipacul-postgres-*" })
    $dumpFiles = @($allPrefixedFiles |
        Where-Object { $_.Name -match '^sipacul-postgres-(\d{8}T\d{9}Z)\.dump$' })
    $recognized = @{}
    $results = @()
    $dateStyles = [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal

    foreach ($dump in $dumpFiles) {
        if ($dump.Name -notmatch '^sipacul-postgres-(\d{8}T\d{9}Z)\.dump$') {
            throw "Nama archive tidak valid: $($dump.Name)"
        }
        $timestampText = $Matches[1]
        $fileTimestamp = [DateTime]::MinValue
        if (-not [DateTime]::TryParseExact(
            $timestampText,
            "yyyyMMdd'T'HHmmssfff'Z'",
            [Globalization.CultureInfo]::InvariantCulture,
            $dateStyles,
            [ref]$fileTimestamp
        )) {
            throw "Timestamp nama archive tidak valid: $($dump.Name)"
        }

        $checksumPath = $dump.FullName + ".sha256"
        $manifestPath = $dump.FullName + ".json"
        if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
            throw "Sidecar SHA256 tidak ditemukan untuk $($dump.Name)."
        }
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            throw "Manifest tidak ditemukan untuk $($dump.Name)."
        }

        try { $manifest = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json }
        catch { throw "Manifest JSON tidak valid untuk $($dump.Name): $($_.Exception.Message)" }
        $properties = @($manifest.PSObject.Properties.Name)
        foreach ($property in @(
            "schemaVersion", "application", "createdAtUtc", "database", "latestMigration",
            "postgresImage", "pgDumpVersion", "backupFile", "sizeBytes", "sha256"
        )) {
            if ($properties -notcontains $property) {
                throw "Manifest $($dump.Name) tidak memiliki properti $property."
            }
        }
        if ([int]$manifest.schemaVersion -ne 1 -or [string]$manifest.application -ne "SiPacul") {
            throw "Identitas manifest tidak didukung untuk $($dump.Name)."
        }
        if ([string]$manifest.backupFile -ne $dump.Name) {
            throw "Nama archive pada manifest tidak cocok untuk $($dump.Name)."
        }
        if ([int64]$manifest.sizeBytes -ne [int64]$dump.Length -or [int64]$dump.Length -le 0) {
            throw "Ukuran archive tidak cocok untuk $($dump.Name)."
        }

        $manifestHash = ([string]$manifest.sha256).ToUpperInvariant()
        if ($manifestHash -notmatch '^[0-9A-F]{64}$') {
            throw "SHA256 manifest tidak valid untuk $($dump.Name)."
        }
        $checksumLine = [IO.File]::ReadAllText($checksumPath).Trim()
        if ($checksumLine -notmatch '^([0-9A-Fa-f]{64})\s+\*?(.+)$') {
            throw "Format sidecar SHA256 tidak valid untuk $($dump.Name)."
        }
        $sidecarHash = $Matches[1].ToUpperInvariant()
        $sidecarName = $Matches[2].Trim()
        if ($sidecarName -ne $dump.Name -or $sidecarHash -ne $manifestHash) {
            throw "Sidecar SHA256 tidak cocok untuk $($dump.Name)."
        }

        $createdValue = $manifest.createdAtUtc
        if ($createdValue -is [DateTimeOffset]) {
            $createdUtc = ([DateTimeOffset]$createdValue).UtcDateTime
        }
        elseif ($createdValue -is [DateTime]) {
            $createdDate = [DateTime]$createdValue
            if ($createdDate.Kind -eq [DateTimeKind]::Unspecified) {
                $createdDate = [DateTime]::SpecifyKind($createdDate, [DateTimeKind]::Utc)
            }
            $createdUtc = $createdDate.ToUniversalTime()
        }
        else {
            $createdAt = [DateTimeOffset]::MinValue
            if (-not [DateTimeOffset]::TryParse(
                [string]$createdValue,
                [Globalization.CultureInfo]::InvariantCulture,
                $dateStyles,
                [ref]$createdAt
            )) {
                throw "createdAtUtc tidak valid untuk $($dump.Name)."
            }
            $createdUtc = $createdAt.UtcDateTime
        }
        $fileTimestampUtc = $fileTimestamp.ToUniversalTime()
        $manifestDelay = $createdUtc - $fileTimestampUtc
        if ($manifestDelay.TotalMinutes -lt -5 -or $manifestDelay.TotalDays -gt 7) {
            throw "Urutan timestamp nama file dan manifest tidak wajar: $($dump.Name)."
        }

        $recognized[$dump.Name] = $true
        $recognized[(Split-Path -Leaf $checksumPath)] = $true
        $recognized[(Split-Path -Leaf $manifestPath)] = $true
        $results += [PSCustomObject]@{
            PSTypeName = "SiPacul.BackupSet"
            BackupFile = $dump.Name
            DumpPath = $dump.FullName
            ChecksumPath = $checksumPath
            ManifestPath = $manifestPath
            CreatedAtUtc = $fileTimestampUtc
            ManifestCreatedAtUtc = $createdUtc
            Database = [string]$manifest.database
            LatestMigration = [string]$manifest.latestMigration
            SizeBytes = [int64]$dump.Length
            Sha256 = $manifestHash
        }
    }

    foreach ($file in $allPrefixedFiles) {
        if (-not $recognized.ContainsKey($file.Name)) {
            throw "File backup tidak berpasangan atau tidak dikenali: $($file.Name)"
        }
    }

    return @($results | Sort-Object CreatedAtUtc -Descending)
}

function Test-SiPaculBackupHash {
    param([Parameter(Mandatory = $true)]$BackupSet)

    $actualHash = (Get-FileHash -LiteralPath $BackupSet.DumpPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualHash -ne [string]$BackupSet.Sha256) {
        throw "SHA256 archive tidak cocok: $($BackupSet.BackupFile)"
    }
    return $true
}

Export-ModuleMember -Function Resolve-SiPaculBackupDirectory, Get-SiPaculBackupSet, Test-SiPaculBackupHash
