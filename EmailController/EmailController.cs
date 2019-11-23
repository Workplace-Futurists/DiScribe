using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmailController
{
    public class EmailController
    {
        // TODO we need one maybe?
        private static EmailAddress OFFICIAL_EMAIL = new EmailAddress("workplace-futurists@hotmail.com", "Hotmail");

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

        public static void SendMinutes(List<EmailAddress> recipients, string meeting_info = "your recent meeting")
        {
            FileInfo minutes = new FileInfo(@"../../../../Transcripts/minutes.txt");
            string subject = "Meeting minutes of " + meeting_info;
            SendGridHelper.SendEmail(OFFICIAL_EMAIL, recipients, subject, minutes).Wait();
        }

        public static void SendMail(List<EmailAddress> recipients, string subject, FileInfo file = null)
        {
            SendGridHelper.SendEmail(OFFICIAL_EMAIL, recipients, subject, file).Wait();
        }

      }
}
