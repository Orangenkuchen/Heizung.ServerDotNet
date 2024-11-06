namespace Heizung.ServerDotNet.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Heizung.ServerDotNet.Data;
    using Heizung.ServerDotNet.Entities;
    using Heizung.ServerDotNet.SignalRHubs;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Servcie für die Heizungsdaten
    /// </summary>
    public class HeaterDataService : IDisposable, IHeaterDataService
    {
        #region fields
        /// <summary>
        /// Repository für die Heizungsdaten
        /// </summary>
        private readonly IHeaterRepository heaterRepository;

        /// <summary>
        /// Liste von Funktionen, welche beim abbauen der Klasse aufgerufen werden sollen
        /// </summary>
        private IList<Action> destroyFunctions;

        /// <summary>
        /// Promise für ein Dictionary mit den HeaterValueDescrptions aus der Datenbank.
        /// Enthält z.B. ob die Daten geloggt werden sollen
        /// </summary>
        private readonly Task<IDictionary<int, ValueDescription>> heaterValueDescriptionDictionaryPromise;

        /// <summary>
        /// Promise für Dictionary welches die Fehlerwerte aus der Datenbank enthält 
        /// </summary>
        private readonly Task<IDictionary<int, ErrorDescription>> errorDictionaryPromise;

        /// <summary>
        /// Liste welcher alle Türöffnungen seit dem ausgehen des Feuers beinhaltet. Wird beim Anheizen geleert.
        /// </summary>
        private readonly IList<DoorOpening> doorOpeningsSinceFireOut;

        /// <summary>
        /// Service für Lognachrichten
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Hub zum senden von Nachrichten an Clients bezüglich den Heizungsdaten
        /// </summary>
        private readonly IHubContext<HeaterDataHub> heaterDataHub;

        /// <summary>
        /// Die zeitliche Auflösung der HistorienDaten in der Datenbank.
        /// </summary>
        /// <remarks>
        /// Wenn der Wert z.B. 15m wurden die Daten in der Datenbank normalerweise
        /// alle 15m geschrieben.
        /// </remarks>
        private readonly TimeSpan historyDataTimeResolution;

        /// <summary>
        /// Buffer von Historien-Heizungsdaten
        /// </summary>
        public IList<HeaterData> heaterValuesBuffer { get; private set; }
        #endregion

        #region events
        /// <summary>
        /// Wird ausgeführt, wenn neue Daten angekommen sind
        /// </summary>
        public event Action<IDictionary<int, HeaterData>>? NewDataEvent;
        #endregion

        #region CurrentHeaterValues
        /// <summary>
        /// Dicitonary mit den aktuellen Heizungsdaten
        /// </summary>
        public IDictionary<int, HeaterData> CurrentHeaterValues { get; private set; }
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="logger">Service für Lognachrichten</param>
        /// <param name="heaterRepository">Repository für die Heizungsdaten</param>
        /// <param name="heaterDataHub">Hub zum senden von Nachrichten an Clients bezüglich den Heizungsdaten</param>
        public HeaterDataService(
            ILogger<HeaterDataService> logger, 
            IHeaterRepository heaterRepository,
            IHubContext<HeaterDataHub> heaterDataHub)
        {
            this.logger = logger;
            this.heaterRepository = heaterRepository;
            this.heaterDataHub = heaterDataHub;
            this.destroyFunctions = new List<Action>();
            this.CurrentHeaterValues = new Dictionary<int, HeaterData>();
            this.doorOpeningsSinceFireOut = new List<DoorOpening>();
            this.heaterValuesBuffer = new List<HeaterData>();

            this.historyDataTimeResolution = TimeSpan.FromMinutes(15);

            this.heaterValueDescriptionDictionaryPromise = Task.Run<IDictionary<int, ValueDescription>>(() => {
                // Das ConcurrentDictionary ist hier notwendig, da ansonsten Exceptions auftreten können bei mehreren Threads
                IDictionary<int, ValueDescription> result = new ConcurrentDictionary<int, ValueDescription>();
                
                var valueDescriptions = this.heaterRepository.GetAllValueDescriptions();

                foreach (var valueDescription in valueDescriptions)
                {
                    result[valueDescription.Id] = valueDescription;
                }
                
                return result;
            });
            this.heaterValueDescriptionDictionaryPromise.ContinueWith((task) =>
            {
                this.FillHeaterDataDictionaryByValueDescription(this.CurrentHeaterValues, task.Result);
            });

            // Periodisches Speichern der Daten im Buffer-Array
            var bufferTimer = new Timer(
                (timerState) => {
                    this.logger.LogDebug("Buffer HistoryDataTimer elapsed. Buffering Historydata.");

                    if (this.CurrentHeaterValues.Count > 0)
                    {
                        foreach (var element in this.CurrentHeaterValues.Values)
                        {
                            if (element.IsLogged)
                            {
                                var heaterData = new HeaterData(element.Description, element.Unit)
                                {
                                    ValueTypeId = element.ValueTypeId,
                                    IsLogged = element.IsLogged
                                };

                                var heaterDataPoint = new HeaterDataPoint(element.Data[0].Value)
                                {
                                    TimeStamp = element.Data[0].TimeStamp
                                };
                                heaterData.Data.Add(heaterDataPoint);

                                this.heaterValuesBuffer.Add(heaterData);
                            }
                        }
                    }
                }, 
                null, 
                0, 
                Convert.ToInt32(new TimeSpan(0, 15, 0).TotalMilliseconds));

            // Periodisches Speichern der Daten in der Datenbank machen
            var saveTimer = new Timer(
                (timerState) => {
                    this.logger.LogDebug("HistoryDataTimer elapsed. Saving Historydata.");

                    if (this.heaterValuesBuffer.Count > 0)
                    {
                        this.heaterRepository.SetHeaterValue(this.heaterValuesBuffer);
                        this.heaterValuesBuffer.Clear();
                    }
                }, 
                null, 
                0, 
                Convert.ToInt32(new TimeSpan(24, 0, 0).TotalMilliseconds));

            this.destroyFunctions.Add(() => {
                bufferTimer.Dispose();
                saveTimer.Dispose();
                saveTimer = null;
                bufferTimer = null;
            });

            this.errorDictionaryPromise = Task.Run(() => {
                IDictionary<int, ErrorDescription> result = new ConcurrentDictionary<int, ErrorDescription>(this.heaterRepository.GetAllErrorDictionary());

                return result;
            });
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Baut die Klasse ab
        /// </summary>
        public void Dispose()
        {
            foreach (var destroyFunction in destroyFunctions)
            {
                destroyFunction();
            }

            this.destroyFunctions.Clear();
        }
        #endregion

        #region SetNewData
        /// <summary>
        /// Setzt die neuen Heizungsdaten. Wenn neuen Daten sich von den bisherigen unterscheiden
        /// wird das NewDataEvnet ausgelöst
        /// </summary>
        /// <param name="heaterValues">Die neuen Daten</param>
        public async void SetNewData(IList<HeaterValue> heaterValues) 
        {
            this.logger.LogTrace("SetNewData started");

            await Task.WhenAll(this.heaterValueDescriptionDictionaryPromise, this.errorDictionaryPromise);

            var heaterValuesDescriptionDictionary = this.heaterValueDescriptionDictionaryPromise.Result;
            var errorDictionary = this.errorDictionaryPromise.Result;

            var isNewData = false;

            foreach (var heaterValue in heaterValues)
            {
                var dataList = new List<HeaterDataPoint>();
                dataList.Add(new HeaterDataPoint(0)
                {
                    TimeStamp = DateTime.Now
                });

                var newHeaterData = new HeaterData(heaterValue.Name, heaterValue.Unit)
                {
                    IsLogged = false,
                    ValueTypeId = heaterValue.Index,
                    Data = dataList
                };

                if (heaterValuesDescriptionDictionary.ContainsKey(newHeaterData.ValueTypeId))
                {
                    newHeaterData.IsLogged = heaterValuesDescriptionDictionary[newHeaterData.ValueTypeId].IsLogged;
                }

                // Wenn Fehler
                if (newHeaterData.ValueTypeId == 99)
                {
                    int? errorId = null;
                    var errorValue = heaterValue.Value.Trim();

                    foreach (var error in errorDictionary)
                    {
                        if (error.Value.Description == errorValue)
                        {
                            errorId = error.Key;
                            break;
                        }
                    }

                    if (errorId == null) 
                    {
                        errorId = this.heaterRepository.SetNewError(errorValue);

                        if (errorDictionary.ContainsKey(errorId.Value) == false)
                        {
                            errorDictionary.Add(errorId.Value, new ErrorDescription(errorValue)
                            {
                                Id = errorId.Value
                            });
                        }
                    }

                    newHeaterData.Data[0].Value = errorId.Value;
                }
                else
                {
                    if (heaterValue.Multiplicator == 0)
                    {
                        heaterValue.Multiplicator = 1;
                    }

                    newHeaterData.Data[0].Value = 0;

                    try
                    {
                        newHeaterData.Data[0].Value = Convert.ToDouble(heaterValue.Value) / heaterValue.Multiplicator;
                    }
                    catch(Exception exception)
                    {
                        this.logger.LogError(exception, "Fehler beim Konvertieren vom Wert \"{0}\"", heaterValue.Value);
                    }
                }

                if (this.CurrentHeaterValues[newHeaterData.ValueTypeId].Data[0].Value != newHeaterData.Data[0].Value)
                {
                    isNewData = true;
                }

                if (isNewData)
                {
                    if (newHeaterData.ValueTypeId == 1)
                    {
                        isNewData = this.CeckForDoorOpening(newHeaterData.Data[0], heaterValuesDescriptionDictionary);
                    }

                    this.CurrentHeaterValues[newHeaterData.ValueTypeId].Data[0].Value = newHeaterData.Data[0].Value;
                    this.CurrentHeaterValues[newHeaterData.ValueTypeId].Data[0].TimeStamp = newHeaterData.Data[0].TimeStamp;
                    this.NewDataEvent?.Invoke(this.CurrentHeaterValues);
                }
            }

            this.logger.LogTrace("SetNewData ended");
        }
        #endregion

        #region SetHistoryData
        /// <inheritdoc cref="SetHistoryData(IList{HistoryHeaterValue})"/>
        public void SetHistoryData(IList<HistoryHeaterValue> historyHeaterValues)
        {
            var minDate = historyHeaterValues.Min((x) => x.Timestamp);
            var maxDate = historyHeaterValues.Max((x) => x.Timestamp);

            var startPoint = new DateTime(
                minDate.Year,
                minDate.Month,
                minDate.Day,
                minDate.Hour,
                Math.Abs(minDate.Minute / this.historyDataTimeResolution.Minutes) * this.historyDataTimeResolution.Minutes,
                0
            );

            var heaterDataDictionary = new Dictionary<int, HeaterData>();

            for (var i = startPoint; i < maxDate; i = i.Add(this.historyDataTimeResolution))
            {
                var latestInChunk = historyHeaterValues.Where((x) => x.Timestamp > i && x.Timestamp < i + this.historyDataTimeResolution)
                                                       .MaxBy((x) => x.Timestamp);

                if (latestInChunk != null)
                {
                    if (heaterDataDictionary.ContainsKey(latestInChunk.Index) == false)
                    {
                        heaterDataDictionary.Add(
                            latestInChunk.Index, 
                            new HeaterData(latestInChunk.Name, latestInChunk.Unit)
                            {
                                IsLogged = false,
                                ValueTypeId = latestInChunk.Index,
                                Data = new List<HeaterDataPoint>()
                            }
                        );
                    }

                    var multiplicator = latestInChunk.Multiplicator > 0 ? latestInChunk.Multiplicator : 1;

                    heaterDataDictionary[latestInChunk.Index].Data.Add(
                        new HeaterDataPoint(Convert.ToDouble(latestInChunk.Value) / multiplicator)
                        {
                            TimeStamp = latestInChunk.Timestamp
                        }
                    );
                }
            }

            this.heaterRepository.SetHeaterValue(heaterDataDictionary.Values);
        }
        #endregion

        #region CeckForDoorOpening
        /// <summary>
        /// Überprüft den Datenpunkt und setzt anhand dessen die Türöffungszeit
        /// </summary>
        /// <param name="statusDataPoint">Der Datenpunkt vom Heizungsstatus</param>
        /// <param name="heaterValuesDescriptionDictionary">Dictionary mit den HeaterValuesDescription</param>
        /// <returns>Gibt zurück ob die Daten sich geändert haben. (Neue Daten)</returns>
        private bool CeckForDoorOpening(
            HeaterDataPoint statusDataPoint, 
            IDictionary<int, ValueDescription> heaterValuesDescriptionDictionary)
        {
            this.logger.LogDebug("CeckForDoorOpening: Cecking for DoorOpenings and calculating Time");

            var result = false;
            var statusValue = Convert.ToInt32(statusDataPoint.Value);

            if (this.CurrentHeaterValues.ContainsKey(200) == false)
            {
                this.logger.LogTrace("CeckForDoorOpening: CurrentHeaterValues does not contain DoorOpenings. Adding empty Object for DoorOpeings at Key 200");
                var doorOpeningsValueDescription = heaterValuesDescriptionDictionary[200];

                this.CurrentHeaterValues[200] = new HeaterData(doorOpeningsValueDescription.Description, doorOpeningsValueDescription.Unit)
                {
                    ValueTypeId = 200,
                    IsLogged = doorOpeningsValueDescription.IsLogged,
                };
            }

            // Wenn auf Zünden gewartet wird (35) oder Vorbelüftet wird (56)
            if ((statusValue == 35 ||
                statusValue == 56) &&
                this.doorOpeningsSinceFireOut.Count > 0)
            {
                this.logger.LogTrace(
                    "CeckForDoorOpening: State after door closing was detected ({0}).", 
                    statusValue);
                var lastDoorOpenings = this.doorOpeningsSinceFireOut.Last();

                if (lastDoorOpenings.EndDateTime == null)
                {
                    this.logger.LogTrace("CheckForDoorOpening: Door closing was not jet detected. Setting Endtime of latest door opening in Dictionary.");
                    lastDoorOpenings.EndDateTime = DateTime.Now;
                    result = true;
                }
                else
                {
                    this.logger.LogTrace("CheckForDoorOpening: Door closing was already detected.");
                }
            }
            else if (statusValue == 2 ||
                     statusValue == 3 ||
                     statusValue == 4)
            {
                this.logger.LogTrace("CheckForDoorOpening: Detected burning state or fire starting state. Clearing door openings.");

                if (this.doorOpeningsSinceFireOut.Count > 0)
                {
                    this.doorOpeningsSinceFireOut.Clear();
                    result = true;
                }
            }
            else if (statusValue == 6)
            {
                this.logger.LogTrace("CheckForDoorOpening: Detected door open state. Cecking if this door opening is already registered.");

                result = true;
                var doorOpeningAlreadyRegistered = false;

                if (this.doorOpeningsSinceFireOut.Count > 0)
                {
                    var lastDoorOpenings = this.doorOpeningsSinceFireOut.Last();

                    doorOpeningAlreadyRegistered = lastDoorOpenings.EndDateTime == null;
                }

                if (doorOpeningAlreadyRegistered == false)
                {
                    this.logger.LogTrace("CheckForDoorOpening: Door opening was not jet detected. Registering in Dictionary.");

                    this.doorOpeningsSinceFireOut.Add(new DoorOpening()
                    {
                        StartDateTime = DateTime.Now,
                        EndDateTime = null
                    });
                }
                else
                {
                    this.logger.LogTrace("CheckForDoorOpening: Door opening is already registered.");
                }
            }

            if (this.CurrentHeaterValues[200].Data.Count == 0)
            {
                this.CurrentHeaterValues[200].Data.Add(new HeaterDataPoint(0)
                {
                    TimeStamp = DateTime.Now
                });
            }

            if (result == true)
            {
                this.logger.LogTrace("CheckForDoorOpening: Door opening timespan has changed. Recalculating. Setting result in CurrentHeaterValues.");

                this.CurrentHeaterValues[200].Data[0].Value = this.GetSumOfDoorOpenings(this.doorOpeningsSinceFireOut);
                this.CurrentHeaterValues[200].Data[0].TimeStamp = DateTime.Now;
            }
            
            return result;
        }
        #endregion

        #region GetSumOfDoorOpenings
        /// <summary>
        /// Ermittelt die Summe der Zeit, welche die Tür geöffnet wurde
        /// </summary>
        /// <param name="doorOpenings">List von Türöffnungen</param>
        /// <returns>Gibt die Zeit in Sekunden an</returns>
        private TimeSpan GetSumOfDoorOpenings(IList<DoorOpening> doorOpenings)
        {
            TimeSpan result = new TimeSpan();

            foreach (var doopOpening in doorOpenings)
            {
                if (doopOpening.EndDateTime == null)
                {
                    result += DateTime.Now - doopOpening.StartDateTime;
                }
                else
                {
                    result += doopOpening.EndDateTime.Value - doopOpening.StartDateTime;
                }
            }

            return result;
        }
        #endregion

        #region SendCurrentHeaterDataToHubClients
        /// <summary>
        /// Sendet die aktuellen Heizungsdaten an alle Clients
        /// </summary>
        /// <returns></returns>
        public async Task SendCurrentHeaterDataToHubClients(IDictionary<int, HeaterData> currentHeaterData)
        {
            await this.heaterDataHub.Clients.All.SendAsync("CurrentHeaterData", currentHeaterData);
        }
        #endregion

        #region FillHeaterDataDictionaryByValueDescription
        /// <summary>
        /// Füllt das HeaterdataDictionary ahand vom ValueDescriptionDictionary
        /// </summary>
        /// <param name="heaterDataDictionary">Das Dictionary, welches gefüllt werden soll</param>
        /// <param name="valueDescriptionDictionary">Das Dictionary, welche zum füllen verwendet werden soll</param>
        private void FillHeaterDataDictionaryByValueDescription(
            IDictionary<int, HeaterData> heaterDataDictionary,
            IDictionary<int, ValueDescription> valueDescriptionDictionary)
        {
            foreach (var valueDescription in valueDescriptionDictionary)
            {
                if (this.CurrentHeaterValues.ContainsKey(valueDescription.Key) == false)
                {
                    this.CurrentHeaterValues.Add(
                        valueDescription.Key, 
                        new HeaterData(
                            valueDescription.Value.Description, 
                            valueDescription.Value.Unit)
                        {
                            ValueTypeId = valueDescription.Value.Id,
                            IsLogged = valueDescription.Value.IsLogged
                        });
                    this.CurrentHeaterValues[valueDescription.Key].Data.Add(new HeaterDataPoint(0));
                }
            }
        }
        #endregion
    }
}
