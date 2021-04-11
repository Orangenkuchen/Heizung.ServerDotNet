namespace Heizung.ServerDotNet.Entities
{    
    using System.Collections.Generic;
    using Heizung.ServerDotNet.Controllers;

    /// <summary>
    /// Optionen für die API-Action <see cref="LogController.Message" />
    /// </summary>
    public class LogMessageActionOptions
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        public LogMessageActionOptions()
        {
            this.Parameters = new List<string>();
        }
        #endregion

        #region Parameters
        /// <summary>
        /// Die Parameter von der Lognachricht
        /// </summary>
        public IList<string> Parameters { get; set; }
        #endregion

        #region Error
        /// <summary>
        /// Der Fehler, welcher beim Client aufgetreten ist
        /// </summary>
        /// <value></value>
        public object? Error { get; set; }
        #endregion
    }
}