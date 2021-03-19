namespace Heizung.ServerDotNet.Mail
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Text;
    using Heizung.ServerDotNet.Data;

    /// <summary>
    /// Klasse zum Versenden der Mails der Heizung
    /// </summary>
    public class MailManager
    {
        #region fields
        /// <summary>
        /// Smtp-Client zum versenden der Mails
        /// </summary>
        private readonly SmtpClient smtpClient;

        /// <summary>
        /// Die Zugangsdaten zum Mailkonto
        /// </summary>
        private readonly NetworkCredential networkCredential;

        /// <summary>
        /// Heizungsrepository aus dem die Daten für geholt werden, welche in den Emails verwendet werden
        /// </summary>
        private readonly HeaterRepository heaterRepository;
        #endregion

        #region ctor
        /// <summary>
        /// Initilisiert die Klasse, damit Email versendet werden können
        /// </summary>
        /// <param name="smtpServer">Die Serveradresse von der Mail, welche verwendet werden soll</param>
        /// <param name="smtpServerPort">Der Serverport von der Mail, welche verwendet werden soll</param>
        /// <param name="smtpServerCredential">Die Zugangsdaten vom Mailkonto</param>
        /// <param name="heaterRepository">Heizungsrepository aus dem die Daten für geholt werden, welche in den Emails verwendet werden</param>
        public MailManager(string smtpServer, int smtpServerPort, NetworkCredential smtpServerCredential, HeaterRepository heaterRepository)
        {
            this.smtpClient = new SmtpClient(smtpServer)
            {
                Port = smtpServerPort,
                Credentials = smtpServerCredential,
                EnableSsl = true
            };
            this.networkCredential = smtpServerCredential;

            this.heaterRepository = heaterRepository;
        }
        #endregion

        #region SendMailWhenCheckFails
        /// <summary>
        /// Überprüft die Temperaturwerte und sendet Mail, wenn die Tempratur zu niedrig ist, ein Fehler anliegt 
        /// oder keine Werte mehr empfangen werden
        /// </summary>
        public void SendMailWhenCheckFails()
        {
            var latestDataValuesDictionary = this.heaterRepository.GetLatestDataValues();
            var mailNotifierConfig = this.heaterRepository.GetMailNotifierConfig();

            IList<string> mailStrings = new List<string>();

            foreach (var mailConfig in mailNotifierConfig.MailConfigs)
            {
                mailStrings.Add(mailConfig.Mail);
            }

            if (mailStrings.Count == 0) {
                this.SendMissingValuesWaringMail(mailStrings.ToArray());
            } else {
                if (latestDataValuesDictionary.ContainsKey("99"))
                {
                    if (latestDataValuesDictionary["99"].Value != 0)
                    {
                        this.SendErrorMail(latestDataValuesDictionary["99"].Value, mailStrings.ToArray());
                    }
                }

                var upperBufferTemperature = 0;
                var lowerBufferTemperature = 0;
                var heatingStatus = 0;

                if (latestDataValuesDictionary.ContainsKey("20"))
                {
                    upperBufferTemperature = latestDataValuesDictionary["20"].Value;
                }
                if (latestDataValuesDictionary.ContainsKey("21"))
                {
                    lowerBufferTemperature = latestDataValuesDictionary["21"].Value;
                }
                if (latestDataValuesDictionary.ContainsKey("1"))
                {
                    heatingStatus = latestDataValuesDictionary["1"].Value;
                }

                // Wenn die Temperatur unterhalb der Grenze ist und in der Heizung das Feuer aus ist
                if (upperBufferTemperature < mailNotifierConfig.LowerThreshold &&
                    heatingStatus == 5)
                {
                    this.SendTemperatureLowMail(lowerBufferTemperature, upperBufferTemperature, mailNotifierConfig.LowerThreshold, mailStrings.ToArray());
                }
            }
        }
        #endregion

        #region SendMissingValuesWaringMail
        /// <summary>
        /// Sendet eine Mail an die Clients
        /// </summary>
        /// <param name="mails">Die Mailadressen an die gesendet werden soll</param>
        private void SendMissingValuesWaringMail(params string[] mails)
        {
            var text = "Es wurden seit > 2 Stunden keine Heizungsdaten mehr empfangen.";
            
            var errorMailMessage = new MailMessage()
            {
                Subject = "Keine Heizungsdaten mehr seit 2 Stunden",
                From = new MailAddress(this.networkCredential.UserName),
                Body = text
            };

            foreach (var mail in mails)
            {
                errorMailMessage.To.Add(mail);
            }
        
            this.smtpClient.Send(errorMailMessage);
        }
        #endregion

        #region SendErrorMail
        /// <summary>
        /// Sendet eine Mail an die Clients
        /// </summary>
        /// <param name="errorNumber">Der Fehlercode welcher anliegt</param>
        /// <param name="mails">Die Mailadressen an die gesendet werden soll</param>
        private void SendErrorMail(int errorNumber, params string[] mails) {
            var text = $"In der Heizung liegt ein Fehler an (Error {errorNumber})";
            
            var errorMailMessage = new MailMessage()
            {
                Subject = "In der Heizung liegt ein Fehler an",
                From = new MailAddress(this.networkCredential.UserName),
                Body = text
            };

            foreach (var mail in mails)
            {
                errorMailMessage.To.Add(mail);
            }
        
            this.smtpClient.Send(errorMailMessage);
        }
        #endregion

        #region SendTemperatureLowMail
        /// <summary>
        /// Sendet eine Mail an die Clients
        /// </summary>
        /// <param name="currentLowerBufferTemperature">Die aktuelle Temperatur vom Puffer unten</param>
        /// <param name="currentUpperBufferTemperature">Die akutelle Temperatur vom Puffer oben</param>
        /// <param name="lowerTreshold">Der Grenzwert ab dem die Mails gesendet werden soll</param>
        /// <param name="mails">Die Mailadressen an die gesendet werden soll</param>
        private void SendTemperatureLowMail(
            double currentLowerBufferTemperature, 
            double currentUpperBufferTemperature, 
            double lowerTreshold, 
            params string[] mails)
        {
            var mailTextBuilder = new StringBuilder(125);
            mailTextBuilder.AppendFormat("Die Temperatur der Heizung ist unter den Grenzwert {0}°C gefallen.{1}", lowerTreshold, Environment.NewLine);
            mailTextBuilder.Append(Environment.NewLine);
            mailTextBuilder.AppendFormat("Temperaturen:{0}", Environment.NewLine);
            mailTextBuilder.AppendFormat("Puffer oben: {0}°C{1}", currentUpperBufferTemperature, Environment.NewLine);
            mailTextBuilder.AppendFormat("Puffer unten: {0}°C{1}", currentLowerBufferTemperature, Environment.NewLine);

            var errorMailMessage = new MailMessage()
            {
                Subject = $"Heizungstemperatur unter dem Grenzwert {lowerTreshold}°C",
                From = new MailAddress(this.networkCredential.UserName),
                Body = mailTextBuilder.ToString()
            };

            foreach (var mail in mails)
            {
                errorMailMessage.To.Add(mail);
            }
        
            this.smtpClient.Send(errorMailMessage);
        }
        #endregion
    }
}


    /*public constructor(mail: string, password: string, connectionPool: Pool) {
        this.mail = mail;
        this.mailTransporter = nodemailer.createTransport({
            'host': 'smtp.gmail.com',
            'port': 587,
            'secure': false,
            'requireTLS': true,
            'auth': {
                'user': mail,
                'pass': password
            }
        });

        this.heizungsRepository = new HeizungRepository(connectionPool);
    }*/