namespace Heizung.ServerDotNet.Controllers
{
    using System;
    using Heizung.ServerDotNet.Data;
    using Heizung.ServerDotNet.Entities;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Controller für die Konfiguration der Mailbenachrichtung
    /// </summary>
    [ApiController]
    [Route("[controller]/[action]")]
    public class MailController : ControllerBase
    {
        #region fields
        /// <summary>
        /// Service für Lognachrichten
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Repository für die Historiendaten der Heizung
        /// </summary>
        private readonly IHeaterRepository heaterRepository;
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert den Controller
        /// </summary>
        /// <param name="logger">Service für Lognachrichten</param>
        /// <param name="heaterRepository">Repository für die Historiendaten der Heizung</param>
        public MailController(ILogger<HeaterDataController> logger, IHeaterRepository heaterRepository)
        {
            this.logger = logger;
            this.heaterRepository = heaterRepository;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Ermittelt die Configuration für die Mailbenarichtigung
        /// </summary>
        /// <returns>Gibt die Daten als <see cref="NotifierConfig" />></returns>
        [HttpGet]
        public ActionResult<NotifierConfig> Configuration()
        {
            return base.Ok(this.heaterRepository.GetMailNotifierConfig());
        }

        /// <summary>
        /// Setzt  die Configuration für die Mailbenarichtigung
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public ActionResult Configuration([FromBody]NotifierConfig notifierConfig)
        {
            this.heaterRepository.SetMailNotifierConfig(notifierConfig);

            return base.NoContent();
        }
        #endregion
    }
}