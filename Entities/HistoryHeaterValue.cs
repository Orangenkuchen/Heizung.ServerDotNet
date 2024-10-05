using System;

namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Ein historischer Wert welcher aus der Heizung ausgelesen wurde
    /// </summary>
    public class HistoryHeaterValue : HeaterValue
    {
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="name">Der Name vom Wert</param>
        /// <param name="value">Der Wert</param>
        /// <param name="unit">Die Einheit vom Wert</param>
        /// <param name="timestamp">Der Zeitpunkt an dem die Heizungswert aufgezeichnet wurde</param>
        public HistoryHeaterValue(string name, string value, string unit, DateTime timestamp)
            : base(name, value, unit)
        {
            this.Timestamp = timestamp;
        }

        /// <summary>
        /// Der Zeitpunkt an dem die Heizungswert aufgezeichnet wurde
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}