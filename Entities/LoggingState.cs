namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Klasse, welche den Logstatus von einem Heizungswert darstellt
    /// </summary>
    public class LoggingState
    {
        #region ValueTypeId
        /// <summary>
        /// Die Id vom WerteTyp, für den die Klasse steht
        /// </summary>
        /// <value></value>
        public int ValueTypeId { get; set; }
        #endregion

        #region IsLoged
        /// <summary>
        /// Gibt an, ob der Wert für den diese Klasse steht in der Historie gespeichert werden soll
        /// </summary>
        /// <value></value>
        public bool IsLoged { get; set; }
        #endregion
    }
}