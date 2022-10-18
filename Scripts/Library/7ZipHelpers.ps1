$7zipPath = "C:\Program Files\7-Zip\7z.exe"

if (-not (Test-Path -Path $7zipPath -PathType Leaf)) {
    & Write-Host "7Zip not found on the system." -ForegroundColor Red
    Exit
}

& Set-Alias 7zip-Archive $7zipPath

function GetArchiveHash([string] $File) {
    if (!(Test-Path -Path $File -PathType Leaf)) {
        return $null
    }

    $fileInfo = Get-ItemProperty -Path $File

    $archivePath = "$env:temp\$($fileInfo.Name)\"

    try {

        if (![System.IO.Directory]::Exists($archivePath)) {
            & Write-Host "Creating temp directory $archivePath" -ForegroundColor Green
            [System.IO.Directory]::CreateDirectory($archivePath) *> $null;
        }

        7zip-Archive x $File "-o$archivePath" -y *> $null

        $data = 7zip-Archive h $archivePath* | FindStr "data:";

        return ($data -split ":")[1].Trim();
    }
    finally {
        & Write-Host "Removing temp directory $archivePath" -ForegroundColor Green
        [System.IO.Directory]::Delete($archivePath, $true)
    }
}
