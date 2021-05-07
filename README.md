# Heizung.ServerDotNet

## Übersicht

Dieses Projekt dient als Serveranwendung für das Speichern und Bereitstellen von Heizungsdaten (Fröhling S3 Turbo).
Außerdem kann die Anwendung Warnungs-Mails versenden, wenn die Temperatur einen Grenzwert unterschritten hat oder ein Fehler in der Heizung aufgetreten ist.

## Datenspeicherung

Der Server kann über die API Heizungsdaten empfangen und diese zwischenspeichern. Diese Daten werden dann an Clients von der WebAPI weitergegeben. Außerdem werden alle 15 Minuten die zischengespeicherten Daten in der Datenbank gespeichert.

## WebApi

Die Software stellt unter der Adresse http://<Entpunkt>/swagger/index.html eine Hilfeseite (swagger) bereit, welche die Funktionalität von dieser erklärt.

## Mailbenachrichtigung

Die Software kann Mail versenden in folgenden Fällen:

1. Der Wert vom Boiler oben ist unter einen Grenzwert gefallen (einstellbar über WebApi)
2. Die Heizung hat einen Fehler gemeldet (einstellbar über WebApi)

## Logging

Die Anwendung loggt in den folgenden Pfaden:
* Windows > C:\Documents and Settings\\{User}\Application Data\Heizung.ServerDotNet\Log.txt
* Linux > /home/{User}/.config/Heizung.ServerDotNet/Log.txt

Der Level vom Logging kann über die WebApi geändert werden.

## Einrichtung der Software

Die Einstellungsdateien appsettings.json und appsettings.Development.json werden benötigt damit die Anwendung funktioniert. Die Datei "appsettings.template.json" stellt eine Vorlage dar, wie die anderen Dateien gefüllt werden müssen.

Beim starten der Anwendung werden die benötigten Tabellen in der Datenbank angelegt, wenn noch nicht vorhanden.

Die Anwendung ist kompatibel mit Linux- und Windows-Services und kann von diesen bedient werden (siehe Kommandozeilen-Parameter "--service-install-help").