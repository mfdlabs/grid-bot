# PowerShell script that recursively deletes all 'bin' and 'obj' (or any other specified) folders inside current folder

$CurrentPath = (Get-Location -PSProvider FileSystem).ProviderPath

# recursively get all folders matching given includes, except ignored folders
$FoldersToRemove = Get-ChildItem .\ -include bin,obj -Recurse   | Where-Object {$_ -notmatch '_tools' -and $_ -notmatch '_build'} | ForEach-Object {$_.fullname}

# recursively get all folders matching given includes
$AllFolders = Get-ChildItem .\ -include bin,obj -Recurse | ForEach-Object {$_.fullname}

# subtract arrays to calculate ignored ones
$IgnoredFolders = $AllFolders | Where-Object {$FoldersToRemove -notcontains $_} 

# remove folders and print to output
if($null -ne $FoldersToRemove)
{			
    Write-Host 
	foreach ($item in $FoldersToRemove) 
	{ 
		remove-item $item -Force -Recurse;
		Write-Host "Removed: ." -nonewline; 
		Write-Host $item.replace($CurrentPath, ""); 
	} 
}

# print ignored folders	to output
if($null -ne $IgnoredFolders)
{
    Write-Host 
	foreach ($item in $IgnoredFolders) 
	{ 
		Write-Host "Ignored: ." -nonewline; 
		Write-Host $item.replace($CurrentPath, ""); 
	} 
	
	Write-Host 
	Write-Host $IgnoredFolders.count "folders ignored" -foregroundcolor yellow
}

# print summary of the operation
Write-Host 
if($null -ne $FoldersToRemove)
{
	Write-Host $FoldersToRemove.count "folders removed" -foregroundcolor green
}
else { 	Write-Host "No folders to remove" -foregroundcolor green }	

Write-Host 
