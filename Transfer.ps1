$ErrorActionPreference = 'Stop';

$DebugPreference = 'Continue';
$VerbosePreference = 'Continue';

Write-Host "Skript gestartet...";

Write-Debug "Stelle das Projekt wieder her...";
dotnet restore;
Write-Verbose "Projekt wieder hergestellt.";

Write-Debug "Publishe das Projekt...";
dotnet publish -c Release --runtime linux-arm;
Write-Verbose "Publish abgeschlossen";

Write-Verbose "Lege den Ordner für Transfer an..."
New-Item -ItemType Directory -Path "Heizungs-Server" -Force;
Copy-Item -Path "./bin/Release/net6.0/linux-arm/publish/*" -Recurse -Force -Destination "./Heizungs-Server";
Write-Verbose "Oder für Transfer angelegt";

Write-Debug "Entferne die Konfig-Dateien vor der übertragung...";
Remove-Item "./Heizungs-Server/appsettings.json";
Remove-Item "./Heizungs-Server/appsettings.Development.json";
Remove-Item "./Heizungs-Server/appsettings.template.json";
Write-Verbose "Konfig-Dateien entfernt.";

Write-Debug "Stoppe den Heizungs-Server-Service";
ssh dominik@Raspberry-Pi-3-b "sudo /bin/systemctl stop heizungs-server.service";
Write-Verbose "Service wurde gestoppt";

Write-Debug "Übertragen auf den Zielrechner...";
scp -r ./Heizungs-Server dominik@Raspberry-Pi-3-b:~/;
Write-Verbose "Übertragung abgeschlossen";

Write-Verbose "Lösche den Transfer-Ordner lokal...";
Remove-Item -Path "./Heizungs-Server" -Recurse -Force;
Write-Verbose "Ordner wurde gelöscht";

Write-Debug "Ändere die Rechte von Heizungs-Server...";
ssh dominik@Raspberry-Pi-3-b "chmod u+x ./Heizungs-Server/Heizung.ServerDotNet";
Write-Verbose "Abgeschlossen";

Write-Debug "Start den Heizungs-Server-Service";
ssh dominik@Raspberry-Pi-3-b "sudo /bin/systemctl start heizungs-server.service";
Write-Verbose "Service wurde gestartet";

Write-Host "Skript abgeschlossen";