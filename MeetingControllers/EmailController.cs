using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MeetingControllers
{
    public static class EmailController
    {
        // TODO we need one maybe?
        private static readonly EmailAddress OFFICIAL_EMAIL = new EmailAddress("workplace-futurists@hotmail.com", "BOT Workplace Futurists");

        private static readonly SendGridClient sendGridClient = Initialize();

        static IConfigurationRoot LoadAppSettings()
        {
            // TODO a more rigid solution
            DirectoryInfo dir = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory().Replace("bin/Debug/netcoreapp3.0", ""));
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(dir.Parent.FullName)
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            // Check for required settings
            if (string.IsNullOrEmpty(appConfig["SENDGRID_API_KEY"]))
            {
                return null;
            }
            return appConfig;
        }

        private static SendGridClient Initialize()
        {
            var appConfig = LoadAppSettings();

            // Throws warning if no appsettings.json exists
            if (appConfig == null)
            {
                Console.WriteLine(">\tMissing or invalid appsettings.json!");
                return null;
            }

            string sendGridAPI = appConfig["SENDGRID_API_KEY"];

            return new SendGridClient(sendGridAPI);
        }

        public static void SendEMail(EmailAddress recipient, string subject, string htmlContent, FileInfo file = null)
        {
            SendEmailHelper(OFFICIAL_EMAIL, new List<EmailAddress> { recipient }, subject, htmlContent, file).Wait();
        }

        public static void SendEMail(List<EmailAddress> recipients, string subject, string htmlContent, FileInfo file = null)
        {
            SendEmailHelper(OFFICIAL_EMAIL, recipients, subject, htmlContent, file).Wait();
        }

        public static void SendMinutes(List<EmailAddress> recipients, FileInfo file, string meeting_info = "your recent meeting")
        {
            Console.WriteLine(">\tSending the Transcription Results to users...");
            string subject = "Meeting minutes of " + meeting_info;

            // TODO need the infos
            var htmlContent = "<h2>Meeting information</h2><h4>Meeting Number: </h4>";
            SendEMail(recipients, htmlContent, subject, file);
        }

        public static void SendEmailForVoiceRegistration(List<EmailAddress> emails)
        {
            Console.WriteLine(">\tSending Emails to Unregistered Users...");
            foreach (EmailAddress email in emails)
            {
                var defaultURL = "https://discribe-cs319.westus.cloudapp.azure.com/regaudio/Users/Create/";
                var registrationURL = defaultURL + email.Email;

                var htmlContent = "<h2>Please register your voice to Voice Registration Website</h2><h4>Link: ";
                htmlContent += registrationURL;
                htmlContent += "</h4>";
                SendEMail(email, "Voice Registration for Your Upcoming Meeting", htmlContent);
            }
        }

        private static async Task SendEmailHelper(EmailAddress from, List<EmailAddress> recipients,
            string subject, string htmlContent, FileInfo file)
        {
            var plainTextContent = "Workplace-Futurists";

            var showAllRecipients = false; // Set to true if you want the recipients to see each others email addresses
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from,
                                                                       recipients,
                                                                       subject,
                                                                       plainTextContent,
                                                                       htmlContent,
                                                                       showAllRecipients
                                                                       );
            if (file != null)
            {
                if (!File.Exists(file.FullName))
                {
                    Console.WriteLine(">\t" + file.Name + " does not exists");
                    return;
                }
                var bytes = File.ReadAllBytes(file.FullName);
                var content = Convert.ToBase64String(bytes);
                msg.AddAttachment("attachment", content);
            }

            await sendGridClient.SendEmailAsync(msg);
        }

        public static List<string> FromEmailAddressListToStringList(List<EmailAddress> emails)
        {
            List<string> emailsAsString = new List<String>();
            foreach (EmailAddress email in emails)
            {
                emailsAsString.Add(email.Email);
            }
            return emailsAsString;
        }

        public static List<EmailAddress> FromStringListToEmailAddressList(List<string> emails)
        {
            List<EmailAddress> emailsAsEmailAddress = new List<EmailAddress>();
            foreach (string email in emails)
            {
                emailsAsEmailAddress.Add(new EmailAddress(email));
            }
            return emailsAsEmailAddress;
        }
    }
}

