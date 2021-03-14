using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Heizung.ServerDotNet
{
    /// <summary>
    /// Diese Klasse wird beim Starten des Webservers aufgerufen und konfiguriert diesen
    /// </summary>
    public class Startup
    {
        #region fields
        /// <summary>
        /// Die Konfiguration, welche der Webserver verwendet
        /// </summary>
        /// <value>Wird beim Erstellen der Klasse gesetzt</value>
        public IConfiguration Configuration { get; }
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert die Klasse und setzt die Konfiguration
        /// </summary>
        /// <param name="configuration">Die Konfiguration welche verwendet werden soll</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        #endregion

        #region ConfigureServices
        /// <summary>
        /// Diese Methode wird zur Laufzeit aufgerufen. Kann benutzt werden, um Services zum Container hinzuzufügen.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            Serilog.Log.Debug("Konfiguriere die Services vom Webserver");

            //services.AddSingleton;

            services.AddControllers();
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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Heizung.ServerDotNet v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        #endregion
    }
}