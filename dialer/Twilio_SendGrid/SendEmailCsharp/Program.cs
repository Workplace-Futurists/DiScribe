using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SendEmailCsharp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Console.WriteLine("Hello World!");
            Execute().Wait();
        }

        static async Task Execute()
        {
            // Set Environment Variable before calling them
            // Environment.SetEnvironmentVariable("SENDGRID_API_KEY_TEST", "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");
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY_TEST");
            var client = new SendGridClient(apiKey);

            // email address and the name of the sender and receiver
            var from = new EmailAddress("seungwook.l95@gmail.com", "SenderKevin");
            var tos = new List<EmailAddress>
            {
                new EmailAddress("seungwook.l95@gmail.com", "ReceiverKevin1"),
                new EmailAddress("tmdenddl@hanmail.com", "ReceiverKevin2")
            };

            // Subject Line of the email
            var subject = "Sending with Twilio SendGrid";

            // Content of the email sent
            var plainTextContent = "This is the conent of the trial version of email";

            // Set to true if you want the recipients to see each others email addresses
            var showAllRecipients = false;

            // Don't really know what this is
            var htmlContent = "<strong>C# was used for this</strong>";
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                from,
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
