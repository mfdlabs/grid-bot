param (
    [string]$root,
    [string]$solutionName,
    [string]$buildKind,
    [string]$buildConfig,
    [bool]$restoreSolution = $true,
    [bool]$cleanObjAndBinFolders = $false
)

IF ([string]::IsNullOrEmpty($buildConfig)) {
    $buildConfig = "Debug";
}

IF ([string]::IsNullOrEmpty($root)) {
    $root = (Get-Location).Path;
}

IF (!$root.EndsWith("\")) {
    $root = $root + "\"
}

IF ([string]::IsNullOrEmpty($buildKind)) {
    & Write-Host "The build kind cannot be empty." -ForegroundColor Red
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

if ($cleanObjAndBinFolders) {
    & .\DeleteObjBinFolders.ps1;
}

$target = $buildKind.Replace(".", "_");

if ($restoreSolution) {
    & Write-Host "Restoring solution..." -ForegroundColor Green
    & net restore $fqnSolutionName
}

& Write-Host "Building solution..." -ForegroundColor Green

& build $fqnSolutionName "/t:$target" /p:Configuration="$buildConfig"