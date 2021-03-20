namespace Heizung.ServerDotNet.Entities
{
    using System.Collections.Generic;

    /// <summary>
    /// Klasse für die Konfiguration der Mailbenachrichtigungen
    /// </summary>
    public class NotifierConfig
    {
        #region LowerThreshold
        /// <summary>
        /// Die Untergrenze vom Puffer ab wann die Mailbenarichtigungen gesendet werden
        /// </summary>
        /// <value></value>
        public double LowerThreshold { get; set; }
        #endregion

        #region MailConfigs
        /// <summary>
        /// Die Adresse, welche benarichtigt werden sollen
        /// </summary>
        /// <value></value>
        public IList<MailConfig> MailConfigs { get; set; }
        #endregion
    }
}