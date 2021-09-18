param ([string]$root, [string]$config, [bool]$isService=$false)

function Get-RandomHex {
    param(
        [int] $Bits = 256
    )
    $bytes = new-object 'System.Byte[]' ($Bits / 8)
    (new-object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
    (new-object System.Runtime.Remoting.Metadata.W3cXsd2001.SoapHexBinary @(,$bytes)).ToString()
}

function Get-Parsed-Number {
    param (
        [int] $num
    )
    
    $str = [string]::new("0", 2 - $num.ToString().Length) + $num.ToString();

    return $str
}

try {

    IF ([string]::IsNullOrEmpty($root)) {
        & Write-Host "The root directory cannot be empty." -ForegroundColor Red
        Exit
    }

    IF ([string]::IsNullOrEmpty($config)) {
        & Write-Host "The config was invalid." -ForegroundColor Red
        Exit
    }



    IF (($config.ToLower() -ne "release") -and ($config.ToLower() -ne "debug")) {
        & Write-Host "The config was invalid." -ForegroundColor Red
        Exit
    }


    [String] $hash = Get-RandomHex -Bits 128
    $date = Get-Date
    $branch = "master"
    $location = $root
    $deploymentKind = IF ($isService) {"MFDLabs.Grid.Bot.Service"} ELSE {"MFDLabs.Grid.Bot"}
    $deploymentFolder = "$($location)$($deploymentKind)/Deploy/"
    $deploymentYear = "$($deploymentFolder)$($date.Year)/"

    Write-Host "Trying to deploy at date $($date)" -ForegroundColor Green

    IF (![System.IO.Directory]::Exists($deploymentFolder)){
        & Write-Host "Deployment folder not found, creating." -foregroundcolor yellow
        [System.IO.Directory]::CreateDirectory($deploymentFolder)
    }

    IF (![System.IO.Directory]::Exists($deploymentYear)) {
        & Write-Host "Deployment bin folder not found, creating." -foregroundcolor yellow
        [System.IO.Directory]::CreateDirectory($deploymentYear)
    }

    IF (![System.IO.Directory]::Exists("$($location)$($deploymentKind)/bin/$($config)/")) {
        & Write-Host "The output folder to read the deployment was not found, aborting." -foregroundcolor yellow
        Exit
    }

    $file = "$($deploymentYear)$($date.Year).$(Get-Parsed-Number -num $date.Month).$(Get-Parsed-Number -num $date.Day)-$(Get-Parsed-Number -num $date.Hour).$(Get-Parsed-Number -num $date.Minute).$(Get-Parsed-Number -num $date.Second)_$($branch)_$($hash.ToLower().Substring(0, 9))-$($config).zip"

    & Write-Host "Deploying $($location)$($deploymentKind)/bin/$($config)/* to $($file)" -ForegroundColor Green

    $lastdeploy = Get-ChildItem "$($deploymentYear)" | Sort-Object LastWriteTime | Select-Object -Last 1

    & Compress-Archive -Path "$($location)$($deploymentKind)/bin/$($config)/*" -CompressionLevel Fastest -DestinationPath $file -Verbose -Force

    $newFile = $file.Replace(".zip", ".mfdlabs-archive")

    IF ($null -ne $lastdeploy) {
        $md5 = (Get-FileHash -Path $file).Hash
        $oldMd5 = (Get-FileHash -Path $lastdeploy.FullName).Hash

        if ($oldMd5 -eq $md5) {
            & Remove-Item -Path $file
            & Write-Host "Found duplicate deployment, ignoring. Please refer to the deployment $($lastdeploy.Fullname)."-foregroundcolor yellow
            Exit
        }
    }


    & Rename-Item -Path $file -NewName $newFile
    & Write-Host "Completed deployment of ""$($newFile)"", press enter to continue." -ForegroundColor Green
} catch [Exception] {
    & Write-Host "An error occurred when trying to deploy, please try again later. Error: $($_)"
    Exit
}