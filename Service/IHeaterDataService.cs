namespace Heizung.ServerDotNet.Service
{
    using System;
    using System.Collections.Generic;
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
        void SetNewData(IList<HeaterValue> heaterValues);
        #endregion

        #region SetHistoryData
        /// <summary>
        /// Fügt die Heizungsdaten in die Datenbank ein, wenn diese noch nicht vorhanden sind oder neuer sind..
        /// </summary>
        /// <remarks>
        /// Dünnt beim Einfügen die Daten aus, wenn diese zu häufig in einem Zeitraum vorkommen.
        /// </remarks>
        /// <param name="historyHeaterValues">Die Heizungsdaten, die eingefügt werden sollen.</param>
        void SetHistoryData(IList<HistoryHeaterValue> historyHeaterValues);
        #endregion
    }
}