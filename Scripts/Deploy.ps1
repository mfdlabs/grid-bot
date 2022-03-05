param (
    [string]$root,
    [string]$deploymentKind,
    [string]$config,
    [string]$targetFramework = "net472",
    [bool]$isGitIntegrated = $true,
    [bool]$replaceExtensions = $true,
    [bool]$checkForExistingSourceArchive = $true,
    [bool]$checkForExistingConfigArchive = $true,
    [bool]$writeUnpackerScripts = $true
)

$date = Get-Date;


$7zipPath = "C:\Program Files\7-Zip\7z.exe"

if (-not (Test-Path -Path $7zipPath -PathType Leaf)) {
    & Write-Host "7Zip not found on the system." -ForegroundColor Red
    Exit
}


& Set-Alias 7zip-Archive $7zipPath

function GetParsedNumber([int] $num) {
    return [string]::new("0", 2 - $num.ToString().Length) + $num.ToString()
}

function GetRandomHexString([int] $bits = 256) {
    $bytes = new-object 'System.Byte[]' ($bits / 8)
    (new-object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
    (new-object System.Runtime.Remoting.Metadata.W3cXsd2001.SoapHexBinary @(, $bytes)).ToString()
}

function GetArchiveHash([string] $File) {
    if (!(Test-Path -Path $File -PathType Leaf)) {
        return $null
    }

    $fileInfo = Get-ItemProperty -Path $File

    $archivePath = "$env:temp\$($fileInfo.Name)\"

    try {

        if (![System.IO.Directory]::Exists($archivePath)) {
            & Write-Host "Creating temp directory $archivePath" -ForegroundColor Green
            [System.IO.Directory]::CreateDirectory($archivePath) *> $null;
        }

        7zip-Archive x $File "-o$archivePath" -y *> $null

        $data = 7zip-Archive h $archivePath* | FindStr "data:";

        return ($data -split ":")[1].Trim();
    }
    finally {
        & Write-Host "Removing temp directory $archivePath" -ForegroundColor Green
        [System.IO.Directory]::Delete($archivePath, $true)
    }
}

function HasGitRepository([string] $path) {
    if ([string]::IsNullOrEmpty($path)) {
        throw "`$path is null or empty";
    }

    $gitDir = [System.IO.Path]::Combine($path, ".git");
    $exists = [System.IO.Directory]::Exists($gitDir);
	
    Write-Host "Test path $($gitDir): $exists";

    return $exists;
}

function ReadGitBranch([string] $from) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    $branch = (Invoke-Expression -Command "git rev-parse --abbrev-ref HEAD");

    if ([string]::IsNullOrEmpty($branch)) {
        return $null
    }

    if ($branch.StartsWith("fatal")) {
        return $null
    }

    return $branch;
}


function CheckHash([string]$newLocation, [string]$existingLocation) {
    & Write-Host "Checking hash for $newLocation" -ForegroundColor Green

    if ([string]::IsNullOrEmpty($existingLocation)) {
        & Write-Host "Existing location is null or empty" -ForegroundColor Yellow
        return $false;
    }

    # determine if the contents of the currently deploying source archive is the same as the last deployed source archive
    $existingSourceArchiveHash = GetArchiveHash -File $existingLocation
    $sourceArchiveHash = GetArchiveHash -File $newLocation

    & Write-Host "New location: $newLocation" -ForegroundColor Green
    & Write-Host "Existing location: $existingLocation" -ForegroundColor Green
    & Write-Host "Existing Archive Hash: $existingSourceArchiveHash" -ForegroundColor Green
    & Write-Host "New Archive Hash: $sourceArchiveHash" -ForegroundColor Green
    & Write-Host "Comparing existing archive hash to new archive hash..." -ForegroundColor Green
    & Write-Host "Existing Archive Hash == New Archive Hash: $(IF($existingSourceArchiveHash -eq $sourceArchiveHash){"Yes"} ELSE {"No"})" -ForegroundColor Green

    return $existingSourceArchiveHash -eq $sourceArchiveHash
}

function ReadGitHash([string] $from, [bool] $readShortHash = $false) {
    if ([string]::IsNullOrEmpty($from)) {
        throw $null;
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    $gitHash = "";

    if ($readShortHash) {
        $gitHash = (Invoke-Expression -Command "git rev-parse --short HEAD")
    }
    else {
        $gitHash = (Invoke-Expression -Command "git rev-parse HEAD")
    }

    # Trim and then remove the git hash spaces via regex
    $gitHash = $gitHash.Trim().Replace(" ", "");

    if ([string]::IsNullOrEmpty($gitHash)) {
        return $null
    }

    if ($gitHash.StartsWith("fatal")) {
        return $null
    }

    return $gitHash;
}

<#
Attempts to read the short hash revision from the directory for github revisions
#>
function ReadGitShortHash([string] $from) {
    $hash = $(ReadGitHash -from $from -readShortHash $true);
    $hash = if ($null -ne $hash) { $hash.Trim().Replace(" ", "") -replace '\t', '' } else { $null }
    return $hash;
}

# TODO: Seperate configurations from the rest of the code

IF ([string]::IsNullOrEmpty($root)) {
    $root = (Get-Location).Path;
}

IF (!$root.EndsWith("\")) {
    $root = $root + "\"
}

if (!(Test-Path $root)) {
    & Write-Host "The root directory does not exist." -ForegroundColor Red
    Exit
}

& Write-Host "Trying to deploy $deploymentKind => $config|$(IF ([string]::IsNullOrEmpty($targetFramework)){"No Specific Framework"}ELSE{$targetFramework}) from root $root at date $date" -ForegroundColor Green


[string] $newSourceArchive;
[string] $newConfigArchive;
[string] $sourceArchive;
[string] $configArchive;
[bool] $deleteArchivesBecauseNotFinished = $true;

try {
    
    $location = $root
    $deploymentFolder = "$($location)$($deploymentKind)\Deploy\"
    $deploymentYear = "$($deploymentFolder)$($date.Year)\"
	
    $componentDir = IF ([string]::IsNullOrEmpty($deploymentKind)) { "$($location)\bin\" } ELSE { "$($location)$($deploymentKind)\bin\" }
    $componentDir = IF ([string]::IsNullOrEmpty($config)) { $componentDir } ELSE { "$($componentDir)$($config)\" }
    $componentDir = IF ([string]::IsNullOrEmpty($targetFramework)) { $componentDir } ELSE { "$($componentDir)$($targetFramework)\" }

    IF (![System.IO.Directory]::Exists($deploymentFolder)) {
        & Write-Host "The deployment folder at $($deploymentFolder) does not exist, creating..." -ForegroundColor Yellow
        [System.IO.Directory]::CreateDirectory($deploymentFolder) *>$null
    }

    IF (![System.IO.Directory]::Exists($deploymentYear)) {
        & Write-Host "The deployment year folder at $($deploymentYear) does not exist, creating..." -ForegroundColor Yellow
        [System.IO.Directory]::CreateDirectory($deploymentYear) *>$null
    }

    IF (![System.IO.Directory]::Exists($componentDir)) {
        & Write-Host "The component bin folder at $($componentDir) does not exist, aborting..." -ForegroundColor Red
        Exit
    }

    # If the component directory is empty, we can't deploy
    IF ([System.IO.Directory]::GetFiles($componentDir, "*.*", [System.IO.SearchOption]::AllDirectories).Length -eq 0) {
        & Write-Host "The component bin folder at $($componentDir) is empty, aborting..." -ForegroundColor Red
        Exit
    }

    IF ([string]::IsNullOrEmpty($targetFramework)) { $targetFramework = "DotNet"; }
    IF ([string]::IsNullOrEmpty($config)) { $config = "AnyConfiguration"; }

    [String] $hash;
    [String] $branch;

    if ($isGitIntegrated) {
        $branch = (ReadGitBranch -from $root);
        $hash = (ReadGitShortHash -from $root);

        if ($null -eq $branch) {
            $branch = "master";
        }

        if ($null -eq $hash) {
            $hash = (GetRandomHexString -Bits 128).ToLower().Substring(0, 7);
        }

        $branch = $branch.Replace("/", "_");
    }
    else {
        $hash = (GetRandomHexString -Bits 128).ToLower().Substring(0, 7);
        $branch = "master"
    }


    & Write-Host "Got Git Branch: $branch, Is Fake Branch: $(IF($isGitIntegrated){"No"} ELSE {"Yes"})" -ForegroundColor Green
    & Write-Host "Got Git Hash: $hash, Is Fake Hash: $(IF($isGitIntegrated){"No"} ELSE {"Yes"})" -ForegroundColor Green

    $archivePrefixName = "$($date.Year).
                      $(GetParsedNumber -num $date.Month).
                      $(GetParsedNumber -num $date.Day)-
                      $(GetParsedNumber -num $date.Hour).
                      $(GetParsedNumber -num $date.Minute).
                      $(GetParsedNumber -num $date.Second)_
                      $($branch)_
                      $($hash)-
                      $($targetFramework)" -replace '\s+', '';

    $archivePrefix = "$($deploymentYear)$($archivePrefixName)" -replace '\s+', '';

    Write-Host "Archive Prefix: $archivePrefix" -ForegroundColor Green

    $sourceArchive = "$($archivePrefix)-$($config).zip"
    $configArchive = "$($archivePrefix)-$($config)Config.zip"
    $existingSourceArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*-$($targetFramework)-$($config).zip", [System.IO.SearchOption]::TopDirectoryOnly)[-1];
    $existingConfigArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*-$($targetFramework)-$($config)Config.zip", [System.IO.SearchOption]::TopDirectoryOnly)[-1];

    & Write-Host "Deploying source archive $componentDir*.pdb,*.dll,*.xml to $($sourceArchive)" -ForegroundColor Green
    & Write-Host "Deploying config archive $componentDir*.config to $($configArchive)" -ForegroundColor Green

    & 7zip-Archive a -r -bb3 -y -tzip $sourceArchive "$componentDir*.*" "-x!*.config"
    & 7zip-Archive a -r -bb3 -y -tzip $configArchive "$componentDir*.config"

    [bool] $deployingNewSource = $true;
    [bool] $deployingNewConfig = $true;

    if ($checkForExistingSourceArchive) {

        if (CheckHash -NewLocation $sourceArchive -ExistingLocation $existingSourceArchive) {
            & Write-Host "There is already an existing source archive with the same hash, deleting the new source archive... Please refer to the old source archive $($existingSourceArchive) for details." -ForegroundColor Yellow
            $newSourceArchive = $existingSourceArchive
            $deployingNewSource = $false
            [System.IO.File]::Delete($sourceArchive)
        }
        else {
            $existingSourceArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*-$($targetFramework)-$($config).mfdlabs-archive", [System.IO.SearchOption]::TopDirectoryOnly)[-1];

            if (![string]::IsNullOrEmpty($existingSourceArchive)) {
                & Rename-Item -Path $existingSourceArchive -NewName ("$existingSourceArchive" -replace ".mfdlabs-archive", ".zip")
                $existingSourceArchive = ("$existingSourceArchive" -replace ".mfdlabs-archive", ".zip")
            }

            & Write-Host "Existing source archive: $($existingSourceArchive)" -ForegroundColor Yellow

            if (CheckHash -NewLocation $sourceArchive -ExistingLocation $existingSourceArchive) {
                if (![string]::IsNullOrEmpty($existingSourceArchive)) {
                    & Rename-Item -Path $existingSourceArchive -NewName ("$existingSourceArchive" -replace ".zip", ".mfdlabs-archive")
                    $existingSourceArchive = ("$existingSourceArchive" -replace ".zip", ".mfdlabs-archive")
                }

                & Write-Host "There is already an existing source archive with the same hash, deleting the new source archive... Please refer to the old source archive $($existingSourceArchive) for details." -ForegroundColor Yellow
                $newSourceArchive = $existingSourceArchive
                $deployingNewSource = $false
                [System.IO.File]::Delete($sourceArchive)
            }

            if (![string]::IsNullOrEmpty($existingSourceArchive) -and $deployingNewSource) {
                & Rename-Item -Path $existingSourceArchive -NewName ("$existingSourceArchive" -replace ".zip", ".mfdlabs-archive")
                $existingSourceArchive = ("$existingSourceArchive" -replace ".zip", ".mfdlabs-archive")
            }
        }
    }

    if ($checkForExistingConfigArchive) {
        if (CheckHash -NewLocation $configArchive -ExistingLocation $existingConfigArchive) {
            & Write-Host "There is already an existing config archive with the same hash, deleting the new config archive... Please refer to the old config archive $($existingConfigArchive) for details." -ForegroundColor Yellow
            $newConfigArchive = $existingConfigArchive
            $deployingNewConfig = $false
            [System.IO.File]::Delete($configArchive)
        }
        else {
            $existingConfigArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*-$($targetFramework)-$($config)Config.mfdlabs-config-archive", [System.IO.SearchOption]::TopDirectoryOnly)[-1];

            if (![string]::IsNullOrEmpty($existingConfigArchive)) {
                & Rename-Item -Path $existingConfigArchive -NewName ("$existingConfigArchive" -replace ".mfdlabs-config-archive", ".zip")
                $existingConfigArchive = ("$existingConfigArchive" -replace ".mfdlabs-config-archive", ".zip")
            }

            if (CheckHash -NewLocation $configArchive -ExistingLocation $existingConfigArchive) {
                if (![string]::IsNullOrEmpty($existingConfigArchive)) {
                    & Rename-Item -Path $existingConfigArchive -NewName ("$existingConfigArchive" -replace ".zip", ".mfdlabs-config-archive")
                    $existingConfigArchive = ("$existingConfigArchive" -replace ".zip", ".mfdlabs-config-archive")
                }

                & Write-Host "There is already an existing config archive with the same hash, deleting the new config archive... Please refer to the old config archive $($existingConfigArchive) for details." -ForegroundColor Yellow
                $newConfigArchive = $existingConfigArchive
                $deployingNewConfig = $false
                [System.IO.File]::Delete($configArchive)
            }

            if (![string]::IsNullOrEmpty($existingConfigArchive) -and $deployingNewConfig) {
                & Rename-Item -Path $existingConfigArchive -NewName ("$existingConfigArchive" -replace ".zip", ".mfdlabs-config-archive")
                $existingConfigArchive = ("$existingConfigArchive" -replace ".zip", ".mfdlabs-config-archive")
            }
        }
    }


    if ($deployingNewSource) {
        $newSourceArchive = $sourceArchive;

        if ($replaceExtensions) {
            $newSourceArchive = $sourceArchive.Replace(".zip", ".mfdlabs-archive")
            & Write-Host "Renaming $($sourceArchive) to $($newSourceArchive)" -ForegroundColor Green
            & Rename-Item -Path $sourceArchive -NewName $newSourceArchive
        }
    }

    if ($deployingNewConfig) {
        $newConfigArchive = $configArchive;

        if ($replaceExtensions) {
            $newConfigArchive = $configArchive.Replace(".zip", ".mfdlabs-config-archive")
            & Write-Host "Renaming $($configArchive) to $($newConfigArchive)" -ForegroundColor Green
            & Rename-Item -Path $configArchive -NewName $newConfigArchive
        }
    }

    # If we aren't deploying a new component archive, but we are deploying a config archive, then delete the new archive and warn the user
    if (!$deployingNewSource -and $deployingNewConfig) {
        & Write-Host "We are not deploying a new component archive, but we are deploying a config archive. Ignoring..." -ForegroundColor Yellow
        Exit;
    }

    $deleteArchivesBecauseNotFinished = $false;

    & Write-Host "Completed deployment of ""$($deploymentKind)"". Source Archive: ""$($newSourceArchive)"", Configuration Archive: ""$($newConfigArchive)"", press enter to continue." -ForegroundColor Green

    if ($writeUnpackerScripts) {
        $defaultPs1ScriptToExtract = "& Set-Alias 7z ""$7zipPath""`n7z x -y ""-o{{SOURCE_NAME}}"" ""{{SOURCE_ARCHIVE}}""";

        $defaultBatchScriptPs1Wrapper = "
@echo off
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Unrestricted ""{{PS1_SCRIPT}}""
"


        & Write-Host "Writing unpacker scripts..." -ForegroundColor Green

        $outputDir = $newSourceArchive.Replace(".zip", "").Replace(".mfdlabs-archive", "").Replace("$($deploymentYear)", "");
        [string] $outSourceArchive = "$outputDir.zip";

        if ($replaceExtensions) {
            $outSourceArchive = "$outputDir.mfdlabs-archive"
        }

        $unpackerPs1ScriptContents = $defaultPs1ScriptToExtract.Replace("{{SOURCE_NAME}}", $outputDir).Replace("{{SOURCE_ARCHIVE}}", $outSourceArchive);

        if ($deployingNewConfig) {
            [string] $outConfigArchive = "$($outputDir)Config.zip";

            if ($replaceExtensions) {
                $outConfigArchive = "$($outputDir)Config.mfdlabs-config-archive"
            }

            $unpackerPs1ScriptContents += "`n7z x -y ""-o$outputDir"" ""$outConfigArchive"""
        }

        $unpackerPs1ScriptContents += "`nexit;";

        $ps1Name = "$($outputDir).Unpacker.ps1";
        $batName = "$($outputDir).Unpacker.bat";

        # Write the unpacker script
        [System.IO.File]::WriteAllText("$($deploymentYear)$($ps1Name)", $unpackerPs1ScriptContents);

        & Write-Host "Wrote unpacker script $($ps1Name)" -ForegroundColor Green

        $unpackerWrapperContents = $defaultBatchScriptPs1Wrapper.Replace("{{PS1_SCRIPT}}", ".\$($ps1Name)");

        # Write the unpacker batch wrapper
        [System.IO.File]::WriteAllText("$($deploymentYear)$($batName)", $unpackerWrapperContents);

        & Write-Host "Wrote unpacker batch wrapper $($batName)" -ForegroundColor Green
    }
}
finally {
    & Write-Host "Cleaning up..." -ForegroundColor Green
    if ($deleteArchivesBecauseNotFinished) {
        # Delete the archive if it exists

        if ($null -ne $sourceArchive) {
            if ([System.IO.File]::Exists($sourceArchive)) {
                & Write-Host "Deleting $($sourceArchive)" -ForegroundColor Green
                [System.IO.File]::Delete($sourceArchive)
            }
        }

        if ($null -ne $configArchive) {
            if ([System.IO.File]::Exists($configArchive)) {
                & Write-Host "Deleting $($configArchive)" -ForegroundColor Green
                [System.IO.File]::Delete($configArchive)
            }
        }
    }
}