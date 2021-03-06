﻿using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.SnapshotCollector;

namespace Microsoft.AspNetCore.ApplicationInsights.HostingStartup
{
    /// <summary>
    /// A dynamic Application Insights lightup experience
    /// </summary>
    public class ApplicationInsightsHostingStartup : IHostingStartup
    {
        private const string ApplicationInsightsSettingsFile = "ApplicationInsights.settings.json";

        /// <summary>
        /// Calls UseApplicationInsights
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.UseApplicationInsights();

            builder.ConfigureServices(InitializeServices);
        }

        /// <summary>
        /// Adds the Javascript <see cref="TagHelperComponent"/> to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> associated with the application.</param>
        private void InitializeServices(IServiceCollection services)
        {
            // Add SnapshotCollector telemetry processor.
            services.AddSingleton<ITelemetryProcessorFactory>(serviceProvider => new SnapshotCollectorTelemetryProcessorFactory(serviceProvider));

            services.AddSingleton<IStartupFilter, ApplicationInsightsLoggerStartupFilter>();
            services.AddSingleton<ITagHelperComponent, JavaScriptSnippetTagHelperComponent>();

            try
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    var settingsFile = Path.Combine(home, "site", "diagnostics", ApplicationInsightsSettingsFile);
                    var configurationBuilder = new ConfigurationBuilder()
                        .AddJsonFile(settingsFile, optional: true, reloadOnChange: true);
                    var configuration = configurationBuilder.Build();

                    services.AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")));

                    // Configure SnapshotCollector
                    services.Configure<SnapshotCollectorConfiguration>(configuration.GetSection(nameof(SnapshotCollectorConfiguration)));
                }
            }
            catch { }
        }
    }
}
