# Information on deployments:
# release and debug refer to vanilla builds.
# release-deploy and debug-deploy refer to vanilla builds, but we are creating a component archive for the application.
# release-vault and debug-vault refer to builds with Hashicorp Vault configuration providers.
# release-vault-deploy and debug-vault-deploy refer to builds with Hashicorp Vault configuration providers, but we are creating a component archive for the application.

param (
    [string] $configurationName,
    [string] $projectDir,
    [string] $outDir,
    [string] $targetName,
    [string] $targetExt,
    [string] $solutionDir,
    [string] $projectName,
    [string] $targetFramework,
    [string] $deployScriptOverride,
    [bool] $deleteDebugSymbols = $true,
	[bool] $writeNewGitHubRelease = $true,
    [string] $releasePrefix = $null
)

# Write-Host "$($MyInvocation.MyCommand.Name) $boundArgsStr" -ForegroundColor Green
Write-Host "Post build invoked with Configuration '$configurationName' at Target '$targetName'" -ForegroundColor Green;

$isDebug = $configurationName.StartsWith("debug");
$isVaultBacked = $configurationName.Contains("-vault");
$isDeployment = $configurationName.Contains("-deploy");
$appSettingsConfiguration = IF ($isDebug) { "debug" } else { "release" };

if ($isVaultBacked) {
	Write-Host "Build is vault-backed, appending vault configuration postfix." -ForegroundColor Green;
	
    $appSettingsConfiguration += "-vault";
}

# Copy the current configuration settings to the target
$projectConfigurationFile = Join-Path $projectDir "App.$AppSettingsConfiguration.config"
# Check if it exists
if (Test-Path $projectConfigurationFile) {
    $outputConfigurationName = "$($projectDir)$($outDir)$($targetName)$($targetExt).config"
    Copy-Item -Path $projectConfigurationFile -Destination $outputConfigurationName -Force
}

# Delete old runtime-scripts and copy new runtime-scripts
$runtimeScriptsOutDir = [System.IO.Path]::Combine($projectDir, $outDir, "runtime-scripts");

IF (Test-Path $runtimeScriptsOutDir) {
    Remove-Item -Path $runtimeScriptsOutDir -Recurse -Force
}

# Copy the runtime-scripts
$runtimeScriptsDir = Join-Path $projectDir "runtime-scripts";
IF (Test-Path $runtimeScriptsDir) {
    Copy-Item -Path $runtimeScriptsDir -Destination $runtimeScriptsOutDir -Recurse -Force
}

# Delete the old Lua scripts and copy the new Lua scripts
$luaScriptsOutDir = [System.IO.Path]::Combine($projectDir, $outDir, "lua");

IF (Test-Path $luaScriptsOutDir) {
    Remove-Item -Path $luaScriptsOutDir -Recurse -Force
}

# Copy the Lua scripts
$luaScriptsDir = Join-Path $projectDir "lua";
IF (Test-Path $luaScriptsDir) {
    Copy-Item -Path $luaScriptsDir -Destination $luaScriptsOutDir -Recurse -Force
}

Write-Host "Finished pre-cleanup, should deploy: $(IF ($isDeployment) { "true" } ELSE { "false" })" -ForegroundColor Green;

IF ($isDeployment) {
    $deploymentConfiguration = IF ($isDebug) { "debug" } ELSE { "release" };

    Write-Host "Deployment configuration: $deploymentConfiguration" -ForegroundColor Green;

    # Determine if we are deploying a debug or release build, if release delete all pdb files
    if (!$isDebug -and $deleteDebugSymbols) {
        Write-Host "Removing pdb files" -ForegroundColor Green;
        $outPath = "$($projectDir)$($outDir)";

        & Write-Host "Removing pdb files in $outPath" -ForegroundColor Green;

        Remove-Item -Path "$($outPath)*.pdb" -Recurse -Force
    }

    # Call the deploy script
    Write-Host "Deploying..." -ForegroundColor Green;
    $scriptName = IF ($deployScriptOverride) { $deployScriptOverride } ELSE { Join-Path -Path $solutionDir -ChildPath "..\..\scripts\windows\powershell\deploy.ps1" };
    if (!(Get-Item -Path $scriptName)) {
        Write-Host "Deploy script $scriptName does not exist" -ForegroundColor Red;
        Exit 1;
    }

    # Invoke the deploy script
    & "$($scriptName)" -root "$solutionDir/src" -config $deploymentConfiguration -targetFramework $targetFramework -deploymentKind $projectName -checkForExistingConfigArchive $false -writeNewRelease $writeNewGitHubRelease -preRelease $isDebug -releasePrefix $releasePrefix -gitRepositoryPath "$solutionDir/../.."
}
