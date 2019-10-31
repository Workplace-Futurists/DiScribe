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
            // Set Environment Variable before calling them
            Environment.SetEnvironmentVariable("SENDGRID_API_KEY", "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");
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
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
