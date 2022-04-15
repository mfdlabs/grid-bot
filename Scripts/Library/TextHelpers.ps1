function GetParsedNumber([int] $num) {
    return [string]::new("0", 2 - $num.ToString().Length) + $num.ToString()
}

function GetRandomHexString([int] $bits = 256) {
    $bytes = new-object 'System.Byte[]' ($bits / 8)
    (new-object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
    (new-object System.Runtime.Remoting.Metadata.W3cXsd2001.SoapHexBinary @(, $bytes)).ToString()
}
