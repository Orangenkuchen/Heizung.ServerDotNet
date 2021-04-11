namespace Heizung.ServerDotNet.Entities
{
    using System;

    /// <summary>
    /// Ein Datenpunkt von einem Heizwert, welcher an den Client gesendet wird
    /// </summary>
    public class HeaterDataPoint
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="value">Der Wert vom Datenpunkt</param>
        public HeaterDataPoint(object value)
        {
            this.Value = value;
        }
        #endregion

        #region TimeStamp
        /// <summary>
        /// Der Timestamp vom Datenpunkt
        /// </summary>
        /// <value></value>
        public DateTime TimeStamp { get; set; }
        #endregion

        #region Value
        /// <summary>
        /// Der Wert vom Datenpunkt (string oder double)
        /// </summary>
        /// <value></value>
        public object Value { get; set; }
        #endregion
    }
}