Set-Location $PSScriptRoot

../../../build.ps1 -root $([System.IO.Path]::GetFullPath("../../../../../services/grid-bot")) -solutionName grid-bot -buildKind MFDLabs.Grid.Bot -restoreSolution $true -cleanObjAndBinFolders $false -buildConfig "Debug" -buildConcurrently $true