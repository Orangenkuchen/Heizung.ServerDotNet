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
        #region TestEcho
        /// <summary>
        /// Sendet den angeben Text zurück.
        /// </summary>
        /// <param name="echoText">Der Text, welcher zurück gesendet werden soll</param>
        /// <returns>Gibt nichts zurück</returns>
        private async Task TestEcho(string echoText)
        {
            await this.Clients.Caller.SendAsync("Echo", echoText);
        }
        #endregion
    }
}