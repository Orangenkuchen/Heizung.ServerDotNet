namespace Heizung.ServerDotNet.Data
{
    using System;
    using System.Collections.Generic;
    using Heizung.ServerDotNet.Entities;

    /// <summary>
    /// DataRepository für Datenbankanfragen bezüglich der Heizung
    /// </summary>
    public interface IHeaterRepository
    {
        #region GetAllValueDescriptions
        /// <summary>
        /// Ermittelt alle DatenWert-Bescheibungen aus der Datenbank
        /// </summary>
        /// <returns>Gibt ein Promise für die DatenBeschreibungen zurück</returns>
        /// <exception type="exception">Wird geworfen, wenn keine Verbindung mit der Datenbank hergestellt werden kann</exception>
        IList<ValueDescription> GetAllValueDescriptions();
        #endregion

        #region GetDataValues
        /// <summary>
        /// Ermittelt alle DatenWerte innherhalb der Zeit aus der Datenbank
        /// </summary>
        /// <param name="fromDate">Der Zeitpunkt, von dem an die Daten ermittelt werden sollen</param>
        /// <param name="toDate">Der Zeitpunkt, bis zu dem die Daten ermittelt werden sollen</param>
        /// <returns>Gibt die Daten zurück</returns>
        IList<DataValue> GetDataValues(DateTime fromDate, DateTime toDate);
        #endregion

        #region GetLatestDataValues
        /// <summary>
        /// Ermittelt die DatenWerte neusten Datenwerte (für jeden DatenTyp) aus der Datenbank
        /// </summary>
        /// <returns>Gibt die Daten zurück</returns>
        IDictionary<string, DataValue> GetLatestDataValues();
        #endregion

        #region SetLoggingStateOfVaueType
        /// <summary>
        /// Stellt ein, welche Heizungswerte in der Historie gespeichert werden sollen
        /// </summary>
        /// <param name="loggingStates">Die Einstellung welche gesetzt werden soll</param>
        void SetLoggingStateOfVaueType(IList<LoggingState> loggingStates);
        #endregion

        #region GetMailNotifierConfig
        /// <summary>
        /// Ermittelt die NotifierConfig aus der Datenbank
        /// </summary>
        /// <returns>Git die NotifierConfig zurück</returns>
        NotifierConfig GetMailNotifierConfig();
        #endregion

        #region SetMailNotifierConfig
        /// <summary>
        /// Ermittelt die NotifierConfig aus der Datenbank
        /// </summary>
        /// <param name="notifierConfig">Die Konfiguration, welche gespeichert werden soll</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        void SetMailNotifierConfig(NotifierConfig notifierConfig);
        #endregion

        #region GetAllErrorDictionary
        /// <summary>
        /// Ermittelt eine Dictionary mit allen Fehlern
        /// </summary>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Fehler als Dictionary zurück</returns>
        IDictionary<int, ErrorDescription> GetAllErrorDictionary();
        #endregion

        #region GetOperatingHoures
        /// <summary>
        /// Ermittelt eine Liste von täglichen Betriebsstunden im angegeben Zeitraum
        /// </summary>
        /// <param name="from">Von welchem Datum an geholt werden soll. (Nur Datum wird beachtet nicht Uhrzeit)</param>
        /// <param name="to">Bis zu welchem Zeitpunkt geholt werden soll. (Nur Datum wird beachtet nicht Uhrzeit)</param>
        /// <returns>Gibt die Liste der ermittelten Einträge zurück</returns>
        IList<DayOperatingHoures> GetOperatingHoures(DateTime from, DateTime to);
        #endregion

        #region SetNewError
        /// <summary>
        /// Erzeugt einen neuen Eintrag in der FehlerTabelle
        /// </summary>
        /// <param name="errorText">Der Fehlertext von der neuen Fehlermeldung</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Id von dem neuen Eintrag zurück</returns>
        int SetNewError(string errorText);
        #endregion

        #region SetHeaterValue
        /// <summary>
        /// Fügt neue Heizwerte in die Tabelle hinzu
        /// </summary>
        /// <param name="heaterDataDictonary">Dictionary mit den Werten, welche hinzugefügt werden sollen</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Id von dem neuen Eintrag zurück</returns>
        void SetHeaterValue(IDictionary<int, HeaterData> heaterDataDictonary);
        #endregion
    }
}