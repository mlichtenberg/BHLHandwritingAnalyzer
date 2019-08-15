using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace BHLHandwritingAnalyzer
{
    public static class Config
    {
        public static string SubscriptionKey { get; set; }
        public static string Endpoint { get; set; }
        public static string BhlApiKey { get; set; }
        public static string InputFolder { get; set; }
        public static string OutputFolder { get; set; }
        public static string OriginalFolder { get; } 
        public static string NewFolder { get; } 
        public static string BhlPageTextUrl { get; set; }
        public static string BhlPageImageUrl { get; set; }
        public static int MaxRetryTimes { get; set; } = 3;
        public static TimeSpan QueryWaitTimeInSeconds { get; set; } = TimeSpan.FromSeconds(3);


        static Config()
        {
            // Load the appropriate config file, based on the ASPNETCORE_ENVIRONMENT environment variable setting
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string configFileName = "appsettings.json";
            if (!string.IsNullOrWhiteSpace(envName))
            {
                configFileName = string.Format($"appsettings.{envName}.json");
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFileName, optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();

            // Read the settings from the config file
            var appSettings = configuration.GetSection("appSettings");
            SubscriptionKey = appSettings.GetSection("computerVisionSubscriptionKey").Value;
            Endpoint = appSettings.GetSection("computerVisionEndpoint").Value;
            BhlApiKey = appSettings.GetSection("bhlApiKey").Value;
            InputFolder = appSettings.GetSection("inputFolder").Value;
            OutputFolder = appSettings.GetSection("outputFolder").Value;
            OriginalFolder = OutputFolder + "\\original";
            NewFolder = OutputFolder + "\\new";
            BhlPageTextUrl = appSettings.GetSection("bhlPageTextUrl").Value;
            BhlPageImageUrl = appSettings.GetSection("bhlPageImageUrl").Value;
        }
    }
}
