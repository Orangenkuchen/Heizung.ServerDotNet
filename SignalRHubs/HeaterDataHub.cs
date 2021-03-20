namespace Heizung.ServerDotNet.SignalRHubs
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Heizung.ServerDotNet.Entities;
    using Microsoft.AspNetCore.SignalR;

    /// <summary>
    /// Hub für Heizunsdaten
    /// </summary>
    public class HeaterDataHub : Hub
    {        
        #region SendCurrentHeaterData
        /// <summary>
        /// Sendet die aktuellen Heizungsdaten an alle Clients
        /// </summary>
        /// <returns></returns>
        protected async Task SendCurrentHeaterData(IDictionary<int, HeaterData> currentHeaterData)
        {
            await Clients.All.SendAsync("CurrentHeaterData", currentHeaterData);
        }
        #endregion
    }
}