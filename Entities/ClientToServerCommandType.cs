namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Enum für die Commandtypes, welche vom Client über den Websocket gesendet werden
    /// </summary>
    public enum ClientToServerCommandType
    {
        #region RequestAllDataCommand
        /// <summary>
        /// Command zum ermitteln von allen Daten (in einem Zeitraum)
        /// </summary>
        RequestAllDataCommand = 1,
        #endregion
        
        #region SetLoggingStateCommand
        /// <summary>
        /// Command zum Einstellen davon, welche Werte geloggt werden sollen
        /// </summary>
        SetLoggingStateCommand = 2,
        #endregion

        #region RequestMailNotifierConfigCommand
        /// <summary>
        /// Command zum Anfordern der Konfiguration von der Mail-Benachrichtigung
        /// </summary>
        RequestMailNotifierConfigCommand = 3,
        #endregion

        #region SaveMailNotifierConfigCommand
        /// <summary>
        /// Command zum Speichern von der Mail-Benachrichtigung-Konfig
        /// </summary>
        SaveMailNotifierConfigCommand = 4,
        #endregion

        #region RequestNewestDataCommand
        /// <summary>
        /// Command zum ermitteln vder neusten Daten
        /// </summary>
        RequestNewestDataCommand = 5
        #endregion
    }
}