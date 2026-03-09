using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenRCT2.API.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace OpenRCT2.API
{
    public class Program
    {
        private const string ConfigDirectory = ".openrct2";
        private const string ConfigFileName = "api.config.yml";

        public static int Main(string[] args)
        {
            Log.Logger = CreateLogger();
            try
            {
                Log.Information("Starting web host");
                BuildHost(args).Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static Logger CreateLogger()
        {
            var logConfig = new LoggerConfiguration();

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase))
            {
                logConfig.MinimumLevel.Debug();
            }
            else
            {
                logConfig.MinimumLevel.Information();
            }

            logConfig
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console();
            return logConfig.CreateLogger();
        }

        private static IHost BuildHost(string[] args)
        {
            // Build / load configuration
            var config = BuildConfiguration();
            var apiConfig = config
                .GetSection("api")
                .Get<ApiConfig>();

            if (apiConfig == null)
            {
                Log.Logger.Warning("No configuration found for api");
            }

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseConfiguration(config);

                    if (apiConfig?.Bind != null)
                    {
                        webBuilder.UseUrls(apiConfig.Bind);
                    }
                });

            return hostBuilder.Build();
        }

        private static IConfiguration BuildConfiguration()
        {
            var config = new ConfigurationBuilder();
            string configDirectory = GetConfigDirectory();
            if (Directory.Exists(configDirectory))
            {
                config
                    .SetBasePath(configDirectory)
                    .AddYamlFile(ConfigFileName, optional: true, reloadOnChange: true);
            }
            config.AddEnvironmentVariables();
            return config.Build();
        }

        private static string GetConfigDirectory()
        {
            string homeDirectory = Environment.GetEnvironmentVariable("HOME");
            if (String.IsNullOrEmpty(homeDirectory))
            {
                homeDirectory = Environment.GetEnvironmentVariable("HOMEDRIVE") +
                                Environment.GetEnvironmentVariable("HOMEPATH");
                if (String.IsNullOrEmpty(homeDirectory))
                {
                    homeDirectory = "~";
                }
            }
            string configDirectory = Path.Combine(homeDirectory, ConfigDirectory);
            return configDirectory;
        }
    }
}
