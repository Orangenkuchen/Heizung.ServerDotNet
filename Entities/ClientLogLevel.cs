namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Der Loglevel vom HttpClient
    /// </summary>
    public enum ClientLogLevel
    {
        /// <summary>
        /// Dieser Level schaltet das Logging aus
        /// </summary>
        Off = 0,

        /// <summary>
        /// Dieser Level loggt nur Nachrichten auf der Ebene Fatal
        /// </summary>
        Fatal = 1,

        /// <summary>
        /// Dieser Level loggt nur Nachrichten auf der Ebene Error und wichtiger
        /// </summary>
        Error = 3,

        /// <summary>
        /// Dieser Level loggt nur Nachrichten auf der Ebene Warning und wichtiger
        /// </summary>
        Warning = 7,

        /// <summary>
        /// Dieser Level loggt nur Nachrichten auf der Ebene Information und wichtiger
        /// </summary>
        Information = 15,

        /// <summary>
        /// Dieser Level loggt nur Nachrichten auf der Ebene Debug und wichtiger
        /// </summary>
        Debug = 31,

        /// <summary>
        /// Dieser Level loggt nur Nachrichten auf der Ebene Verbose und wichtiger
        /// </summary>
        Verbose = 63
    }
}