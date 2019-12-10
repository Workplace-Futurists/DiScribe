using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using DiScribe.Meeting;


namespace DiScribe.Email
{
    public static class EmailSender
    {
        private static EmailAddress OfficialEmail;
        private static string RegUrl;

        private static readonly SendGridClient sendGridClient = Initialize();

        static IConfigurationRoot LoadAppSettings()
        {
            DirectoryInfo dir = new DirectoryInfo(
                System.IO.Directory.GetCurrentDirectory()
                .Replace("bin/Debug/netcoreapp3.0", ""));
            string basepath;
            if (dir.Parent.Name == "cs319-2019w1-hsbc")
                basepath = dir.Parent.FullName;
            else
                basepath = Directory.GetCurrentDirectory();
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(basepath)
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

            if (appConfig == null)
            {
                Console.WriteLine(">\tMissing or invalid appsettings.json!");
                return null;
            }

            string sendGridAPI = appConfig["SENDGRID_API_KEY"];
            OfficialEmail = new EmailAddress(appConfig["BOT_MAIL_ACCOUNT"], "DiScribe Bot");
            RegUrl = appConfig["DEFAULT_REG_URL"];

            //string sendGridAPI = "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";
            //OfficialEmail = new EmailAddress("levana@workplacefupurists.onmicrosoft.com", "DiScribe Bot");
            //RegUrl = "https://discribe-cs319.westus.cloudapp.azure.com/regaudio/Users/Create/";

            return new SendGridClient(sendGridAPI);
        }

        public static void SendEmail(EmailAddress recipient, string subject, string htmlContent, FileInfo file = null)
        {
            SendEmailHelper(OfficialEmail, new List<EmailAddress> { recipient }, subject, htmlContent, file).Wait();
        }

        public static void SendEmail(List<EmailAddress> recipients, string subject, string htmlContent, FileInfo file = null)
        {
            if (recipients.Count > 0)
                SendEmailHelper(OfficialEmail, recipients, subject, htmlContent, file).Wait();
            else
                Console.Error.WriteLine(">\tWarning: No recipients were found");
        }

        public static void SendEmail(MeetingInfo meetingInfo, string subject, string htmlContent = "", FileInfo file = null)
        {
            if (htmlContent.Equals(""))
                htmlContent = $"<h2>Meeting information</h2>\n<h4>Meeting Number: {meetingInfo.AccessCode}</h4>\n";
            SendEmailHelper(OfficialEmail, meetingInfo.AttendeesEmails, subject, htmlContent, file).Wait();
        }

        public static void SendMinutes(MeetingInfo meetingInfo, FileInfo file)
        {
            Console.WriteLine(">\tSending the Transcription Results to users...");
            foreach (var email in meetingInfo.AttendeesEmails)
            {
                Console.WriteLine(">\t-\t" + email.Email);
            }
            string subject = $"Meeting minutes of {meetingInfo.Subject}";

            // TODO need the infos
            var htmlContent = $"<h2>Meeting information</h2>\n<h4>Meeting Number: {meetingInfo.AccessCode}</h4>\n";
            SendEmail(meetingInfo.AttendeesEmails, subject, htmlContent, file);
        }

        public static void SendEmailForVoiceRegistration(List<EmailAddress> emails)
        {
            Console.WriteLine(">\tSending Emails to Unregistered Users...");
            if (emails.Count == 0)
                throw new Exception(">\tNo recipients were found");

            foreach (EmailAddress email in emails)
            {
                var defaultURL = RegUrl;
                var registrationURL = defaultURL + email.Email;

                var htmlContent = "<h2>Please register your voice to Voice Registration Website(Recommend using Chrome)</h2><h4>Link: ";
                htmlContent += "<a href=\""+ registrationURL + "\">"+ registrationURL + "</a>";
                htmlContent += "</h4>";
                SendEmail(email, "Voice Registration for Your Upcoming Meeting", htmlContent);
            }
        }

        public static void SendEmailForStartURL(MeetingInfo meetingInfo)
        {
            Console.WriteLine(">\tSending Emails to Users...");
            if (meetingInfo.AttendeesEmails.Count == 0)
                throw new Exception(">\tNo recipients were found");

            string startURL = XMLHelper.RetrieveStartUrl(meetingInfo.AccessCode);

            foreach (EmailAddress email in meetingInfo.AttendeesEmails)
            {
                var htmlContent = "<h2>When it is time, please click on this link to start the meeting: "+ meetingInfo.Subject + "</h2><h4>Link: ";
                htmlContent += "<a href=\"" + startURL + "\">" + startURL + "</a>";
                htmlContent += "</h4>";
                SendEmail(email, "Link to Start Your Meeting - "+ meetingInfo.Subject, htmlContent);
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
                msg.AddAttachment("attachment.txt", content);
            }

            await sendGridClient.SendEmailAsync(msg);
        }
    }
}

namespace DiScribe.Email
{
    public static class EmailHelper
    {
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
