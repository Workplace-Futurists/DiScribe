using System;
using System.Collections.Generic;
using transcriber.TranscribeAgent;
using EmailController;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO dial in
            // TODO record the meeting
            // TODO download the recording
            // TODO transcribe the meeting

            // send meeting minutes to recipients
            // TODO how are we going to know the recipients
            var recipients = new List<EmailAddress>
            {
                new EmailAddress("jinhuang696@gmail.com", "Gmail")
            };
            EmailController.EmailController.Initialize();
            EmailController.EmailController.SendMinutes(recipients);
        }
    }
}
