# Globals.
$APIKEY = "YOUR_API_KEY"
$BASEURL = "https://infinibattle.infi.nl"
$CONFIGURATION = "Release"

Function Build-Project {
    Param ([string]$configuration)

    $ps = new-object System.Diagnostics.Process
    $ps.StartInfo.Filename = "dotnet"
    $ps.StartInfo.Arguments = "build --configuration $($configuration)"
    $ps.StartInfo.WorkingDirectory = $PSScriptRoot
    $ps.StartInfo.RedirectStandardOutput = $True
    $ps.StartInfo.UseShellExecute = $false
    $ps.start() | Out-Null 
    $ps.WaitForExit()

    return [string]$ps.StandardOutput.ReadToEnd()
}

Function Get-Build-Directory {
    Param ([string]$configuration)

    $binDirectory = "$($PSScriptRoot)\bin\$($configuration)"
    $buildDirectory = Get-ChildItem -Path $binDirectory -Filter *.dll -Force -Recurse -File | % { $_[0].FullName } | Split-Path

    return $buildDirectory
}

Function Create-Zip {
    Param ([string]$source, [string]$target)

    # Remove old zip file when exists.
    if(Test-Path $target)  {
        Remove-Item ($target)
    }

    # Create new zip file.
    Add-Type -Assembly System.IO.Compression.FileSystem
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory($source, $target, $compressionLevel, $false)
}

Function Upload {
    Param ([string]$baseUrl, [string]$ApiKey, [string]$zipPath)

    # Upload new zip file.
    $uploadUrl = "$($baseUrl)/api/uploadBot/$($ApiKey)"
    $response = (New-Object Net.WebClient).UploadFile($uploadUrl, $zipPath)

    # Output server response.
    return [System.Text.Encoding]::UTF8.GetString($response)
}

# Entry point.
Write-Output(Build-Project $CONFIGURATION)

$buildDirectory = Get-Build-Directory($CONFIGURATION)
$zipPath = "$($PSScriptRoot)\publish.zip"

Create-Zip $buildDirectory $zipPath

Write-Output(Upload $BASEURL $APIKEY $zipPath)