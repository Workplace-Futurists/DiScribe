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

        private static SendGridClient sendGridClient = Initialize();

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
            //Kay: Please donot change it to read from Main appsettings here. Because it conflicts with the web Appsetting reader. 
            string sendGridAPI = "!!ADD SENDRGID API KEY HERE!!";
            OfficialEmail = new EmailAddress("!!ADD BOT OUTPUT EMAIL ADDRES HERE!!", "DiScribe Bot");
            RegUrl = "https://discribe-cs319.azurewebsites.net/regaudio/Users/Create/";

            return new SendGridClient(sendGridAPI);
        }


        /// <summary>
        /// Sends an email to a single recipient with the specified subject, html content and (optional) attachment.
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="htmlContent"></param>
        /// <param name="file"></param>
        public static void SendEmail(EmailAddress recipient, string subject, string htmlContent, FileInfo file = null)
        {
            SendEmailHelper(OfficialEmail, new List<EmailAddress> { recipient }, subject, htmlContent, file).Wait();
        }


        /// <summary>
        /// Sends an email to a list of recipients with the specified subject, html content, and (optional) attachment
        /// </summary>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="htmlContent"></param>
        /// <param name="file"></param>
        public static void SendEmail(List<EmailAddress> recipients, string subject, string htmlContent, FileInfo file = null)
        {
            if (recipients.Count > 0)
                SendEmailHelper(OfficialEmail, recipients, subject, htmlContent, file).Wait();
            else
                Console.Error.WriteLine(">\tWarning: No recipients were found");
        }


        /// <summary>
        /// Sends an email with the specified meeting info, subject, (optional) html content and (optional) attachement
        /// </summary>
        /// <param name="meetingInfo"></param>
        /// <param name="subject"></param>
        /// <param name="htmlContent"></param>
        /// <param name="file"></param>
        public static void SendEmail(MeetingInfo meetingInfo, string subject, string htmlContent = "", FileInfo file = null)
        {
            if (htmlContent.Equals(""))
                htmlContent = $"<h2>Meeting information</h2>\n<h4>Meeting Number: {meetingInfo.AccessCode}</h4>\n";
            SendEmailHelper(OfficialEmail, meetingInfo.AttendeesEmails, subject, htmlContent, file).Wait();
        }


        /// <summary>
        /// Sends a meeting minutes file to all attendees.
        /// </summary>
        /// <param name="meetingInfo"></param>
        /// <param name="file"></param>
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


        /// <summary>
        /// Sends an email for voice profile registration to a list of users
        /// </summary>
        /// <param name="emails"></param>
        public static void SendEmailForVoiceRegistration(List<EmailAddress> emails)
        {
            Console.WriteLine(">\tSending Emails to Unregistered Users...");
            if (emails.Count == 0)
                throw new Exception(">\tNo recipients were found");

            foreach (EmailAddress email in emails)
            {
                var defaultURL = RegUrl;
                var registrationURL = defaultURL + email.Email;

                Console.WriteLine(">\t\t-sending to " + email.Email);

                var htmlContent = "<h2>Please register your voice to Voice Registration Website(Recommend using Chrome)</h2><h4>Link: ";
                //htmlContent += "<a href=\""+ registrationURL + "\">"+ registrationURL + "</a>";
                htmlContent += registrationURL;
                htmlContent += "</h4>";
                SendEmail(email, "Voice Registration for Your Upcoming Meeting", htmlContent);
            }
        }



        /// <summary>
        /// Sends email to the meeting organizer to allow them to start the Webex meeting.
        /// </summary>
        /// <param name="organizer"></param>
        public static void SendEmailForStartURL(MeetingInfo meetingInfo, EmailAddress organizer)
        {
            Console.WriteLine(">\tSending Email to Meeting Host for Meeting Reminder...");
            if (organizer is null)
                throw new Exception(">\tNo recipient for start meeting start email was found");

            string startURL = XMLHelper.RetrieveStartUrl(meetingInfo.AccessCode);

            var htmlContent = "<h4>Dear "+ organizer.Name + ": </h4><br/><h2>When it is time, please copy past this link to Chrome to start the meeting: "+ meetingInfo.Subject + "</h2><h4>Link: ";
            //htmlContent += "<a href=\"" + startURL + "\">" + startURL + "</a>";
            htmlContent += startURL;
            htmlContent += "</h4>";
            SendEmail(organizer, "Link to Start Your Meeting - "+ meetingInfo.Subject, htmlContent);
            
        }




        /// <summary>
        /// Makes a call to the Sendgrid API in order to send an email with the specified recipient(s),
        /// subject, html content, and attachement. If the attachment is null, it is ignored.
        /// Otherwise a check if performed to ensure that the file exists, and it is attached if it exists.
        /// Otherwise method returns without sending.
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="htmlContent"></param>
        /// <param name="file"></param>
        /// <returns>Task representing this request to Sendgrid API</returns>
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
