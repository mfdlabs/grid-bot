param (
    [string]$root,
    [string]$deploymentKind,
    [string]$config,
    [string]$targetFramework = "net48",
    [bool]$isGitIntegrated = $true,
    [bool]$replaceExtensions = $true,
    [bool]$checkForExistingSourceArchive = $true,
    [bool]$checkForExistingConfigArchive = $true,
    [bool]$writeUnpackerScripts = $true,
    [bool]$writeNewRelease = $true,
    [bool]$preRelease = $false,
    [bool]$allowPreReleaseGridDeployment = $false,
    [string]$githubToken = $null,
    [string]$remoteName = "origin",
    [string]$releasePrefix = $null,
    [bool]$skipDefer = $false
)

if ($false -eq $skipDefer) {
    $randomWait = Get-Random -Minimum 1.0 -Maximum 20.0

    & Write-Host "Deferring for $randomWait seconds to ensure that other machines are not creating duplicate releases." -ForegroundColor Yellow

    & Start-Sleep -Seconds $randomWait
}

$date = Get-Date;

. $PSScriptRoot\Library\TextHelpers.ps1;
. $PSScriptRoot\Library\GithubHelpers.ps1;
. $PSScriptRoot\Library\7zipHelpers.ps1;
. $PSScriptRoot\Library\DeploymentHelpers.ps1;

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

IF ([string]::IsNullOrEmpty($env:GITHUB_TOKEN) -and ![string]::IsNullOrEmpty($githubToken)) {
    $env:GITHUB_TOKEN = $githubToken
}

& Write-Host "Trying to deploy $deploymentKind => $config|$(IF ([string]::IsNullOrEmpty($targetFramework)){"No Specific Framework"}ELSE{$targetFramework}) from root $root at date $date" -ForegroundColor Green

[string] $newSourceArchive;
[string] $newConfigArchive;
[string] $sourceArchive;
[string] $configArchive;
[bool] $deleteArchivesBecauseNotFinished = $true;

$location = $root
$deploymentFolder = [System.IO.Path]::Combine($location, $deploymentKind, "Deploy");
$deploymentYear = [System.IO.Path]::Combine($deploymentFolder, $date.Year);

try {
    $componentDir = IF ([string]::IsNullOrEmpty($deploymentKind)) { [System.IO.Path]::Combine($location, "bin") } ELSE { [System.IO.Path]::Combine($location, $deploymentKind, "bin") }
    $componentDir = IF ([string]::IsNullOrEmpty($config)) { $componentDir } ELSE { [System.IO.Path]::Combine($componentDir, $config) }
    $componentDir = IF ([string]::IsNullOrEmpty($targetFramework)) { $componentDir } ELSE { [System.IO.Path]::Combine($componentDir, $targetFramework) }

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

    $deploymentFiles = new-object system.collections.arraylist;

    $versioningTag = "$($date.Year).
                    $(GetParsedNumber -num $date.Month).
                    $(GetParsedNumber -num $date.Day)-
                    $(GetParsedNumber -num $date.Hour).
                    $(GetParsedNumber -num $date.Minute).
                    $(GetParsedNumber -num $date.Second)_
                    $($branch)_
                    $($hash)" -replace '\s+', '';

    $archivePrefixName = "$($versioningTag)_$($targetFramework)" -replace '\s+', '';
    $archivePrefix = [System.IO.Path]::Combine($deploymentYear, $archivePrefixName) -replace '\s+', '';

    Write-Host "Archive Prefix: $archivePrefix" -ForegroundColor Green

    $sourceArchive = "$($archivePrefix)-$($config).zip"
    $configArchive = "$($archivePrefix)-$($config)Config.zip"
    $existingSourceArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*_$($targetFramework)-$($config).zip", [System.IO.SearchOption]::TopDirectoryOnly)[-1];
    $existingConfigArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*_$($targetFramework)-$($config)Config.zip", [System.IO.SearchOption]::TopDirectoryOnly)[-1];

    & Write-Host "Deploying source archive $componentDir\*.pdb,*.dll,*.xml to $($sourceArchive)" -ForegroundColor Green
    & Write-Host "Deploying config archive $componentDir\*.config,appsettings.json,appsettings.*.json to $($configArchive)" -ForegroundColor Green

    & 7zip-Archive a -r -bb3 -y -tzip $sourceArchive "$componentDir\*.*" "-x!*.config" "-x!appsettings.json" "-x!appsettings.*.json"
    & 7zip-Archive a -r -bb3 -y -tzip $configArchive "$componentDir\*.config" "$componentDir\appsettings.json" "$componentDir\appsettings.*.json"

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
            $existingSourceArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*_$($targetFramework)-$($config).mfdlabs-archive", [System.IO.SearchOption]::TopDirectoryOnly)[-1];

            if (![string]::IsNullOrEmpty($existingSourceArchive)) {
                $fileName = Split-Path $existingSourceArchive -Leaf

                & Rename-Item -Path $existingSourceArchive -NewName ($fileName -replace ".mfdlabs-archive", ".zip")
                $existingSourceArchive = ("$existingSourceArchive" -replace ".mfdlabs-archive", ".zip")
            }

            & Write-Host "Existing source archive: $($existingSourceArchive)" -ForegroundColor Yellow

            if (CheckHash -NewLocation $sourceArchive -ExistingLocation $existingSourceArchive) {
                if (![string]::IsNullOrEmpty($existingSourceArchive)) {
                    $fileName = Split-Path $existingSourceArchive -Leaf

                    & Rename-Item -Path $existingSourceArchive -NewName ($fileName -replace ".zip", ".mfdlabs-archive")
                    $existingSourceArchive = ("$existingSourceArchive" -replace ".zip", ".mfdlabs-archive")
                }

                & Write-Host "There is already an existing source archive with the same hash, deleting the new source archive... Please refer to the old source archive $($existingSourceArchive) for details." -ForegroundColor Yellow
                $newSourceArchive = $existingSourceArchive
                $deployingNewSource = $false
                [System.IO.File]::Delete($sourceArchive)
            }

            if (![string]::IsNullOrEmpty($existingSourceArchive) -and $deployingNewSource) {
                $fileName = Split-Path $existingSourceArchive -Leaf

                & Rename-Item -Path $existingSourceArchive -NewName ($fileName -replace ".zip", ".mfdlabs-archive")
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
            $existingConfigArchive = [System.IO.Directory]::GetFiles($deploymentYear, "*_$($targetFramework)-$($config)Config.mfdlabs-config-archive", [System.IO.SearchOption]::TopDirectoryOnly)[-1];

            if (![string]::IsNullOrEmpty($existingConfigArchive)) {
                $fileName = Split-Path $existingConfigArchive -Leaf

                & Rename-Item -Path $existingConfigArchive -NewName ($fileName -replace ".mfdlabs-config-archive", ".zip")
                $existingConfigArchive = ("$existingConfigArchive" -replace ".mfdlabs-config-archive", ".zip")
            }

            if (CheckHash -NewLocation $configArchive -ExistingLocation $existingConfigArchive) {
                if (![string]::IsNullOrEmpty($existingConfigArchive)) {
                    $fileName = Split-Path $existingConfigArchive -Leaf

                    & Rename-Item -Path $existingConfigArchive -NewName ($fileName -replace ".zip", ".mfdlabs-config-archive")
                    $existingConfigArchive = ("$existingConfigArchive" -replace ".zip", ".mfdlabs-config-archive")
                }

                & Write-Host "There is already an existing config archive with the same hash, deleting the new config archive... Please refer to the old config archive $($existingConfigArchive) for details." -ForegroundColor Yellow
                $newConfigArchive = $existingConfigArchive
                $deployingNewConfig = $false
                [System.IO.File]::Delete($configArchive)
            }

            if (![string]::IsNullOrEmpty($existingConfigArchive) -and $deployingNewConfig) {
                $fileName = Split-Path $existingConfigArchive -Leaf

                & Rename-Item -Path $existingConfigArchive -NewName ($fileName -replace ".zip", ".mfdlabs-config-archive")
                $existingConfigArchive = ("$existingConfigArchive" -replace ".zip", ".mfdlabs-config-archive")
            }
        }
    }


    if ($deployingNewSource) {
        $newSourceArchive = $sourceArchive;

        if ($replaceExtensions) {
            $newSourceArchive = "$($archivePrefixName)-$($config).mfdlabs-archive"
            & Write-Host "Renaming $($sourceArchive) to $($newSourceArchive)" -ForegroundColor Green
            & Rename-Item -Path $sourceArchive -NewName $newSourceArchive
        }

        $deploymentFiles.Add($newSourceArchive) > $null
    }

    if ($deployingNewConfig) {
        $newConfigArchive = $configArchive;

        if ($replaceExtensions) {
            $newConfigArchive = "$($archivePrefixName)-$($config)Config.mfdlabs-config-archive"
            & Write-Host "Renaming $($configArchive) to $($newConfigArchive)" -ForegroundColor Green
            & Rename-Item -Path $configArchive -NewName $newConfigArchive
        }

        $deploymentFiles.Add($newConfigArchive) > $null
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

        $outputDir = $newSourceArchive.Replace(".zip", "").Replace(".mfdlabs-archive", "").Replace("$($deploymentYear)", "").Replace('\', '');
        [string] $outSourceArchive = "$outputDir.zip";

        if ($replaceExtensions) {
            $outSourceArchive = "$outputDir.mfdlabs-archive"
        }

        $unpackerPs1ScriptContents = $defaultPs1ScriptToExtract.Replace("{{SOURCE_NAME}}", $outputDir).Replace("{{SOURCE_ARCHIVE}}", $outSourceArchive);

        if ($deployingNewConfig) {
            [string] $outConfigArchive = "$($outputDir)Config.zip";

            if ($replaceExtensions) {
                $outConfigArchive = "$($outputDir)Config.mfdlabs-config-archive";
            }

            $unpackerPs1ScriptContents += "`n7z x -y ""-o$outputDir"" ""$outConfigArchive"""
        }

        $unpackerPs1ScriptContents += "`nexit;";

        $ps1Name = "$($outputDir).Unpacker.ps1";
        $batName = "$($outputDir).Unpacker.bat";

        # Write the unpacker script
        [System.IO.File]::WriteAllText("$($deploymentYear)\$($ps1Name)", $unpackerPs1ScriptContents);

        & Write-Host "Wrote unpacker script $($deploymentYear)\$($ps1Name)" -ForegroundColor Green

        $deploymentFiles.Add("$($deploymentYear)\$($ps1Name)") > $null

        $unpackerWrapperContents = $defaultBatchScriptPs1Wrapper.Replace("{{PS1_SCRIPT}}", ".\$($ps1Name)");

        # Write the unpacker batch wrapper
        [System.IO.File]::WriteAllText("$($deploymentYear)\$($batName)", $unpackerWrapperContents);

        & Write-Host "Wrote unpacker batch wrapper $($deploymentYear)\$($batName)" -ForegroundColor Green

        $deploymentFiles.Add("$($deploymentYear)\$($batName)") > $null
    }

    if ($isGitIntegrated -and $writeNewRelease -and ![string]::IsNullOrEmpty($env:GITHUB_TOKEN) -and ![string]::IsNullOrEmpty($remoteName)) {

        & Write-Host "Writing new release..." -ForegroundColor Green

        [string] $name = $null;

        if ($null -ne $releasePrefix -and "" -ne $releasePrefix) {
            $name = "$($releasePrefix.ToLowerInvariant())_$($versioningTag)";
        }

        try {
            PublishGitRelease -from $root -tag $versioningTag -name $name -branch $(ReadGitBranch -from $root) -remoteName $remoteName -files $deploymentFiles.ToArray() -preRelease $preRelease -allowPreReleaseGridDeployment $allowPreReleaseGridDeployment
        }
        catch {}
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

    # If the deployment directory is empty, delete it
    if ($null -ne $deploymentYear) {
        if ([System.IO.Directory]::Exists($deploymentYear)) {
            if ([System.IO.Directory]::GetFiles($deploymentYear).Count -eq 0) {
                # If it's parent directory only contains this directory, delete it
                if ([System.IO.Directory]::GetDirectories($deploymentFolder).Count -eq 1) {
                    & Write-Host "Deleting deployment folder $($deploymentYear) as it's empty." -ForegroundColor Yellow
                    [System.IO.Directory]::Delete($deploymentFolder, $true)
                }
            }
        }
    }
}