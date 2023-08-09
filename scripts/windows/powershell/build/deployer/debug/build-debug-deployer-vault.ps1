Set-Location $PSScriptRoot

../../../build.ps1 -root $([System.IO.Path]::GetFullPath("../../../../../services/auto-deployer")) -solutionName auto-deployer -buildKind Grid.AutoDeployer -restoreSolution $true -cleanObjAndBinFolders $false -buildConfig "debug-vault" -buildConcurrently $true