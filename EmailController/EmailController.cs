using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmailController
{
    public class EmailController
    {
        static IConfigurationRoot LoadAppSettings()
        {
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            // Check for required settings
            if (string.IsNullOrEmpty(appConfig["SENDGRID_API_KEY"]))
            {
                return null;
            }
            return appConfig;
        }

        public static void Initialize()
        {
            var appConfig = LoadAppSettings();

            // Throws warning if no appsettings.json exists
            if (appConfig == null)
            {
                Console.WriteLine("Missing or invalid appsettings.json...exiting");
                return;
            }

            string sendGridAPI = appConfig["SENDGRID_API_KEY"];
            SendGridHelper.Initialize(sendGridAPI);
        }

        public static void SendMail()
        {
            SendGridHelper.SendEmail().Wait();
        }
    }
}
