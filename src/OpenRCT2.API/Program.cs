using System;
using System.IO;
using System.Linq;
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
            var apiSection = config.GetSection("api");
            var apiConfig = apiSection.Get<ApiConfig>();
            var configFilePath = GetConfigFilePath();

            if (!apiSection.Exists())
            {
                Log.Logger.Warning(
                    "No 'api' configuration section was found. Expected file: {ConfigFilePath}. Expected keys: api.bind, api.baseUrl, api.passwordServerSalt, api.authTokenSecret.",
                    configFilePath);
            }
            else
            {
                var missingApiKeys = new[]
                {
                    (Key: "api.bind", Value: apiConfig?.Bind),
                    (Key: "api.passwordServerSalt", Value: apiConfig?.PasswordServerSalt),
                    (Key: "api.authTokenSecret", Value: apiConfig?.AuthTokenSecret)
                }
                .Where(x => string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.Key)
                .ToArray();

                if (missingApiKeys.Length > 0)
                {
                    Log.Logger.Warning(
                        "Missing API configuration values: {MissingKeys}. Expected file: {ConfigFilePath}.",
                        string.Join(", ", missingApiKeys),
                        configFilePath);
                }
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
            var configFilePath = GetConfigFilePath();
            if (Directory.Exists(configDirectory))
            {
                config
                    .SetBasePath(configDirectory)
                    .AddYamlFile(ConfigFileName, optional: true, reloadOnChange: true);

                if (!File.Exists(configFilePath))
                {
                    Log.Logger.Warning("Configuration directory exists, but config file was not found: {ConfigFilePath}", configFilePath);
                }
            }
            else
            {
                Log.Logger.Warning("Configuration directory does not exist: {ConfigDirectory}", configDirectory);
            }
            config.AddEnvironmentVariables();
            return config.Build();
        }

        private static string GetConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), ConfigFileName);
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
