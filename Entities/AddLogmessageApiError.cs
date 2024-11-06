namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Dieser Fehler tritt auf, wenn der Client versucht eine Lognachricht zu Loggen, welche
    /// niedriger ist als der minimale Log-Level
    /// </summary>
    public class AddLogmessageApiError
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        public AddLogmessageApiError()
        {
            this.Message = string.Empty;
        }
        #endregion

        #region Message
        /// <summary>
        /// Die Fehlermeldung
        /// </summary>
        /// <value></value>
        public string Message { get; set; }
        #endregion

        #region MinimumLogLevel
        /// <summary>
        /// Der Minimumloglevel, welcher von der Api angenommen wird
        /// </summary>
        /// <value></value>
        public ClientLogLevel MinimumLogLevel { get;set; }
        #endregion
    }
}