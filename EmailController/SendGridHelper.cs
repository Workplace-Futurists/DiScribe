using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace EmailController
{
    public class SendGridHelper
    {
        private static SendGridClient sendGridClient;

        public static void Initialize(String SENDGRID_API_KEY)
        {
            sendGridClient = new SendGridClient(SENDGRID_API_KEY);
            Console.WriteLine("SendGrid Client successfully created!");
        }

        public static async Task SendEmail(EmailAddress from, List<EmailAddress> recipients,
            string subject, FileInfo file)
        {
            var plainTextContent = "Workplace-Futurists";

            // TODO: Find a way to use class or sln from the above directory
            // var accessCode = Graph.GraphHelper.GetEmailMeetingNumAsync().Result;
            // var htmlContent = "<h2>Meeting information</h2><h4>Meeting Number: " + accessCode + "</h4>";
            var htmlContent = "<h2>Meeting information</h2><h4>Meeting Number: </h4>";

            var showAllRecipients = true; // Set to true if you want the recipients to see each others email addresses

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from,
                                                                       recipients,
                                                                       subject,
                                                                       plainTextContent,
                                                                       htmlContent,
                                                                       showAllRecipients
                                                                       );

            /*
            byte[] byteData = Encoding.ASCII.GetBytes(@"../../../transcriber/transcript/Minutes.txt");
            msg.Attachments = new List<SendGrid.Helpers.Mail.Attachment>
            {
                new SendGrid.Helpers.Mail.Attachment
                {
                    Content = Convert.ToBase64String(byteData),
                    Filename = "Minutes.txt",
                    Type = "txt/plain",
                    Disposition = "attachment"
                }
            };
            */

            var bytes = File.ReadAllBytes(file.FullName);
            var content = Convert.ToBase64String(bytes);
            msg.AddAttachment("attachment", content);

            await sendGridClient.SendEmailAsync(msg);
            Console.WriteLine(">\tEmail sent successfully");
        }
    }
}
