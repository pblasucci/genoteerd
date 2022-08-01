# configure script execution (error handling, et cetera)
Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

# handles calling an external process and failing on non-successful exit code
function Exec{
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = $true)]
        [scriptblock] $cmd,

        [Parameter(Position = 1, Mandatory = $false)]
        [string]$errorMessage = ("Error executing command {0}" -f $cmd)
    )
    & $cmd
    if ($LastExitCode -ne 0){
        throw ("Exec: " + $errorMessage)
    }
}

# binds arbitrary data into arbitrary string templates
function Invoke-Template
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ScriptBlock] $scriptBlock
    )

    function Format-Template
    {
        param([string] $template)
        Invoke-Expression "@`"`r`n$template`r`n`"@"
    }
    & $scriptBlock
}

# Commonly-used, well-known values
$FsProj = './genoteerd/genoteerd.fsproj'
$ExecName = 'genoteerd'
$IconPath = './genoteerd.png'
$DesktopTemp = './template.desktop'
$DesktopFile = 'com.mulberrylabs.genoteerd.desktop'
$OutFolder = './out'
$ExecFolder = '/home/pblasucci/.local/share/genoteerd'
$InfoFolder = '/home/pblasucci/.local/share/applications'

# remove contents of out dir
if (Test-Path $OutFolder) { Remove-Item $OutFolder -Recurse -Force -Verbose }

# publish application
Exec { dotnet publish -c 'Release' -o $OutFolder $FsProj }

# move application to final destination
Copy-Item "$OutFolder/$ExecName" -Destination $ExecFolder -Verbose

# move icon to final destination
Copy-Item $IconPath -Destination $ExecFolder -Verbose

# update .desktop file
Invoke-Template {
    $Template = Get-Content -Raw -Path $DesktopTemp
    # extract version from project file
    $Version =
        Select-Xml -Path $FsProj -XPath './/*/VersionPrefix' `
        | ForEach-Object { $_.Node.InnerXml }
    # bind data to template and write to final destination
    Format-Template $Template `
    | Set-Content -Path "$InfoFolder/$DesktopFile" `
    | Write-Verbose
}
