namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Ein Wert welcher aus der Heizung ausgelesen wurde
    /// </summary>
    public class HeaterValue
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="name">Der Name vom Wert</param>
        /// <param name="value">Der Wert</param>
        /// <param name="unit">Die Einheit vom Wert</param>
        public HeaterValue(string name, string value, string unit)
        {
            this.Name = name;
            this.Value = value;
            this.Unit = unit;
        }
        #endregion

        #region Name
        /// <summary>
        /// Der Name vom gelesenen Wert
        /// </summary>
        /// <value></value>
        public string Name { get; set; }
        #endregion

        #region Value
        /// <summary>
        /// Der Wert, welcher ermittelt wurde
        /// </summary>
        /// <value></value>
        public string Value { get; set; }
        #endregion

        #region Index
        /// <summary>
        /// Der Index vom Wert
        /// </summary>
        /// <value></value>
        public int Index { get; set; }
        #endregion

        #region Multiplicator
        /// <summary>
        /// Der Messwert wurde mit diesem Wert multipliziert um auf <see cref="Value"/> zu kommen
        /// </summary>
        /// <value></value>
        public double Multiplicator { get; set; }
        #endregion

        #region Unit
        /// <summary>
        /// Die Einheit des Werts
        /// </summary>
        /// <value></value>
        public string Unit { get; set; }
        #endregion
    }
}