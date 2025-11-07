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
    using Serilog.Events;

    /// <summary>
    /// Enum für die Quelle vom Logging
    /// </summary>
    public enum LoggingSource
    {
        /// <summary>
        /// Diese Quelle gilt für alle Lognarichten, welche nicht genau spezifiert werden
        /// </summary>
        General,

        /// <summary>
        /// Diese Quelle gilt für alle Lognarichten, welche von Microsoft kommen
        /// </summary>
        Microsoft,

        /// <summary>
        /// Diese Quelle gilt für alle Lognarichten, welche vom Client kommen
        /// </summary>
        Client
    }

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
        /// Service für Lognachrichten, welche vom Client stammen
        /// </summary>
        private readonly Serilog.ILogger clientLogger;

        /// <summary>
        /// Klasse, welche entscheidet, was geloggt wird
        /// </summary>
        private readonly AppLoggingLevelSwitch appLoggingLevelSwitch;
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert den Controller
        /// </summary>
        /// <param name="logger">Service für Lognachrichten</param>
        /// <param name="appLoggingLevelSwitch">Klasse, welche entscheidet, was geloggt wird</param>
        public LogController(
            ILogger<HeaterDataController> logger,
            AppLoggingLevelSwitch appLoggingLevelSwitch)
        {
            this.logger = logger;

            // Wird gemacht, damit der Loglevel von den Client-Nachrichten sperat geschaltet werden kann
            this.clientLogger = Serilog.Log.ForContext("SourceContext", "Client");
            this.appLoggingLevelSwitch = appLoggingLevelSwitch;
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
            return base.Ok(this.ConvertClientLogLevel(this.appLoggingLevelSwitch.ClientLoggingLevelSwitch.MinimumLevel));
        }
        #endregion

        #region MinimumLevel PUT
        /// <summary>
        /// Setzt den MiniumLevel für Lognarchiten in der Kategorie
        /// </summary>
        /// <returns>Gibt nichts zurück</returns>
        [HttpPut]
        public ActionResult MinimumLevel(LoggingSource loggingSource, LogEventLevel newMiniumLevel)
        {
            this.logger.LogTrace("LogController: MinimumLevel PUT called");

            switch(loggingSource)
            {
                case LoggingSource.General:
                    this.appLoggingLevelSwitch.GerneralLoggingLevelSwitch.MinimumLevel = newMiniumLevel;
                    break;
                case LoggingSource.Microsoft:
                    this.appLoggingLevelSwitch.MicrosoftLoggingLevelSwitch.MinimumLevel = newMiniumLevel;
                    break;
                case LoggingSource.Client:
                    this.appLoggingLevelSwitch.ClientLoggingLevelSwitch.MinimumLevel = newMiniumLevel;
                    break;
            }

            return base.NoContent();
        }
        #endregion

        #region Message POST
        /// <summary>
        /// <para>Speichert die Lognachricht auf dem Server, wenn dieser aktuell 
        /// Nachrichten mit diesem Level annimt.</para>
        /// <para>Gibt eine Fehlermeldung zurück, wenn der Level nicht hoch genug ist.</para>
        /// <para>Kann bei <see cref="MinimumLevel()" /> ermittelt werden.</para>
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

            if (clientLogLevel <= this.ConvertClientLogLevel(this.appLoggingLevelSwitch.ClientLoggingLevelSwitch.MinimumLevel))
            {
                IList<object> parametersObjectsList = new List<object>();
                
                loglevelToLow = false;
                var msLogLevel = this.ConvertToLogLevel(clientLogLevel);

                message = "<{clientIdentification}> " + message;
                if (options.Error is string errorString)
                {
                    var stringError = errorString;

                    stringError = stringError.Replace("{", "{{");
                    stringError = stringError.Replace("}", "}}");

                    message += string.Format(" ({0})", stringError);
                }

                options.Parameters.Insert(0, clientIdentification);

                foreach (var parameter in options.Parameters)
                {
                    parametersObjectsList.Add(parameter);
                }

                this.clientLogger.Write(this.ConvertLogEventLevel(clientLogLevel), message, parametersObjectsList.ToArray());
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
                    Message = "Level is lower than the minium Level",
                    MinimumLogLevel = this.ConvertClientLogLevel(this.appLoggingLevelSwitch.ClientLoggingLevelSwitch.MinimumLevel)
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

        #region ConvertClientLogLevel
        /// <summary>
        /// Konvertiert <see cref="LogEventLevel" /> zu <see cref="ClientLogLevel" />
        /// </summary>
        /// <param name="logEventLevel">Der Loglevel welche konvertiert werden soll</param>
        /// <returns>Gibt den <see cref="LogLevel" /> zurück</returns>
        private ClientLogLevel ConvertClientLogLevel(LogEventLevel logEventLevel)
        {
            ClientLogLevel result = ClientLogLevel.Off;

            switch (logEventLevel)
            {
                case LogEventLevel.Fatal:
                    result = ClientLogLevel.Fatal;
                    break;
                case LogEventLevel.Error:
                    result = ClientLogLevel.Error;
                    break;
                case LogEventLevel.Warning:
                    result = ClientLogLevel.Warning;
                    break;
                case LogEventLevel.Information:
                    result = ClientLogLevel.Information;
                    break;
                case LogEventLevel.Debug:
                    result = ClientLogLevel.Debug;
                    break;
                case LogEventLevel.Verbose:
                    result = ClientLogLevel.Verbose;
                    break;
                default:
                    result = ClientLogLevel.Off;
                    break;
            }

            return result;
        }
        #endregion

        #region ConvertLogEventLevel
        /// <summary>
        /// Konvertiert <see cref="ClientLogLevel" /> zu <see cref="LogEventLevel" />
        /// </summary>
        /// <param name="clientLogLevel">Der Loglevel welche konvertiert werden soll</param>
        /// <returns>Gibt den <see cref="LogEventLevel" /> zurück</returns>
        private LogEventLevel ConvertLogEventLevel(ClientLogLevel clientLogLevel)
        {
            LogEventLevel result = LogEventLevel.Verbose;

            switch (clientLogLevel)
            {
                case ClientLogLevel.Fatal:
                    result = LogEventLevel.Fatal;
                    break;
                case ClientLogLevel.Error:
                    result = LogEventLevel.Error;
                    break;
                case ClientLogLevel.Warning:
                    result = LogEventLevel.Warning;
                    break;
                case ClientLogLevel.Information:
                    result = LogEventLevel.Information;
                    break;
                case ClientLogLevel.Debug:
                    result = LogEventLevel.Debug;
                    break;
                case ClientLogLevel.Verbose:
                    result = LogEventLevel.Verbose;
                    break;
                default:
                    result = LogEventLevel.Verbose;
                    break;
            }

            return result;
        }
        #endregion
    }
}