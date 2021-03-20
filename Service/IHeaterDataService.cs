namespace Heizung.ServerDotNet.Service
{
    using System.Collections.Generic;
    using Heizung.ServerDotNet.Entities;

    /// <summary>
    /// Servcie für die Heizungsdaten
    /// </summary>
    public interface IHeaterDataService
    {
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
    }
}