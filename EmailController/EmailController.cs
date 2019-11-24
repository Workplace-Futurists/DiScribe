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
    public static class EmailController
    {
        // TODO we need one maybe?
        private static readonly EmailAddress OFFICIAL_EMAIL = new EmailAddress("workplace-futurists@hotmail.com", "Workplace Futurists");

        static IConfigurationRoot LoadAppSettings()
        {
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory().Replace("bin/Debug/netcoreapp3.0", ""))
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
            SendGridHelper.SendMinuteEmail(OFFICIAL_EMAIL, recipients, subject, minutes).Wait();
        }

        public static void SendMail(List<EmailAddress> recipients, string subject, FileInfo file = null)
        {
            SendGridHelper.SendMinuteEmail(OFFICIAL_EMAIL, recipients, subject, file).Wait();
        }

        public static List<EmailAddress> GetAttendeeEmails(string accessCode)
        {
            return XMLHelper.GetAttendeeEmails(accessCode);
        }

        public static List<string> GetAttendeeEmailsAsString(List<EmailAddress> emails)
        {
            List<string> emailsAsString = new List<String>();
            foreach (EmailAddress email in emails)
            {
                emailsAsString.Add(email.Email);
            }
            return emailsAsString;
        }

        // SpeakerRegistration -> CheckProfileExists(string email)
        public static void SendEmailForVoiceRegistration(List<EmailAddress> emails)
        {
            foreach (EmailAddress email in emails)
            {
                bool profileExist = false;

                //if (SpeakerRegistration.CheckProfileExists(email.Email) != null)
                //  profileExist = true;

                if (!profileExist)
                    SendGridHelper.SendRegistrationEmail(OFFICIAL_EMAIL, email, "Voice Registration for your upcoming meeting").Wait();
            }
        }

    }
}

