namespace Heizung.ServerDotNet.Mail
{
    using System.Net;

    /// <summary>
    /// Klasse mit Zugangsdaten zu einer Email
    /// </summary>
    public class MailConfiguration
    {
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