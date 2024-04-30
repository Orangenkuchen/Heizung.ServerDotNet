namespace Heizung.ServerDotNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Heizung.ServerDotNet.Data;
    using Heizung.ServerDotNet.Mail;
    using Heizung.ServerDotNet.Service;
    using Heizung.ServerDotNet.SignalRHubs;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpsPolicy;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using MySqlConnector;
    using FluentMigrator.Runner;
    using FluentMigrator.Runner.Initialization;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Diese Klasse wird beim Starten des Webservers aufgerufen und konfiguriert diesen
    /// </summary>
    public class Startup
    {
        #region ctor
        /// <summary>
        /// Initialisiert die Klasse und setzt die Konfiguration
        /// </summary>
        /// <param name="configuration">Die Konfiguration welche verwendet werden soll</param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }
        #endregion
        
        #region Configuration
        /// <summary>
        /// Die Konfiguration, welche der Webserver verwendet
        /// </summary>
        /// <value>Wird beim Erstellen der Klasse gesetzt</value>
        public IConfiguration Configuration { get; }
        #endregion

        #region ConfigureServices
        /// <summary>
        /// Diese Methode wird zur Laufzeit aufgerufen. Kann benutzt werden, um Services zum Container hinzuzufügen.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            Serilog.Log.Debug("Konfiguriere die Services vom Webserver");

            var mailConfig = new MailConfiguration(
                this.Configuration["MailConfig:WarningMail:SmtpHostServer"], 
                new NetworkCredential(this.Configuration["MailConfig:WarningMail:UserName"], this.Configuration["MailConfig:WarningMail:UserPassword"]))
            {
                SmtpServerPort = this.Configuration.GetValue<uint>("MailConfig:WarningMail:SmtpHostPort")
            };

            services.AddSingleton<MailConfiguration>(mailConfig);
            
            services.AddFluentMigratorCore();
            services.ConfigureRunner(migrationRunnerBuilder => 
            {
                //migrationRunnerBuilder.AddSQLite();
                //migrationRunnerBuilder.WithGlobalConnectionString("Data Source=test.db");
                migrationRunnerBuilder.AddMySql5();
                migrationRunnerBuilder.WithGlobalConnectionString(this.Configuration.GetConnectionString("HeaterDatabase"));
                migrationRunnerBuilder.ScanIn(typeof(Migrations._0000_Empty).Assembly).For.Migrations();
            });

            services.AddSingleton<IHeaterRepository>(
                (serviceProvider) => {
                    return new HeaterRepository(this.Configuration.GetConnectionString("HeaterDatabase")!, serviceProvider.GetService<ILogger>()!);
                }
            );
            services.AddSingleton<IHeaterDataService, HeaterDataService>();

            services.AddCors();

            services.AddControllers()
                    .AddNewtonsoftJson(options => 
                    {
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    });
            services.AddSignalR();
            
            services.AddSwaggerGenNewtonsoftSupport();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Heizung.ServerDotNet", Version = "v1", });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
            });

            Serilog.Log.Debug("Konfigurieren der Services vom Webserver abgeschlossen");
        }
        #endregion

        #region Configure
        /// <summary>
        /// Diese Methode wird zu Laufzeit aufgerufen. Sie kann benutzt werden um die HTTP-Request pipline zu konfigurieren.
        /// </summary>
        /// <param name="app">ApplicationBuilder</param>
        /// <param name="env">WebHostEnvironment</param>
        /// <param name="migrationRunner">Fluentdatamigration-Runner</param>
        /// <param name="logger">Service für Lognachrichten</param>
        public void Configure(
            IApplicationBuilder app, 
            IWebHostEnvironment env, 
            IMigrationRunner migrationRunner,
            ILogger<Startup> logger)
        {
            logger.LogDebug("Configuration vom WebHost beginnt");

            migrationRunner.MigrateUp();

            if (env.IsDevelopment())
            {
                logger.LogDebug("Die Software wurde im 'Development'-Modus gestartet. Interne Serverfehler werden ausführlich dargestellt.");
                app.UseDeveloperExceptionPage();
            }
            
            logger.LogDebug("Konfiguriere Swagger und füge diesen zu den Endpunkten dazu");
            app.UseSwagger();
            app.UseSwaggerUI(c => 
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Heizung.ServerDotNet v1");
            });

            logger.LogDebug("Leitet HTTP-Anfragen auf HTTPS um");
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseAuthentication();

            app.UseCors(
                builder => 
                {
                    var allowedOrigins = this.Configuration.GetSection("AllowedCorsOrigins")
                                                            .GetChildren()
                                                            .Select((x) => x.Value)
                                                            .ToArray();

                    foreach(var element in allowedOrigins)
                    {
                        logger.LogDebug("Füge CORS für folgende Origins hinzu: {0}", element);
                    }
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials(); 
                }
            );

            logger.LogDebug("Füge API-Endpunkte hinzu");
            app.UseEndpoints(endpoints =>
            {
                logger.LogDebug("Füge alle API-Controller vom Projekt zur API hinzu");
                endpoints.MapControllers();

                var heaterDataHubAddress = "/HeaterDataHub";
                logger.LogDebug("Füge den Hub {0} unter dem API-Punkt {1} hinzu", nameof(HeaterDataHub), heaterDataHubAddress);
                endpoints.MapHub<HeaterDataHub>(heaterDataHubAddress);

                var heaterDataHubContext = app.ApplicationServices.GetService<IHubContext<HeaterDataHub>>();
                app.ApplicationServices.GetService<IHeaterDataService>().NewDataEvent += (currentHeaterDataDictionary) =>
                {
                    heaterDataHubContext.Clients.All.SendAsync("CurrentHeaterData", currentHeaterDataDictionary);
                };
            });
        }
        #endregion

    }
}