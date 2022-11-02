# OnPostBuildInvoked

# Information on deployments:
# Release and Debug refer to vanilla builds.
# ReleaseDeploy and DebugDeploy refer to vanilla builds, but we are creating a component archive for the application.
# ReleaseGrid and DebugGrid refer to builds with Hashicorp Vault configuration providers.
# ReleaseGridDeploy and DebugGridDeploy refer to builds with Hashicorp Vault configuration providers, but we are creating a component archive for the application.

param (
    [string] $ConfigurationName,
    [string] $ProjectDir,
    [string] $OutDir,
    [string] $TargetName,
    [string] $TargetExt,
    [string] $SolutionDir,
    [string] $ProjectName,
    [string] $TargetFramework,
    [string] $DeployScriptOverride,
    [bool] $DeleteDebugSymbols = $true,
	[bool] $WriteNewGitHubRelease = $true,
    [string] $ReleasePrefix = $null,
    [bool] $IsUsingSystemConfiguration = $true
)

# $boundArgs = $MyInvocation.BoundParameters.Keys;
# $boundArgsStr = "";

# foreach ($key in $boundArgs) {
#     $value = $(get-variable $key).Value;

#     $boundArgsStr += "-$key $value ";
# }

# Write-Host "$($MyInvocation.MyCommand.Name) $boundArgsStr" -ForegroundColor Green
Write-Host "OnPostBuildInvoked with Configuration '$ConfigurationName' at Target '$TargetName'" -ForegroundColor Green;

$isDebug = $ConfigurationName.StartsWith("Debug");
$isVaultBacked = $ConfigurationName.Contains("Grid");
$isDeployment = $ConfigurationName.Contains("Deploy");
$appSettingsConfiguration = IF ($isDebug) { "Debug" } else { "Release" };

if ($isVaultBacked) {
	Write-Host "Build is vault-backed, appending vault configuration postfix." -ForegroundColor Green;
	
    $appSettingsConfiguration += "-Vault";
}

# Copy the current configuration settings to the target
$projectConfigurationFile = "$($ProjectDir)App.$AppSettingsConfiguration.config";
# Check if it exists
if (Test-Path $projectConfigurationFile) {
    $outputConfigurationName = "$($ProjectDir)$($OutDir)$($TargetName)$($TargetExt).config"
    Copy-Item -Path $projectConfigurationFile -Destination $outputConfigurationName -Force
}

# Delete old runtime-scripts and copy new runtime-scripts
$runtimeScriptsDir = "$($ProjectDir)$($OutDir)RuntimeScripts";

IF (Test-Path $runtimeScriptsDir) {
    Remove-Item -Path $runtimeScriptsDir -Recurse -Force
}

# Copy the runtime-scripts
IF (Test-Path "$($ProjectDir)RuntimeScripts") {
    Copy-Item -Path "$($ProjectDir)RuntimeScripts" -Destination $runtimeScriptsDir -Recurse -Force
}

# Delete the old Lua scripts and copy the new Lua scripts
$luaScriptsDir = "$($ProjectDir)$($OutDir)Lua";

IF (Test-Path $luaScriptsDir) {
    Remove-Item -Path $luaScriptsDir -Recurse -Force
}

# Copy the Lua scripts
IF (Test-Path "$($ProjectDir)Lua") {
    Copy-Item -Path "$($ProjectDir)Lua" -Destination $luaScriptsDir -Recurse -Force
}

# Delete all translation scripts at the output
$translationScriptsDirectories = @(
    "$($ProjectDir)$($OutDir)cs\",
    "$($ProjectDir)$($OutDir)de\",
    "$($ProjectDir)$($OutDir)es\",
    "$($ProjectDir)$($OutDir)fr\",
    "$($ProjectDir)$($OutDir)it\",
    "$($ProjectDir)$($OutDir)ja\",
    "$($ProjectDir)$($OutDir)ko\",
    "$($ProjectDir)$($OutDir)pl\",
    "$($ProjectDir)$($OutDir)pt-BR\",
    "$($ProjectDir)$($OutDir)ru\",
    "$($ProjectDir)$($OutDir)tr\",
    "$($ProjectDir)$($OutDir)zh-Hans\",
    "$($ProjectDir)$($OutDir)zh-Hant\"
);

foreach ($translationScriptsDirectory in $translationScriptsDirectories) {
    if (Test-Path $translationScriptsDirectory) {
        Remove-Item -Path $translationScriptsDirectory -Recurse -Force
    }
}

Write-Host "Finished pre-cleanup, should deploy: $(IF ($isDeployment) { "true" } ELSE { "false" })" -ForegroundColor Green;

IF ($isDeployment) {
    $deploymentConfiguration = IF ($isDebug) { "Debug" } ELSE { "Release" };

    Write-Host "Deployment configuration: $deploymentConfiguration" -ForegroundColor Green;

    # Determine if we are deploying a debug or release build, if release delete all pdb files
    if (!$isDebug -and $DeleteDebugSymbols) {
        Write-Host "Removing pdb files" -ForegroundColor Green;
        $outPath = "$($ProjectDir)$($OutDir)";

        & Write-Host "Removing pdb files in $outPath" -ForegroundColor Green;

        Remove-Item -Path "$($outPath)*.pdb" -Recurse -Force
    }

    # Call the deploy script
    Write-Host "Deploying..." -ForegroundColor Green;
    $scriptName = IF ($DeployScriptOverride) { $DeployScriptOverride } ELSE { "$($SolutionDir)Scripts\Deploy.ps1" };
    if (!(Get-Item -Path $scriptName)) {
        Write-Host "Deploy script $scriptName does not exist" -ForegroundColor Red;
        Exit 1;
    }

    # Invoke the deploy script
    & "$($scriptName)" -root $SolutionDir -config $deploymentConfiguration -targetFramework $TargetFramework -deploymentKind $ProjectName -checkForExistingConfigArchive $false -writeNewRelease $WriteNewGitHubRelease -preRelease $isDebug -releasePrefix $ReleasePrefix
}