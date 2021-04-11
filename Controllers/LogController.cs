namespace Heizung.ServerDotNet.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Heizung.ServerDotNet.Data;
    using Heizung.ServerDotNet.Entities;
    using Heizung.ServerDotNet.Service;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Controller für die akutellen Heizungsdaten und die Historie
    /// </summary>
    [ApiController]
    [Route("[controller]/[action]")]
    public class LogController : ControllerBase
    {
        #region fields
        /// <summary>
        /// Service für Lognachrichten
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Der LogLevel von den Clientnachrichten, welche gespeichert werden
        /// </summary>
        private readonly ClientLogLevel minimumClientLogLevel;
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert den Controller
        /// </summary>
        /// <param name="logger">Service für Lognachrichten</param>
        public LogController(ILogger<HeaterDataController> logger)
        {
            this.logger = logger;
            this.minimumClientLogLevel = ClientLogLevel.Information;
        }
        #endregion

        #region MinimumLevel GET
        /// <summary>
        /// Gibt den LogLevel aus, welcher von <see cref="Message" /> angenommen wird
        /// </summary>
        /// <returns>Gibt den Loglevel zurück, welcher angenommen werden</returns>
        [HttpGet]
        public ActionResult<ClientLogLevel> MinimumLevel()
        {
            this.logger.LogTrace("LogController: MinimumLevel called");
            return base.Ok(this.minimumClientLogLevel);
        }
        #endregion

        #region Message POST
        /// <summary>
        /// <para>Speichert die Lognachricht auf dem Server, wenn dieser aktuell 
        /// Nachrichten mit diesem Level annimt.</para>
        /// <para>Gibt eine Fehlermeldung zurück, wenn der Level nicht hoch genug ist.</para>
        /// <para>Kann bei <see cref="MinimumLevel" /> ermittelt werden.</para>
        /// </summary>
        /// <param name="clientIdentification">
        /// Die Identifikation vom User. Diese wird selbst vom Client vergeben
        /// und dient dazu, dass der Server die Anfragen nachvolziehen kann
        /// </param>
        /// <param name="clientLogLevel">Der Level von der Lognachricht</param>
        /// <param name="message">
        /// Die Lognachricht welche gespeichert werden soll. 
        /// Sollte nicht den Timestamp und den Loglevel beinhalten
        /// </param>
        /// <param name="options">Die Optionen von der Lognachricht</param>
        /// <returns>Gibt bei Erfolg nichts zurück. Gibt</returns>
        [HttpPost]
        public ActionResult Message(
            string clientIdentification,
            ClientLogLevel clientLogLevel,
            string message, 
            [FromBody]LogMessageActionOptions options)
        {
            this.logger.LogTrace("LogController: Message called");
            var loglevelToLow = true;

            if (clientLogLevel <= this.minimumClientLogLevel)
            {
                IList<object> parametersObjectsList = new List<object>();
                
                loglevelToLow = false;
                var msLogLevel = this.ConvertToLogLevel(clientLogLevel);

                message = "<{clientIdentification}> " + message;
                if (options.Error != null)
                {
                    message += string.Format(" ({0})", options.Error);
                }

                options.Parameters.Insert(0, clientIdentification);

                foreach (var parameter in options.Parameters)
                {
                    parametersObjectsList.Add(parameter);
                }

                this.logger.Log(msLogLevel, message, parametersObjectsList.ToArray());
            }

            this.logger.LogTrace("LogController: Message finished");

            if (loglevelToLow == false)
            {
                return base.NoContent();
            }
            else
            {
                var error = new AddLogmessageApiError()
                {
                    Message = "LogMessage is higher than the minium Level",
                    MinimumLogLevel = this.minimumClientLogLevel
                };
                return base.StatusCode(412, error);
            }
        }
        #endregion

        #region ConvertToLogLevel
        /// <summary>
        /// Konvertiert <see cref="ClientLogLevel" /> zu <see cref="LogLevel" />
        /// </summary>
        /// <param name="clientLogLevel">Der Loglevel welche konvertiert werden soll</param>
        /// <returns>Gibt den <see cref="LogLevel" /> zurück</returns>
        private LogLevel ConvertToLogLevel(ClientLogLevel clientLogLevel)
        {
            LogLevel result = LogLevel.None;

            switch (clientLogLevel)
            {
                case ClientLogLevel.Fatal:
                    result = LogLevel.Critical;
                    break;
                case ClientLogLevel.Error:
                    result = LogLevel.Error;
                    break;
                case ClientLogLevel.Warning:
                    result = LogLevel.Warning;
                    break;
                case ClientLogLevel.Information:
                    result = LogLevel.Information;
                    break;
                case ClientLogLevel.Debug:
                    result = LogLevel.Debug;
                    break;
                case ClientLogLevel.Verbose:
                    result = LogLevel.Trace;
                    break;
                default:
                    result = LogLevel.None;
                    break;
            }

            return result;
        }
        #endregion
    }
}