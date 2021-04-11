namespace Heizung.ServerDotNet.Entities
{
    using System;

    /// <summary>
    /// Interface für eine Öffnung der Tür
    /// </summary>
    public class DoorOpening
    {
        #region StartDateTime
        /// <summary>
        /// Der Zeitpunkt an dem die Tür geöffnet wurde
        /// </summary>
        /// <value></value>
        public DateTime StartDateTime { get; set; }
        #endregion

        #region EndDateTime
        /// <summary>
        /// Der Zeitpunkt an dem die Tür geschlossen wurde
        /// </summary>
        /// <value></value>
        public DateTime? EndDateTime { get; set; }
        #endregion
    }
}