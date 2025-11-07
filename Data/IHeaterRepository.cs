namespace Heizung.ServerDotNet.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
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
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <returns>Gibt ein Promise für die DatenBeschreibungen zurück</returns>
        /// <exception type="exception">Wird geworfen, wenn keine Verbindung mit der Datenbank hergestellt werden kann</exception>
        Task<IList<ValueDescription>> GetAllValueDescriptions(CancellationToken cancellationToken);
        #endregion

        #region GetDataValues
        /// <summary>
        /// Ermittelt alle DatenWerte innherhalb der Zeit aus der Datenbank
        /// </summary>
        /// <param name="fromDate">Der Zeitpunkt, von dem an die Daten ermittelt werden sollen</param>
        /// <param name="toDate">Der Zeitpunkt, bis zu dem die Daten ermittelt werden sollen</param>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <returns>Gibt die Daten zurück</returns>
        Task<IList<DataValue>> GetDataValues(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);
        #endregion

        #region GetLatestDataValues
        /// <summary>
        /// Ermittelt die DatenWerte neusten Datenwerte (für jeden DatenTyp) aus der Datenbank
        /// </summary>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <returns>Gibt die Daten zurück</returns>
        Task<IDictionary<string, DataValue>> GetLatestDataValues(CancellationToken cancellationToken);
        #endregion

        #region SetLoggingStateOfVaueType
        /// <summary>
        /// Stellt ein, welche Heizungswerte in der Historie gespeichert werden sollen
        /// </summary>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <param name="loggingStates">Die Einstellung welche gesetzt werden soll</param>
        Task<bool> SetLoggingStateOfVaueType(IList<LoggingState> loggingStates, CancellationToken cancellationToken);
        #endregion

        #region GetMailNotifierConfig
        /// <summary>
        /// Ermittelt die NotifierConfig aus der Datenbank
        /// </summary>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <returns>Git die NotifierConfig zurück</returns>
        Task<NotifierConfig> GetMailNotifierConfig(CancellationToken cancellationToken);
        #endregion

        #region SetMailNotifierConfig
        /// <summary>
        /// Ermittelt die NotifierConfig aus der Datenbank
        /// </summary>
        /// <param name="notifierConfig">Die Konfiguration, welche gespeichert werden soll</param>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        Task<bool> SetMailNotifierConfig(NotifierConfig notifierConfig, CancellationToken cancellationToken);
        #endregion

        #region GetAllErrorDictionary
        /// <summary>
        /// Ermittelt eine Dictionary mit allen Fehlern
        /// </summary>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Fehler als Dictionary zurück</returns>
        Task<IDictionary<int, ErrorDescription>> GetAllErrorDictionary(CancellationToken cancellationToken);
        #endregion

        #region GetOperatingHoures
        /// <summary>
        /// Ermittelt eine Liste von täglichen Betriebsstunden im angegeben Zeitraum
        /// </summary>
        /// <param name="from">Von welchem Datum an geholt werden soll. (Nur Datum wird beachtet nicht Uhrzeit)</param>
        /// <param name="to">Bis zu welchem Zeitpunkt geholt werden soll. (Nur Datum wird beachtet nicht Uhrzeit)</param>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <returns>Gibt die Liste der ermittelten Einträge zurück</returns>
        Task<IList<DayOperatingHoures>> GetOperatingHoures(DateTime from, DateTime to, CancellationToken cancellationToken);
        #endregion

        #region SetNewError
        /// <summary>
        /// Erzeugt einen neuen Eintrag in der FehlerTabelle
        /// </summary>
        /// <param name="errorText">Der Fehlertext von der neuen Fehlermeldung</param>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Id von dem neuen Eintrag zurück</returns>
        Task<int> SetNewError(string errorText, CancellationToken cancellationToken);
        #endregion

        #region SetHeaterValue
        /// <summary>
        /// Fügt neue Heizwerte in die Tabelle hinzu
        /// </summary>
        /// <param name="heaterDataList">List von Werten, welche gespeichert werden sollen</param>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Id von dem neuen Eintrag zurück</returns>
        public Task<bool> SetHeaterValue(IEnumerable<HeaterData> heaterDataList, CancellationToken cancellationToken);
        #endregion
    }
}