namespace Heizung.ServerDotNet.Entities
{
    using System;

    /// <summary>
    /// Beinhaltet die Betriebstunden der Heizung vom Tag
    /// </summary>
    public class DayOperatingHoures
    {
        #region Date
        /// <summary>
        /// Der Tag von dem die Betriebsstunden stammen
        /// </summary>
        /// <value></value>
        public DateTime Date { get; set; }
        #endregion
        
        #region Houres
        /// <summary>
        /// Die Anzahl der Stunden, wie lang die Heizung an diesem Tag gebrannt hat
        /// </summary>
        /// <value></value>
        public uint Houres { get; set; }
        #endregion

        #region MinHoures
        /// <summary>
        /// Die Anzahl der Betriebstunden am Anfang des Tages
        /// </summary>
        /// <value></value>
        public uint MinHoures { get; set; }
        #endregion

        #region MaxHoures
        /// <summary>
        /// Die Anzahl der Betriebstunden am Ende des Tages
        /// </summary>
        /// <value></value>
        public uint MaxHoures { get; set; }
        #endregion
    }
}