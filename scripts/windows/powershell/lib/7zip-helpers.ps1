$7zipPath = $(Get-Command -Name 7z -ErrorAction SilentlyContinue).Source

if (-not (Test-Path -Path $7zipPath -PathType Leaf)) {
    & Write-Host "7Zip not found on the system." -ForegroundColor Red
    Exit
}

& Set-Alias 7z $7zipPath

function Get-ArchiveHash([string] $File) {
    if (!(Test-Path -Path $File -PathType Leaf)) {
        return $null
    }

    return (Get-FileHash $File).Hash
}
