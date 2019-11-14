using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Environment;

namespace Example
{
    internal class Example
    {
        private static void Main()
        {
            Execute().Wait();
        }

        static async Task Execute()
        {
            System.Environment.SetEnvironmentVariable("SENDGRID_API_KEY", "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");
            var apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            Console.WriteLine(apiKey);
            var client = new SendGridClient(apiKey);
            // var client = new SendGridClient("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");

            var from = new EmailAddress("tmdenddl@hanmail.net", "SenderKevin");
            var tos = new List<EmailAddress>
            {
                new EmailAddress("seungwook@gmail.com", "Receiver Kevin"),
                new EmailAddress("tmdenddl@hanmail.net", "Receiver Kevin"),
            };
            var subject = "Testing SendGrid";
            var plainTextContent = "YAY! IT WORKED!";
            var htmlContent = "<strong>NOW WORK ON INTEGRATION</strong>";
            var showAllRecipients = false; // Set to true if you want the recipients to see each others email addresses

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from,
                                                                       tos,
                                                                       subject,
                                                                       plainTextContent,
                                                                       htmlContent,
                                                                       showAllRecipients
                                                                       );
            var response = await client.SendEmailAsync(msg);
        }
    }
}