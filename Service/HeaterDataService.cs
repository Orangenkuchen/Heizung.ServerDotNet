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
        /// Zeit an, das neue Daten empfangen wurden seit dem letzten Speichern in der Datenbnak
        /// </summary>
        private bool updateSinceDBSave;

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

            // Periodisches Speichern der Daten in der Datenbank machen
            var saveTimer = new Timer(
                (timerState) => {
                    this.logger.LogDebug("HistoryDataTimer elapsed. Saving Historydata.");
                    this.updateSinceDBSave = false;

                    if (this.CurrentHeaterValues.Count > 0)
                    {
                        this.heaterRepository.SetHeaterValue(this.CurrentHeaterValues);
                    }
                }, 
                null, 
                0, 
                Convert.ToInt32(new TimeSpan(0, 15, 0).TotalMilliseconds));
            this.destroyFunctions.Add(() => {
                saveTimer.Dispose();
                saveTimer = null;
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
            this.updateSinceDBSave = true;

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

                // Wenn Heizungsstatus
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

                        if (errorDictionary.ContainsKey(errorId.Value))
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
                    if (newHeaterData.ValueTypeId == 1)
                    {
                        isNewData = this.CeckForDoorOpening(newHeaterData.Data[0], heaterValuesDescriptionDictionary);
                    }

                    if (heaterValue.Multiplicator == 0)
                    {
                        heaterValue.Multiplicator = 1;
                    }

                    newHeaterData.Data[0].Value = Convert.ToDouble(heaterValue.Value) / heaterValue.Multiplicator;
                }

                if (this.CurrentHeaterValues[newHeaterData.ValueTypeId].Data[0].Value != newHeaterData.Data[0].Value)
                {
                    isNewData = true;
                }

                if (isNewData)
                {
                    this.CurrentHeaterValues[newHeaterData.ValueTypeId].Data[0].Value = newHeaterData.Data[0].Value;
                    this.CurrentHeaterValues[newHeaterData.ValueTypeId].Data[0].TimeStamp = newHeaterData.Data[0].TimeStamp;
                    this.NewDataEvent?.Invoke(this.CurrentHeaterValues);
                }
            }

            this.logger.LogTrace("SetNewData ended");
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
            var result = false;
            var statusValue = (int)statusDataPoint.Value;

            if (this.CurrentHeaterValues.ContainsKey(200) == false)
            {
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
                var lastDoorOpenings = this.doorOpeningsSinceFireOut.Last();

                if (lastDoorOpenings.EndDateTime == null)
                {
                    lastDoorOpenings.EndDateTime = DateTime.Now;
                    result = true;
                }
            }
            else if (statusValue == 2 ||
                     statusValue == 3 ||
                     statusValue == 4)
            {
                if (this.doorOpeningsSinceFireOut.Count > 0)
                {
                    this.doorOpeningsSinceFireOut.Clear();
                    result = true;
                }
            }
            else if (statusValue == 6)
            {
                result = true;
                var doorOpeningAlreadyRegistered = false;

                if (this.doorOpeningsSinceFireOut.Count > 0)
                {
                    var lastDoorOpenings = this.doorOpeningsSinceFireOut.Last();

                    doorOpeningAlreadyRegistered = lastDoorOpenings.EndDateTime == null;
                }

                if (doorOpeningAlreadyRegistered == false)
                {
                    this.doorOpeningsSinceFireOut.Add(new DoorOpening()
                    {
                        StartDateTime = DateTime.Now,
                        EndDateTime = null
                    });
                }
            }

            if (this.CurrentHeaterValues[200].Data.Count == 0)
            {
                this.CurrentHeaterValues[200].Data.Add(new HeaterDataPoint(0)
                {
                    TimeStamp = DateTime.Now
                });

                if (result == true)
                {
                    this.CurrentHeaterValues[200].Data[0].Value = this.GetSumOfDoorOpenings(this.doorOpeningsSinceFireOut);
                    this.CurrentHeaterValues[200].Data[0].TimeStamp = DateTime.Now;
                }
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