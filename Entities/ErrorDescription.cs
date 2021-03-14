namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Klasse welche eine Fehlerbeschreibung darstellt
    /// </summary>
    public class ErrorDescription
    {
        #region Id
        /// <summary>
        /// Die Id von der Fehlerbeschreibung
        /// </summary>
        /// <value></value>
        public int Id { get; set; }
        #endregion

        #region Description
        /// <summary>
        /// Die Beschreibung vom Fehler
        /// </summary>
        /// <value></value>
        public int Description { get; set; }
        #endregion
    }
}