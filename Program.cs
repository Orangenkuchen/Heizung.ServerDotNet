using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Heizung.ServerDotNet
{
    /// <summary>
    /// Klasse welche beim Programmstart aufgerufen wird
    /// </summary>
    public class Program
    {
        #region Main
        /// <summary>
        /// Die Funktion welcher beim Programmstart aufgerufen wird
        /// </summary>
        /// <param name="args">Die Argumente welche an das Programm gegeben werden</param>
        public static void Main(string[] args)
        {
            var logfilePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "", 
                "Log.txt");

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logfilePath, fileSizeLimitBytes: 10 * 1024 * 1024)
                .CreateLogger();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleUnhandledException);

            try
            {
                Log.Information("Starte den WebServer");
                CreateHostBuilder(args).Build().Run();
            }
            catch(Exception exception)
            {
                Log.Fatal(exception, "Fehler beim Starten des Servers");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        #endregion

        #region CreateHostBuilder
        /// <summary>
        /// Konfiguriert den Webserver
        /// </summary>
        /// <param name="args">Die Argumente welche dem Webserver übergeben werden sollen</param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog() // Überschreibt das Logging mit Serilog
                .UseSystemd()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }
            );
        }
        #endregion

        #region HandleUnhandledException
        /// <summary>
        /// Wird aufgerufen, wenn eine Exception im Programm nicht abgefangen wird
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception exception = (Exception) args.ExceptionObject;

            Log.Error(exception, "Unhandled exception accourced");
        }
        #endregion
    }
}