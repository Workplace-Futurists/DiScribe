using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Environment;
using System.Text;

namespace SendEmailCsharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Execute().Wait();
        }

        static async Task Execute()
        {
            // var apiKey = Environment.GetEnvironmentVariable("SENDGRID_KEY_API");
            String apiKey = "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";
            Console.WriteLine("SENDGRID_API_KEY: " + apiKey);
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress("workplace-futurists@hotmail.com", "Workplace Futurists");
            var tos = new List<EmailAddress>
            {
                new EmailAddress("workplace-futurists@hotmail.com", "Hotmail"),
                new EmailAddress("seungwook.l95@gmail.com", "Gmail"),
                new EmailAddress("tmdenddl@hanmail.net", "Hanmail")
            };

            // TODO: Change the subject and content to match the meeting information
            var subject = "WebEx Meeting Minutes (Workplace-Futurists)";
            var plainTextContent = "Testing";
            var htmlContent = "<strong>Meeting information</strong>";
            var showAllRecipients = true; // Set to true if you want the recipients to see each others email addresses

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from,
                                                                       tos,
                                                                       subject,
                                                                       plainTextContent,
                                                                       htmlContent,
                                                                       showAllRecipients
                                                                       );

            // Add attachment as txt/plain
            byte[] byteData = Encoding.ASCII.GetBytes("file.txt");
            msg.Attachments = new List<SendGrid.Helpers.Mail.Attachment>
            {
                new SendGrid.Helpers.Mail.Attachment
                {
                    Content = Convert.ToBase64String(byteData),
                    // TODO: Must change the name to match the meeting minutes
                    Filename = "file.txt",
                    Type = "txt/plain",
                    Disposition = "attachment"
                }
            };

            var response = await client.SendEmailAsync(msg);
        }

    }
}


// using System;

// namespace SendEmailCsharp
// {
//     class Program
//     {
//         static void Main(string[] args)
//         {
//             ExecuteManualAttachmentAdd().Wait();
//             ExecuteStreamAttachmentAdd().Wait();
//         }

//         static async Task ExecuteManualAttachmentAdd()
//         {
//             var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
//             Console.WriteLine("SENDGRID_API_KEY: " + apiKey);
//             var client = new SendGridClient(apiKey);

//             var from = new EmailAddress("workplace-futurists@hotmail.com");
//             var subject = "Subject";
//             var to = new EmailAddress("seungwook.l95@gmail.com");
//             var body = "Email Body";
//             var msg = MailHelper.CreateSingleEmail(from, to, subject, body, "");
//             var bytes = File.ReadAllBytes("file.txt");
//             var file = Convert.ToBase64String(bytes);
//             msg.AddAttachment("file.txt", file);
//             var response = await client.SendEmailAsync(msg);
//         }

//         static async Task ExecuteStreamAttachmentAdd()
//         {
//             var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
//             Console.WriteLine("SENDGRID_API_KEY: " + apiKey);
//             var client = new SendGridClient(apiKey);

//             var from = new EmailAddress("workplace-futurists@hotmail.com");
//             var subject = "Subject";
//             var to = new EmailAddress("seungwook.l95@gmail.com");
//             var body = "Email Body";
//             var msg = MailHelper.CreateSingleEmail(from, to, subject, body, "");

//             using (var fileStream = File.OpenRead("C:\\Users\\username\\file.txt"))
//             {
//                 await msg.AddAttachmentAsync("file.txt", fileStream);
//                 var response = await client.SendEmailAsync(msg);
//             }
//         }

//     }
// }
