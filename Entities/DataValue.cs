namespace Heizung.ServerDotNet.Entities
{
    using System;

    /// <summary>
    /// Beschreibung für einen DatenWert
    /// </summary>
    public class DataValue
    {
        #region ctor
        /// <summary>
        /// Initalisiert die Klasse
        /// </summary>
        /// <param name="value">Der Wert von den Daten</param>
        public DataValue(object value)
        {
            this.Value = value;
        }
        #endregion

        #region Id
        /// <summary>
        /// Die Id vom Wert
        /// </summary>
        /// <value></value>
        public int Id { get; set; }
        #endregion

        #region ValueType
        /// <summary>
        /// Die Id von dem DatenWert-Typ
        /// </summary>
        /// <value></value>
        public int ValueType { get; set; }
        #endregion

        #region Value
        /// <summary>
        /// Der Wert (kann String oder int sein)
        /// </summary>
        /// <value></value>
        public object Value { get; set; }
        #endregion

        #region TimeStamp
        /// <summary>
        /// Der Zeitpunkt zu dem der Wert aufgenommen wurde
        /// </summary>
        /// <value></value>
        public DateTime TimeStamp { get; set; }
        #endregion
    }
}