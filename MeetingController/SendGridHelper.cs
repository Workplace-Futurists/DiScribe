using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace MeetingControllers
{
    public class SendGridHelper
    {
        private static SendGridClient sendGridClient;

        public static void Initialize(String SENDGRID_API_KEY)
        {
            sendGridClient = new SendGridClient(SENDGRID_API_KEY);
            Console.WriteLine(">\tEmail Client successfully created!");
        }

        public static async Task SendMinuteEmail(EmailAddress from, List<EmailAddress> recipients,
            string subject, FileInfo file)
        {
            var plainTextContent = "Workplace-Futurists";

            // TODO: Find a way to use class or sln from the above directory
            // var accessCode = Graph.GraphHelper.GetEmailMeetingNumAsync().Result;
            // var htmlContent = "<h2>Meeting information</h2><h4>Meeting Number: " + accessCode + "</h4>";
            var htmlContent = "<h2>Meeting information</h2><h4>Meeting Number: </h4>";

            var showAllRecipients = false; // Set to true if you want the recipients to see each others email addresses
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from,
                                                                       recipients,
                                                                       subject,
                                                                       plainTextContent,
                                                                       htmlContent,
                                                                       showAllRecipients
                                                                       );

            if (!File.Exists(file.FullName) || file == null)
            {
                Console.WriteLine(">\tminutes.txt does not exists");
                return;
            }
            var bytes = File.ReadAllBytes(file.FullName);
            var content = Convert.ToBase64String(bytes);
            msg.AddAttachment("attachment", content);

            await sendGridClient.SendEmailAsync(msg);
            Console.WriteLine(">\tEmail sent successfully");
        }

        public static async Task SendRegistrationEmail(EmailAddress from, EmailAddress recipient,
            string subject)
        {
            Console.WriteLine(">\tSending Emails to [" + recipient.Name + "]");
            var plainTextContent = "Workplace-Futurists";
            var defaultURL = "http://discribe-cs319.westus.cloudapp.azure.com/regaudio/Users/Create/";
            var registrationURL = defaultURL + recipient.Email;

            var htmlContent = "<h2>Please register your voice to Voice Registration Website</h2><h4>Link: ";
            htmlContent += registrationURL;
            htmlContent += "</h4>";

            var msg = MailHelper.CreateSingleEmail(from,
                                                    recipient,
                                                    subject,
                                                    plainTextContent,
                                                    htmlContent
                                                    );
            await sendGridClient.SendEmailAsync(msg);
            Console.WriteLine(">\tEmail sent successfully");
        }
    }
}
