[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]
    $SshConnectionString,

    [Parameter(Mandatory=$true)]
    [string]
    $LogFilesFolderPath
)

scp "$($SshConnectionString):$LogFilesFolderPath/*" ./Logs;
scp "$($SshConnectionString):/home/dominik/.config/Heizung.ServerDotNet/*" ./Logs;