Set-Location $PSScriptRoot

../../../build.ps1 -root $([System.IO.Path]::GetFullPath("../../../../..")) -solutionName Services -buildKind MFDLabs.Grid.AutoDeployer -restoreSolution $true -cleanObjAndBinFolders $false -buildConfig "ReleaseGridDeploy" -buildConcurrently $true