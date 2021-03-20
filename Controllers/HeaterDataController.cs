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
        public HeaterDataController(ILogger<HeaterDataController> logger, IHeaterDataService heaterDataService, IHeaterRepository heaterRepository)
        {
            this.logger = logger;
            this.heaterDataService = heaterDataService;
            this.heaterRepository = heaterRepository;
        }
        #endregion

        #region GetLatest
        /// <summary>
        /// Ermittelt die Heizungsdaten welche zuletzt empfangen wurden
        /// </summary>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpGet]
        public ActionResult<IDictionary<int, HeaterData>> GetLatest()
        {
            this.logger.LogTrace("GetLatest called");
            return base.Ok(this.heaterDataService.CurrentHeaterValues);
        }
        #endregion

        #region Get
        /// <summary>
        /// Ermittelt die Heizungsdaten im angegeben Zeitraum
        /// </summary>
        /// <param name="fromDate">Der Startzeitpunkt der Datenbeschaffung</param>
        /// <param name="toDate">Der Endzeitpunkt der Datenbeschaffung</param>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpGet]
        public ActionResult<IDictionary<int, HeaterData>> Get(DateTime fromDate, DateTime toDate)
        {
            this.logger.LogTrace("Get called");
            IDictionary<int, HeaterData> result = new Dictionary<int, HeaterData>();

            var valueDescriptions = this.heaterRepository.GetAllValueDescriptions();
            var errorDescription = this.heaterRepository.GetAllErrorDictionary();
            var dataList = this.heaterRepository.GetDataValues(fromDate, toDate);

            foreach (var valueDescription in valueDescriptions)
            {
                result.Add(
                    valueDescription.Id, 
                    new HeaterData()
                    {
                        ValueTypeId = valueDescription.Id,
                        Description = valueDescription.Description,
                        IsLogged = valueDescription.IsLogged,
                        Unit = valueDescription.Unit,
                        Data = new List<HeaterDataPoint>()
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

                    result[dataPoint.ValueType].Data.Add(new HeaterDataPoint()
                    {
                        TimeStamp = dataPoint.TimeStamp,
                        Value = dataPoint.Value
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

            this.logger.LogTrace("Get finished");
            return base.Ok(result);
        }
        #endregion

        #region GetValueDescriptions
        /// <summary>
        /// Ermittelt die Beschreibungen zu den Heizungsdaten
        /// </summary>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpGet]
        public ActionResult<IDictionary<int, ValueDescription>> GetValueDescriptions()
        {
            this.logger.LogTrace("GetValueDescriptions called");
            IDictionary<int, ValueDescription> result = new Dictionary<int, ValueDescription>();

            var valueDescriptions = this.heaterRepository.GetAllValueDescriptions();
            
            result = valueDescriptions.ToDictionary((x) => x.Id);

            this.logger.LogTrace("GetValueDescriptions finished");
            return base.Ok(result);
        }
        #endregion

        #region SetHeaterData
        /// <summary>
        /// Setzt die neuen Heizungsdaten als aktuelle Daten
        /// </summary>
        /// <param name="heaterData">Die Heizungsdaten welche gesetzt werden sollen</param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult SetHeaterData([FromBody]IList<HeaterData> heaterData)
        {
            this.logger.LogTrace("SetHeaterData called");

            IDictionary<int, HeaterData> dataToSave = heaterData.ToDictionary((x) => x.ValueTypeId);

            this.heaterRepository.SetHeaterValue(dataToSave);

            this.logger.LogTrace("SetHeaterData finished");
            return base.NoContent();
        }
        #endregion

        #region SetLoggingState
        /// <summary>
        /// Stellt ein, welche Daten in der Historie aufgezeichnet werden
        /// </summary>
        /// <param name="loggingStates">Die Heizungsdaten welche gesetzt werden sollen</param>
        /// <returns>Gibt die Daten als IDictionary zurück</returns>
        [HttpPut]
        public ActionResult SetLoggingState([FromBody]IList<LoggingState> loggingStates)
        {
            this.logger.LogTrace("SetLoggingState called");

            this.heaterRepository.SetLoggingStateOfVaueType(loggingStates);

            this.logger.LogTrace("SetLoggingState finished");
            return base.NoContent();
        }
        #endregion
    }
}