using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Environment;
using System.Text;
using System.IO;

namespace twilio_caller.SendEmailCsharp
{
    public class SendEmailCsharp
    {
        private static SendGridClient sendGridClient;

        public static void Initialize(String SENDGRID_API_KEY)
        {
            sendGridClient = new SendGridClient(SENDGRID_API_KEY);
            Console.WriteLine("SendGrid Client successfully created!");
        }

        public static async Task sendEmail()
        {
            // var apiKey = Environment.GetEnvironmentVariable("SENDGRID_KEY_API");
            // String apiKey = "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";
            // var client = new SendGridClient(apiKey);

            var from = new EmailAddress("workplace-futurists@hotmail.com", "Workplace Futurists");
            var tos = new List<EmailAddress>
            {
                new EmailAddress("workplace-futurists@hotmail.com", "Hotmail"),
                new EmailAddress("seungwook.l95@gmail.com", "Gmail"),
                new EmailAddress("tmdenddl@hanmail.net", "Hanmail")
            };

            // TODO: Change the subject and content to match the meeting information
            var subject = "WebEx Meeting Minutes (Workplace-Futurists)";
            var plainTextContent = "Workplace-Futurists";

            Task<String> accessCode = Graph.GraphHelper.GetEmailMeetingNumAsync();
            var htmlContent = "<h2>Meeting information</h2><h4>Meeting Number: " + accessCode + "</h4>";
            var showAllRecipients = true; // Set to true if you want the recipients to see each others email addresses

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from,
                                                                       tos,
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
                    // TODO: Must change the name to match the meeting minutes
                    Filename = "Minutes.txt",
                    Type = "txt/plain",
                    Disposition = "attachment"
                }
            };
            */
            
            var bytes = File.ReadAllBytes("../../../cs319-2019w1-hsbc/transcriber/transcript/Minutes.txt");
            var file = Convert.ToBase64String(bytes);
            msg.AddAttachment("Minutes.txt", file);

            var response = await sendGridClient.SendEmailAsync(msg);
            Console.WriteLine("Meeting Minute was sent successfully!");
        }
    }
}
