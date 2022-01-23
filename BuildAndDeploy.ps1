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
    [bool]$restoreSolution = $true
)

IF ([string]::IsNullOrEmpty($buildConfig))
{
    $buildConfig = "Debug";
}

IF ([string]::IsNullOrEmpty($deploymentConfig))
{
    $deploymentConfig = $buildConfig;
}

IF ([string]::IsNullOrEmpty($root)) {
    & Write-Host "The root directory cannot be empty." -ForegroundColor Red
    Exit
}

IF (!$root.EndsWith("\")) {
    $root = $root + "\"
}

IF ([string]::IsNullOrEmpty($deploymentKind))
{
    & Write-Host "The deployment kind cannot be empty." -ForegroundColor Red
    Exit
}

if (!(Test-Path $root)) {
    & Write-Host "The root directory does not exist." -ForegroundColor Red
    Exit
}

IF ([string]::IsNullOrEmpty($solutionName)) {
    & Write-Host "The solution name cannot be empty." -ForegroundColor Red
    Exit
}

$fqnSolutionName = $root + $solutionName + ".sln"

IF (!(Test-Path $fqnSolutionName)) {
    & Write-Host "The solution does not exist." -ForegroundColor Red
    Exit
}

$msbuildLocation = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe;

IF (!(Test-Path $msbuildLocation)) {
    & Write-Host "The MSBuild location does not exist." -ForegroundColor Red
    Exit
}

$dotNetLocation = "${env:ProgramFiles}\dotnet\dotnet.exe"

IF (!(Test-Path $dotNetLocation)) {
    & Write-Host "The dotnet location does not exist." -ForegroundColor Red
    Exit
}

Set-Alias build $msbuildLocation
Set-Alias net $dotNetLocation

$target = $deploymentKind.Replace(".", "_");

if ($restoreSolution)
{
    & Write-Host "Restoring solution..." -ForegroundColor Green
    & net restore $fqnSolutionName
}

& Write-Host "Building solution..." -ForegroundColor Green

& build $fqnSolutionName "/t:$target" /p:Configuration="$buildConfig"

& .\Deploy.ps1 -root $root -deploymentKind $deploymentKind -config $deploymentConfig -targetFramework $targetFramework -isGitIntegrated $isGitIntegrated -replaceExtensions $replaceExtensions -checkForExistingSourceArchive $checkForExistingSourceArchive -checkForExistingConfigArchive $checkForExistingConfigArchive