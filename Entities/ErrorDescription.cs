namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Klasse welche eine Fehlerbeschreibung darstellt
    /// </summary>
    public class ErrorDescription
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="description">Die Beschreibung vom Fehler</param>
        public ErrorDescription(string description)
        {
            this.Description = description;
        }
        #endregion

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
        public string Description { get; set; }
        #endregion
    }
}