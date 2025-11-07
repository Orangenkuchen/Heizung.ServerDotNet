using Serilog.Core;
using Serilog.Events;

namespace Heizung.ServerDotNet.Entities
{
    /// <summary>
    /// Klasse welche die LoggingLevelSwitches für die Anwendung beinhaltet
    /// </summary>
    public class AppLoggingLevelSwitch
    {
        #region ctor
        /// <summary>
        /// Erstellt die Klasse
        /// </summary>
        /// <param name="generalMinimumLoggingLevel">Wenn Lognachrichten niedriger als dieser Wert sind, werden diese ignoriert</param>
        /// <param name="micorosftMinimumLoggingLevel">Wenn Lognachrichten niedriger als dieser Wert sind, werden diese ignoriert. (Überschreibt den Level für alle Lognachrichten, welche von der Quelle 'Microsoft' kommen. Hat höhere Priorität als generalMinimumLogingLevel)</param>
        /// <param name="clientMinimumLoggingLevel">Wenn Lognachrichten niedriger als dieser Wert sind, werden diese ignoriert. (Überschreibt den Level für alle Lognachrichten, welche von der Quelle 'Client' kommen. Hat höhere Priorität als generalMinimumLogingLevel)</param>
        public AppLoggingLevelSwitch(
            LogEventLevel generalMinimumLoggingLevel = LogEventLevel.Warning,
            LogEventLevel micorosftMinimumLoggingLevel = LogEventLevel.Warning,
            LogEventLevel clientMinimumLoggingLevel = LogEventLevel.Warning)
        {
            this.GerneralLoggingLevelSwitch = new LoggingLevelSwitch(generalMinimumLoggingLevel);
            this.MicrosoftLoggingLevelSwitch = new LoggingLevelSwitch(generalMinimumLoggingLevel);
            this.ClientLoggingLevelSwitch = new LoggingLevelSwitch(clientMinimumLoggingLevel);
        }
        #endregion

        #region GerneralLoggingLevelSwitch
        /// <summary>
        /// Dieser LoggingLevelSwitch ist für alle Lognachrichten aktiv, welche nicht von einem anderen Switch überschrieben werden
        /// </summary>
        /// <value></value>
        public LoggingLevelSwitch GerneralLoggingLevelSwitch { get; private set; }
        #endregion

        #region MicrosoftLoggingLevelSwitch
        /// <summary>
        /// Dieser LoggingLevelSwitch ist für alle Lognachrichten aktiv, welche als Quelle Microsoft haben. (Überschreibt <see cref="GerneralLoggingLevelSwitch"/> für Microsoft-Nachrichten.)
        /// </summary>
        /// <value></value>
        public LoggingLevelSwitch MicrosoftLoggingLevelSwitch { get; private set; }
        #endregion

        #region ClientLoggingLevelSwitch
        /// <summary>
        /// Dieser LoggingLevelSwitch ist für alle Lognachrichten aktiv, welche als Quelle 'Client' haben. Das beinhaltet alle Lognarichten vom Web-Client. (Überschreibt <see cref="GerneralLoggingLevelSwitch"/> für Client-Nachrichten.)
        /// </summary>
        /// <value></value>
        public LoggingLevelSwitch ClientLoggingLevelSwitch { get; private set; }
        #endregion
    }
}