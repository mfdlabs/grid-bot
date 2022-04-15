function CheckHash([string]$newLocation, [string]$existingLocation) {
    & Write-Host "Checking hash for $newLocation" -ForegroundColor Green

    if ([string]::IsNullOrEmpty($existingLocation)) {
        & Write-Host "Existing location is null or empty" -ForegroundColor Yellow
        return $false;
    }

    # determine if the contents of the currently deploying source archive is the same as the last deployed source archive
    $existingSourceArchiveHash = GetArchiveHash -File $existingLocation
    $sourceArchiveHash = GetArchiveHash -File $newLocation

    & Write-Host "New location: $newLocation" -ForegroundColor Green
    & Write-Host "Existing location: $existingLocation" -ForegroundColor Green
    & Write-Host "Existing Archive Hash: $existingSourceArchiveHash" -ForegroundColor Green
    & Write-Host "New Archive Hash: $sourceArchiveHash" -ForegroundColor Green
    & Write-Host "Comparing existing archive hash to new archive hash..." -ForegroundColor Green
    & Write-Host "Existing Archive Hash == New Archive Hash: $(IF($existingSourceArchiveHash -eq $sourceArchiveHash){"Yes"} ELSE {"No"})" -ForegroundColor Green

    return $existingSourceArchiveHash -eq $sourceArchiveHash
}
