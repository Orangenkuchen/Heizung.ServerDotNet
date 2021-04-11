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

            services.AddSingleton<IHeaterRepository>(new HeaterRepository(this.Configuration.GetConnectionString("HeaterDatabase")));
            services.AddSingleton<IHeaterDataService, HeaterDataService>();

            services.AddCors();

            services.AddControllers();
            services.AddSignalR();

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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseCors(
                builder => 
                {
                    builder.AllowAnyOrigin();
                    builder.AllowAnyMethod();
                    builder.AllowAnyHeader(); 
                }
            );

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Heizung.ServerDotNet v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<HeaterDataHub>("/HeaterDataHub");

                var heaterDataHubContext = app.ApplicationServices.GetService<IHubContext<HeaterDataHub>>();
                app.ApplicationServices.GetService<IHeaterDataService>().NewDataEvent += (currentHeaterDataDictionary) =>
                {
                    heaterDataHubContext.Clients.All.SendCoreAsync("CurrentHeaterData", new object[] { currentHeaterDataDictionary });
                };
            });
        }
        #endregion
    }
}