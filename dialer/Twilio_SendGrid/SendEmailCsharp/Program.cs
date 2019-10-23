using System;
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
            var apiKey = Environment.GetEnvironmentVariable("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");
            var client = new SendGridClient(apiKey);

            // email address and the name of the sender and receiver
            var from = new EmailAddress("seungwook.l95@gmail.com", "Kevin");
            var to = new EmailAddress("seungwook.l95@gmail.com", "Kevin");

            // Subject Line of the email
            var subject = "Sending with Twilio SendGrid";

            // Content of the email sent
            var plainTextContent = "This is the conent of the trial version of email";

            // Don't really know what this is
            var htmlContent = "<strong>C# was used for this</strong>";
            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent,
                htmlContent
                );

            var response = await client.SendEmailAsync(msg);
        }
    }
}
