function Test-GitRepository([string] $path) {
    if ([string]::IsNullOrEmpty($path)) {
        throw "`$path is null or empty";
    }

    $gitDir = [System.IO.Path]::Combine($path, ".git");
    $exists = [System.IO.Directory]::Exists($gitDir);

    Write-Host "Git repository exists: $exists. At: $gitDir";
	
    return $exists;
}

function Get-GitBranch([string] $from) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(Test-GitRepository($from))) {
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

function Get-GitHash([string] $from, [bool] $readShortHash = $false) {
    if ([string]::IsNullOrEmpty($from)) {
        throw $null;
    }

    if ($false -eq $(Test-GitRepository($from))) {
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

function Get-GitShortHash([string] $from) {
    $hash = $(Get-GitHash -from $from -readShortHash $true);
    $hash = if ($null -ne $hash) { $hash.Trim().Replace(" ", "") -replace '\t', '' } else { $null }
    return $hash;
}

function Get-GitRemoteUrl([string] $from, [string] $remoteName) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ([string]::IsNullOrEmpty($remoteName)) {
        throw "`$remoteName is null or empty";
    }

    if ($false -eq $(Test-GitRepository($from))) {
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
		$remoteUrl = $remoteUrl.Replace(":", "/")
    }
    
    if ($remoteUrl.StartsWith("git@")) {
        $remoteUrl = $remoteUrl.Replace("git@", "")
		$remoteUrl = $remoteUrl.Replace(":", "/")
    }

    if ($remoteUrl.EndsWith(".git")) {
        $remoteUrl = $remoteUrl.Replace(".git", "")
    }

    return $remoteUrl
}

function Get-GitRepositoryOwner([string] $from, [string] $remoteName) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(Test-GitRepository($from))) {
        return $null;
    }

    # Use the git remote to find the owner of the repo
    $gitRemote = Get-GitRemoteUrl -from $from -remoteName $remoteName;

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

function Get-GitRepositoryName([string] $from, [string] $remoteName) {
    if ([string]::IsNullOrEmpty($from)) {
        throw "`$from is null or empty";
    }

    if ($false -eq $(Test-GitRepository($from))) {
        return $null;
    }

    # Use the git remote to find the owner of the repo
    $gitRemote = Get-GitRemoteUrl -from $from -remoteName $remoteName;

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
