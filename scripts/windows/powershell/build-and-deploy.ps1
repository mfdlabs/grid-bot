param (
    [string]$root,
    [string]$solutionName,
    [string]$deploymentKind,
    [string]$buildConfig,
    [string]$deploymentConfig,
    [string]$targetFramework = "net472",
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
    [bool]$restoreSolution = $true,
    [bool]$cleanObjAndBinFolders = $false
)

& .\build.ps1 -root $root -solutionName $solutionName -buildKind $deploymentKind -buildConfig $buildConfig -restoreSolution $restoreSolution -cleanObjAndBinFolders $cleanObjAndBinFolders
& .\deploy.ps1 -root $root -deploymentKind $deploymentKind -config $deploymentConfig -targetFramework $targetFramework -isGitIntegrated $isGitIntegrated -replaceExtensions $replaceExtensions -checkForExistingSourceArchive $checkForExistingSourceArchive -checkForExistingConfigArchive $checkForExistingConfigArchive -writeUnpackerScripts $writeUnpackerScripts -writeNewRelease $writeNewRelease -preRelease $preRelease -allowPreReleaseGridDeployment $allowPreReleaseGridDeployment -githubToken $githubToken -remoteName $remoteName -releasePrefix $releasePrefix