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
    public class HeaterDataController : ControllerBase
    {
        #region fields
        /// <summary>
        /// Service für Lognachrichten
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Service für die aktuellen Heizungsdaten
        /// </summary>
        private readonly IHeaterDataService heaterDataService;

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
        /// <param name="heaterDataService">Service für die aktuellen Heizungsdaten</param>
        /// <param name="heaterRepository">Repository für die Historiendaten der Heizung</param>
        public HeaterDataController(
            ILogger<HeaterDataController> logger, 
            IHeaterDataService heaterDataService, 
            IHeaterRepository heaterRepository)
        {
            this.logger = logger;
            this.heaterDataService = heaterDataService;
            this.heaterRepository = heaterRepository;
        }
        #endregion

        #region Data
        /// <summary>
        /// Ermittelt die Heizungsdaten im angegeben Zeitraum
        /// </summary>
        /// <param name="fromDate">Der Startzeitpunkt der Datenbeschaffung</param>
        /// <param name="toDate">Der Endzeitpunkt der Datenbeschaffung</param>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpGet]
        public ActionResult<IDictionary<int, HeaterData>> Data(DateTime fromDate, DateTime toDate)
        {
            this.logger.LogTrace("Data: Get called");
            IDictionary<int, HeaterData> result = new Dictionary<int, HeaterData>();

            var valueDescriptions = this.heaterRepository.GetAllValueDescriptions();
            var errorDescription = this.heaterRepository.GetAllErrorDictionary();
            var dataList = this.heaterRepository.GetDataValues(fromDate, toDate);

            foreach (var valueDescription in valueDescriptions)
            {
                result.Add(
                    valueDescription.Id, 
                    new HeaterData(valueDescription.Description, valueDescription.Unit)
                    {
                        ValueTypeId = valueDescription.Id,
                        IsLogged = valueDescription.IsLogged
                    });
            }

            IList<int> loggedMissingValueTypes = new List<int>();

            foreach (var dataPoint in dataList)
            {
                if (result.ContainsKey(dataPoint.ValueType))
                {
                    if (dataPoint.ValueType == 99)
                    {
                        if (dataPoint.Value.GetType() == typeof(int))
                        {
                            if (errorDescription.ContainsKey((int)dataPoint.Value))
                            {
                                dataPoint.Value = errorDescription[(int)dataPoint.Value];
                            }
                        }
                    }

                    result[dataPoint.ValueType].Data.Add(new HeaterDataPoint(dataPoint.Value)
                    {
                        TimeStamp = dataPoint.TimeStamp
                    });
                }
                else
                {
                    if (loggedMissingValueTypes.Contains(dataPoint.ValueType) == false)
                    {
                        this.logger.LogWarning("A data point was received from the data base wich has no value descritpion assoziatet to it. It will be skipped and not be send to the client. (Id: {0})", dataPoint.ValueType);
                        
                        loggedMissingValueTypes.Add(dataPoint.Id);
                    }
                }
            }

            this.logger.LogTrace("Data: Get finished");
            return base.Ok(result);
        }
        #endregion

        #region ValueDescriptions
        /// <summary>
        /// Ermittelt die Beschreibungen zu den Heizungsdaten
        /// </summary>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpGet]
        public ActionResult<IDictionary<int, ValueDescription>> ValueDescriptions()
        {
            this.logger.LogTrace("ValueDescriptions called");
            IDictionary<int, ValueDescription> result = new Dictionary<int, ValueDescription>();

            var valueDescriptions = this.heaterRepository.GetAllValueDescriptions();
            
            result = valueDescriptions.ToDictionary((x) => x.Id);

            this.logger.LogTrace("ValueDescriptions finished");
            return base.Ok(result);
        }
        #endregion

        #region Latest GET
        /// <summary>
        /// Ermittelt die Heizungsdaten welche zuletzt empfangen wurden
        /// </summary>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpGet]
        public ActionResult<IDictionary<int, HeaterData>> Latest()
        {
            this.logger.LogTrace("Latest called");
            return base.Ok(this.heaterDataService.CurrentHeaterValues);
        }
        #endregion

        #region Latest PUT
        /// <summary>
        /// Setzt die neuen Heizungsdaten als aktuelle Daten
        /// </summary>
        /// <param name="heaterValues">Die Heizungsdaten welche gesetzt werden sollen</param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult Latest([FromBody]IList<HeaterValue> heaterValues)
        {
            this.logger.LogTrace("Latest called");

            this.heaterDataService.SetNewData(heaterValues);

            this.logger.LogTrace("Latest finished");
            return base.NoContent();
        }
        #endregion

        #region OperatingHoures GET
        /// <summary>
        /// Ermittelt die Betriebsstunden im angegeben Zeitraum
        /// </summary>
        /// <param name="fromDate">Der Startzeitpunkt der Datenbeschaffung</param>
        /// <param name="toDate">Der Endzeitpunkt der Datenbeschaffung</param>
        /// <returns>Gibt die Daten als IList zurück</returns>
        [HttpGet]
        public ActionResult<IList<DayOperatingHoures>> OperatingHoures(DateTime fromDate, DateTime toDate)
        {
            this.logger.LogTrace("OperatingHoures: Get called");
            IList<DayOperatingHoures> result = this.heaterRepository.GetOperatingHoures(fromDate, toDate);

            this.logger.LogTrace("Get finished");
            return base.Ok(result);
        }
        #endregion

        #region LoggingState
        /// <summary>
        /// Stellt ein, welche Daten in der Historie aufgezeichnet werden
        /// </summary>
        /// <param name="loggingStates">Die Heizungsdaten welche gesetzt werden sollen</param>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpPut]
        public ActionResult LoggingState([FromBody]IList<LoggingState> loggingStates)
        {
            this.logger.LogTrace("LoggingState called");

            this.heaterRepository.SetLoggingStateOfVaueType(loggingStates);

            this.logger.LogTrace("LoggingState finished");
            return base.NoContent();
        }
        #endregion
    }
}