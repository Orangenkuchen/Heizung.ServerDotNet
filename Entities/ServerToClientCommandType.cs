namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Gibt an, ob welcher CommandType an den Client gesendet wird (Websocket)
    /// </summary>
    public enum ServerToClientCommandType
    {
        #region AllDataCommand
        /// <summary>
        /// Command zum ermitteln aller Commands
        /// </summary>
        AllDataCommand = 1,
        #endregion

        #region FeedbackCommand
        /// <summary>
        /// Command welche dem Client feedback bezüglich eines vorherigen Commands gibt
        /// </summary>
        FeedbackCommand = 2,
        #endregion

        #region NotifierConfigCommand
        /// <summary>
        /// Command, welche die NotifierConfig dem Client mitteilt
        /// </summary>
        NotifierConfigCommand = 3,
        #endregion

        #region NewestDataCommand
        /// <summary>
        /// Command, welche der Client die neusten Daten mitteilt
        /// </summary>
        NewestDataCommand = 4
        #endregion
    }
}