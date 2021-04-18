CREATE TABLE `DataValues` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT COMMENT 'Die Id vom Datensatz.',
  `ValueType` tinyint(3) unsigned NOT NULL COMMENT 'Der Typ vom Heizungswert.',
  `Value` float DEFAULT NULL COMMENT 'Der Wert von der Heizung.',
  `Timestamp` datetime NOT NULL COMMENT 'Der Zeitstempel vom Datenpunkt.',
  PRIMARY KEY (`Id`),
  KEY `DataValues_ValueType_IDX` (`ValueType`) USING BTREE,
  KEY `DataValues_Timestamp_IDX` (`Timestamp`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=1451071 DEFAULT CHARSET=utf8mb4 COMMENT='Tabelle mit den Datenwerten, welche von der Heizung empfangen und gespeichert wurden.'

CREATE TABLE `ErrorList` (
  `Id` int(10) unsigned NOT NULL COMMENT 'Die Id vom Error.',
  `Description` char(100) DEFAULT NULL COMMENT 'Die Beschreibung vom Fehler, welche von der Heizung kommt.',
  UNIQUE KEY `ErrorList_Id_IDX` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='In dieser Tabelle werden die Fehlerbeschreibungen, welche von der Heizung kommen, gespeichert.'

CREATE TABLE `HeatingStates` (
  `Id` int(11) NOT NULL COMMENT 'Die Id vom Heizungsstatus.',
  `Description` varchar(30) NOT NULL COMMENT 'Die Beschreibung vom Heizstatus.',
  UNIQUE KEY `HeatingStates_Id_IDX` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Tabelle mit den Beschreibungen der Status welche die Heizung annehmen kann.'

CREATE TABLE `NotifierConfig` (
  `Id` int(11) DEFAULT NULL COMMENT 'Die Id von der Konfiguration.',
  `LowerThreshold` double NOT NULL COMMENT 'Wenn diese Grenze unterschritten wird, werden die User benachrichtigt.',
  UNIQUE KEY `NotifierConfig_Id_IDX` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='In dieser Tabelle werden die Konfigurationen für die Benachrichtigung der User per Mail konfiguriert.'

CREATE TABLE `NotifierMails` (
  `Id` int(11) NOT NULL COMMENT 'Die Id von der Mailkonfiguration.',
  `Mail` varchar(100) NOT NULL COMMENT 'Die Emailadresse vom User.',
  UNIQUE KEY `NotifierMails_Id_IDX` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='In dieser Tabelle werden die Emailadresse gepsichert, welche vom Server benachrichtigt werden sollen.'

CREATE TABLE `ValueDescription` (
  `Id` int(11) NOT NULL COMMENT 'Die Id von der Typ-Beschreibung.',
  `Description` varchar(100) NOT NULL COMMENT 'Beschreibt, was der Heizungstyp darstellt.',
  `Unit` varchar(50) DEFAULT NULL COMMENT 'Die Einheit von Wert. Ist Null, wenn der Wert keine Einheit hat.',
  `IsLogged` bit(1) NOT NULL COMMENT 'Gibt an, ob dieser Wert in der Datenbank periodisch gespeichert werden soll.',
  UNIQUE KEY `ValueDescription_Id_IDX` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='In dieser Tabelle werden die verschiedenen Typen von Wert, welche von der Heizung empfangen werden können, beschrieben. Hier wird auch festgelegt, welche davon in der Datenbank gepsichert werden.'