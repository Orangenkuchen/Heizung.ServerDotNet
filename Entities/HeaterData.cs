namespace Heizung.ServerDotNet.Entities
{
    using System.Collections.Generic;

    /// <summary>
    /// Klasse für einen Datenpunkt welcher an den Client geschickt wird.
    /// </summary>
    public class HeaterData
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="description">Die Beschreibung der Heizungsdaten</param>
        /// <param name="unit"></param>
        public HeaterData(string description, string unit)
        {
            this.Description = description;
            this.Unit = unit;
            this.Data = new List<HeaterDataPoint>();
        }
        #endregion

        #region ValueTypeId
        /// <summary>
        /// Die Id vom Heizwerttyp
        /// </summary>
        /// <value></value>
        public int ValueTypeId { get; set; }
        #endregion

        #region Description
        /// <summary>
        /// Die Beschreibung vom Heizwert
        /// </summary>
        /// <value></value>
        public string Description { get; set; }
        #endregion

        #region IsLogged
        /// <summary>
        /// Gibt an ob der Heizwert gelogged wird
        /// </summary>
        /// <value></value>
        public bool IsLogged { get; set; }
        #endregion

        #region Unit
        /// <summary>
        /// Die Einheit des Werts
        /// </summary>
        /// <value></value>
        public string Unit { get; set; }
        #endregion

        #region Data
        /// <summary>
        /// Die Daten für den Heizwert
        /// </summary>
        /// <value></value>
        public IList<HeaterDataPoint> Data { get; set; }
        #endregion
    }
}