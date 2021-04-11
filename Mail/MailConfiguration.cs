namespace Heizung.ServerDotNet.Mail
{
    using System.Net;

    /// <summary>
    /// Klasse mit Zugangsdaten zu einer Email
    /// </summary>
    public class MailConfiguration
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="smtpServer">Die Serveradresse von der Mail</param>
        /// <param name="smtpServerCredential">Die Zugangsdaten zum Mail-Konto</param>
        public MailConfiguration(string smtpServer, NetworkCredential smtpServerCredential)
        {
            this.SmtpServer = smtpServer;
            this.SmtpServerCredential = smtpServerCredential;
        }
        #endregion

        #region SmtpServer
        /// <summary>
        /// Die Serveradresse von der Mail
        /// </summary>
        /// <value></value>
        public string SmtpServer { get; set; }
        #endregion

        #region SmtpServerPort
        /// <summary>
        /// Der Serverport von der Mail
        /// </summary>
        /// <value></value>
        public uint SmtpServerPort { get; set; }
        #endregion

        #region SmtpServerCredential
        /// <summary>
        /// Die Zugangsdaten zum Mail-Konto
        /// </summary>
        /// <value></value>
        public NetworkCredential SmtpServerCredential { get; set; }
        #endregion
    }
}