Set-Location $PSScriptRoot

../../../build.ps1 -root $([System.IO.Path]::GetFullPath("../../../../../services/grid-bot")) -solutionName grid-bot -buildKind Grid.Bot -restoreSolution $true -cleanObjAndBinFolders $false -buildConfig "DebugDeploy" -buildConcurrently $true