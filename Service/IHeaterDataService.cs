namespace Heizung.ServerDotNet.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Heizung.ServerDotNet.Entities;

    /// <summary>
    /// Servcie für die Heizungsdaten
    /// </summary>
    public interface IHeaterDataService
    {
        #region events
        /// <summary>
        /// Wird ausgeführt, wenn neue Daten angekommen sind
        /// </summary>
        event Action<IDictionary<int, HeaterData>> NewDataEvent;
        #endregion

        #region CurrentHeaterValues
        /// <summary>
        /// Dicitonary mit den aktuellen Heizungsdaten
        /// </summary>
        IDictionary<int, HeaterData> CurrentHeaterValues { get; }
        #endregion

        #region SetNewData
        /// <summary>
        /// Setzt die neuen Heizungsdaten. Wenn neuen Daten sich von den bisherigen unterscheiden
        /// wird das NewDataEvnet ausgelöst
        /// </summary>
        /// <param name="heaterValues">Die neuen Daten</param>
        /// <param name="cancellationToken">Token mit dem die Ausführung der Abfrage abgebrochen werden kann</param>
        Task SetNewData(IList<HeaterValue> heaterValues, CancellationToken cancellationToken);
        #endregion
    }
}