
function HasGitRepository([string] $path) {
    if ([string]::IsNullOrEmpty($path)) {
        throw "`$path is null or empty";
    }

    $gitDir = [System.IO.Path]::Combine($path, ".git");
    $exists = [System.IO.Directory]::Exists($gitDir);

    Write-Host "Git repository exists: $exists. At: $gitDir";
	
    return $exists;
}

function ReadGitBranch([string] $from) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    $oldDir = Get-Location;

    Set-Location $from -ErrorAction SilentlyContinue > $null;

    $branch = Invoke-Expression -Command "git rev-parse --abbrev-ref HEAD";

    Set-Location $oldDir -ErrorAction SilentlyContinue > $null;

    if ([string]::IsNullOrEmpty($branch)) {
        return $null
    }

    if ($branch.StartsWith("fatal")) {
        return $null
    }

    return $branch;
}

function ReadGitHash([string] $from, [bool] $readShortHash = $false) {
    if ([string]::IsNullOrEmpty($from)) {
        throw $null;
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    $gitHash = "";

    $oldDir = Get-Location;

    Set-Location $from -ErrorAction SilentlyContinue > $null;

    if ($readShortHash) {
        $gitHash = (Invoke-Expression -Command "git rev-parse --short HEAD")
    }
    else {
        $gitHash = (Invoke-Expression -Command "git rev-parse HEAD")
    }

    Set-Location $oldDir -ErrorAction SilentlyContinue > $null;

    # Trim and then remove the git hash spaces via regex
    $gitHash = $gitHash.Trim().Replace(" ", "");

    if ([string]::IsNullOrEmpty($gitHash)) {
        return $null
    }

    if ($gitHash.StartsWith("fatal")) {
        return $null
    }

    return $gitHash;
}

<#
Attempts to read the short hash revision from the directory for github revisions
#>
function ReadGitShortHash([string] $from) {
    $hash = $(ReadGitHash -from $from -readShortHash $true);
    $hash = if ($null -ne $hash) { $hash.Trim().Replace(" ", "") -replace '\t', '' } else { $null }
    return $hash;
}

function ReadGitRemoteUrl([string] $from, [string] $remoteName) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ([string]::IsNullOrEmpty($remoteName)) {
        throw "`$remoteName is null or empty";
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    $oldDir = Get-Location;

    Set-Location $from -ErrorAction SilentlyContinue > $null;

    # Get the remote url
    $remoteUrl = Invoke-Expression "git config --get remote.$remoteName.url"

    Set-Location $oldDir -ErrorAction SilentlyContinue > $null;

    if ([string]::IsNullOrEmpty($remoteUrl)) {
        return $null;
    }

    # Determine if it's ssh or https
    if ($remoteUrl.StartsWith("https://")) {
        $remoteUrl = $remoteUrl.Replace("https://", "")
    }

    if ($remoteUrl.StartsWith("ssh://")) {
        $remoteUrl = $remoteUrl.Replace("ssh://", "")
    }
    
    if ($remoteUrl.StartsWith("git@")) {
        $remoteUrl = $remoteUrl.Replace("git@", "")
    }

    if ($remoteUrl.EndsWith(".git")) {
        $remoteUrl = $remoteUrl.Replace(".git", "")
    }

    return $remoteUrl
}

function ReadGitRepoOwner([string] $from, [string] $remoteName) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    # Use the git remote to find the owner of the repo
    $gitRemote = ReadGitRemoteUrl -from $from -remoteName $remoteName;

    if ([string]::IsNullOrEmpty($gitRemote)) {
        return $null
    }

    # It will be in the format of {hostName}/{owner}/{repoName}
    $gitRemoteParts = $gitRemote.Split("/")

    if ($gitRemoteParts.Length -lt 3) {
        return $null
    }

    return $gitRemoteParts[1]
}

function ReadGitRepoName([string] $from, [string] $remoteName) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    # Use the git remote to find the owner of the repo
    $gitRemote = ReadGitRemoteUrl -from $from -remoteName $remoteName;

    if ([string]::IsNullOrEmpty($gitRemote)) {
        return $null
    }

    # It will be in the format of {hostName}/{owner}/{repoName}
    $gitRemoteParts = $gitRemote.Split("/")

    if ($gitRemoteParts.Length -lt 3) {
        return $null
    }

    return $gitRemoteParts[2]
}

function PublishGitRelease([string] $from, [string] $tag, [string] $name, [string] $branch, [string] $remoteName, [string[]] $files, [bool] $preRelease = $false, [bool] $allowPreReleaseGridDeployment = $false, [string] $gitBaseUrl = "api.github.com") {
    # If the environment variable GITHUB_TOKEN is not set, then we can't publish, just warn and return
    if ($null -eq $env:GITHUB_TOKEN) {
        Write-Host "GITHUB_TOKEN environment variable is not set, skipping publish" -ForegroundColor Yellow
        return
    }

    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(HasGitRepository($from))) {
        return $null;
    }

    if ([string]::IsNullOrEmpty($tag)) {
        throw "`$tag is null or empty";
    }

    if ([string]::IsNullOrEmpty($branch)) {
        throw "`$branch is null or empty";
    }

    if ([string]::IsNullOrEmpty($remoteName)) {
        throw "`$remoteName is null or empty";
    }

    if ($files.Length -eq 0) {
        throw "`$files is an empty array";
    }

    # Get the remote info, like owner and repo name
    $owner = ReadGitRepoOwner -from $from -remoteName $remoteName;
    $repoName = ReadGitRepoName -from $from -remoteName $remoteName;

    Write-Host "Publishing release for $tag to $owner/$repoName" -ForegroundColor Green

    if ([string]::IsNullOrEmpty($owner)) {
        return;
    }

    if ([string]::IsNullOrEmpty($repoName)) {
        return;
    }

    # Set up the payload
    

    # Step 1 is to determine if the release already exists
    # Step 2 is to create the release as a draft
    # Step 3 is to upload the files
    # Step 4 is to update the release to be a non-draft

    # Step 1
    $url = "https://$gitBaseUrl/repos/$owner/$repoName/releases/tags/$tag"
    $response = try {
        Invoke-WebRequest -Uri $url -Method Get -Headers @{
            "Authorization" = "token $env:GITHUB_TOKEN"
        } -ErrorAction SilentlyContinue
    }
    catch [System.Net.WebException] {
        $_.Exception.Response
    }

    if ($response.StatusCode -eq 200) {

        Write-Host "Release $tag already exists, skipping publish" -ForegroundColor Yellow
        return
    }

    # determine if it was unauthorized
    if ($response.StatusCode -eq 401) {
        Write-Host "Unauthorized, skipping publish" -ForegroundColor Yellow
        return
    }

    # Step 2

    $url = "https://$gitBaseUrl/repos/$owner/$repoName/releases"

    $name = if ($null -ne $name -and "" -ne $name) { $name } else { $tag }

    $payload = @{
        "tag_name"               = $tag
        "target_commitish"       = $branch
        "name"                   = IF ($preRelease -eq $true -and $allowPreReleaseGridDeployment -eq $true) { "$name [DEPLOY]" } ELSE { $name }
        "draft"                  = $true
        "prerelease"             = $preRelease
        "generate_release_notes" = $true
    }

    $jsonEncodedPayload = ConvertTo-Json -InputObject $payload
    
    Write-Host "Payload: $jsonEncodedPayload" -ForegroundColor Yellow

    $response = try {
        Invoke-WebRequest -Uri $url -Method POST -Body $jsonEncodedPayload -Headers @{
            "Authorization" = "token $env:GITHUB_TOKEN"
            "Accept"        = "application/vnd.github.v3+json"
            "Content-Type"  = "application/json"
        }
    }
    catch [System.Net.WebException] {
        $_.Exception.Response
    }

    if ($response.StatusCode -ne 201) {
        Write-Host "Failed to create release because $($response.StatusCode), skipping publish" -ForegroundColor Yellow
        return
    }

    # Response is json, so parse it
    $release = ConvertFrom-Json -InputObject $response.Content

    $releaseId = $release.id

    # Step 3
    $uploadUrl = $release.upload_url
    $uploadUrl = $uploadUrl.Replace("{?name,label}", "")

    foreach ($file in $files) {
        # use system.io to split the filename from the path
        $fileName = Split-Path $file -leaf

        if (!(Test-Path $file)) {
            Write-Output "File $file does not exist, skipping"
            continue
        }

        Write-Host "Uploading $fileName to $uploadUrl" -ForegroundColor Green

        $url = $uploadUrl + "?name=$fileName"

        $bytes = [System.IO.File]::ReadAllBytes($file)

        Invoke-RestMethod -Method PUT -Uri $url -Headers @{
            "Authorization" = "token $env:GITHUB_TOKEN"
            "Accept"        = "application/vnd.github.v3+json"
            "Content-Type"  = "application/octet-stream"
        } -Body $bytes
    }

    # Step 4
    $url = "https://$gitBaseUrl/repos/$owner/$repoName/releases/$releaseId"
    $payload = @{
        "draft" = $false
    }

    $jsonEncodedPayload = ConvertTo-Json -InputObject $payload
    $response = try {
        Invoke-WebRequest -Uri $url -Method PATCH -Body $jsonEncodedPayload -Headers @{
            "Authorization" = "token $env:GITHUB_TOKEN"
            "Accept"        = "application/vnd.github.v3+json"
            "Content-Type"  = "application/json"
        } 
    }
    catch [System.Net.WebException] {
        $_.Exception.Response
    }

    if ($response.StatusCode -ne 200) {
        Write-Output "Failed to update release to non-draft, because $($response.Content), skipping publish"
        return
    }

    Write-Output "Successfully published release $tag"
}
