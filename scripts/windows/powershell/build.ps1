param (
    [string]$root,
    [string]$solutionName,
    [string]$buildKind,
    [string]$buildConfig,
    [bool]$restoreSolution = $true,
    [bool]$cleanObjAndBinFolders = $false,
    [bool]$buildConcurrently = $false
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

$dotNetLocation = $(Get-Command -Name dotnet -ErrorAction SilentlyContinue).Source

IF (!(Test-Path $dotNetLocation)) {
    & Write-Host "The dotnet location does not exist." -ForegroundColor Red
    Exit
}

Set-Alias msbuild $msbuildLocation
Set-Alias dotnet $dotNetLocation

if ($cleanObjAndBinFolders) {
    & .\clean.ps1;
}

$target = $buildKind.Replace(".", "_");

if ($restoreSolution) {
    & Write-Host "Restoring solution..." -ForegroundColor Green
    & dotnet restore $fqnSolutionName
}

& Write-Host "Building solution..." -ForegroundColor Green


if ($buildConcurrently) {
    # Use 2 less processes than the current machine's processor count (the machine's core count)
    [int] $numberOfProcessesToUseInParallel = $env:NUMBER_OF_PROCESSORS - 2;
    & msbuild $fqnSolutionName "/t:$target" /p:Configuration="$buildConfig" /m:$numberOfProcessesToUseInParallel /p:BuildInParallel=true
} else {
    & msbuild $fqnSolutionName "/t:$target" /p:Configuration="$buildConfig"
}