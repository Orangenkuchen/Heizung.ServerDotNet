namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Klasse welche einen HeizungswertTyp darstellt
    /// </summary>
    public class ValueDescription
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        public ValueDescription()
        {
            this.Description = "";
            this.Unit = null;
        }

        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="description">Die Berschreibung vom Wert</param>
        /// <param name="unit">Die Einheit vom Wert</param>
        public ValueDescription(string description, string unit)
        {
            this.Description = description;
            this.Unit = unit;
        }
        #endregion

        #region Id
        /// <summary>
        /// Die Nummer/Id welche die ValueDescription hat
        /// </summary>
        /// <value></value>
        public int Id { get; set; }
        #endregion

        #region Description
        /// <summary>
        /// Die Beschreibung von der ValueDescription
        /// </summary>
        /// <value></value>
        public string Description { get; set; }
        #endregion

        #region IsLogged
        /// <summary>
        /// Gibt an, ob der Wert in der Datenbank periodisch gespeichert wird
        /// </summary>
        /// <value>True = wird gelogged</value>
        public bool IsLogged { get; set; }
        #endregion

        #region Unit
        /// <summary>
        /// Die Einheit vom ValueDescription
        /// </summary>
        /// <value></value>
        public string? Unit { get; set; }
        #endregion
    }
}