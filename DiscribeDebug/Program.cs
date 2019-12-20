using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DiScribe.DatabaseManager;
using DiScribe.DatabaseManager.Data;
using DiScribe.DiScribeDebug;
using DiScribe.Email;
using DiScribe.Meeting;
using Microsoft.ProjectOxford.SpeakerRecognition;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DiScribe.DiScribeDebug
{
    class Program
    {
        static void Main(string[] args)
        {

            TrancriptionTest.TestTranscription("MultipleSpeakers.wav");
            
            










        }


        static async Task Execute()
        {
            var apiKey = "";
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress("", "Example User");


            var subject = "Did it work?";

            var to = new EmailAddress("", "Example User");

            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            var responseBody = await response.DeserializeResponseBodyAsync(response.Body);

            if (responseBody != null)
            {
                foreach (var elem in responseBody)
                {
                    Console.WriteLine(elem.Value);

                }
            }
            
            var responseHeaders = response.DeserializeResponseHeaders(response.Headers);


            Console.WriteLine("\n\nHEADERS:");

            foreach (var header in responseHeaders)
            {
                Console.WriteLine(header.Value);

            }

            Console.WriteLine("\n\nRESPONSE CODE: " + response.StatusCode);

        }





    }
}
