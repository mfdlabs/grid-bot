Set-Location $PSScriptRoot

../../../build.ps1 -root $([System.IO.Path]::GetFullPath("../../../../..")) -solutionName MFDLabs.Grid -buildKind MFDLabs.Grid.Bot -restoreSolution $true -cleanObjAndBinFolders $false -buildConfig "ReleaseGrid" -buildConcurrently $true