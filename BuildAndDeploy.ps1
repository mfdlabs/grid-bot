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
    [bool]$restoreSolution = $true,
    [bool]$cleanObjAndBinFolders = $false
)

& .\Build.ps1 -root $root -solutionName $solutionName -buildKind $deploymentKind -buildConfig $buildConfig -restoreSolution $restoreSolution -cleanObjAndBinFolders $cleanObjAndBinFolders
& .\Deploy.ps1 -root $root -deploymentKind $deploymentKind -config $deploymentConfig -targetFramework $targetFramework -isGitIntegrated $isGitIntegrated -replaceExtensions $replaceExtensions -checkForExistingSourceArchive $checkForExistingSourceArchive -checkForExistingConfigArchive $checkForExistingConfigArchive -writeUnpackerScripts $writeUnpackerScripts