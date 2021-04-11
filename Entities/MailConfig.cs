namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Konfiguration von einer Mailadresse welche benachrichtigt werden soll.
    /// </summary>
    public class MailConfig
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="mail">Die Mailadresse von der Config</param>
        public MailConfig(string mail)
        {
            this.Mail = mail;
        }
        #endregion

        #region Mail
        /// <summary>
        /// Die Adresse
        /// </summary>
        /// <value></value>
        public string Mail { get; set; }
        #endregion
    }
}