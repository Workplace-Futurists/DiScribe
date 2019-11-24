using System;
using System.Collections.Generic;
using Transcriber.TranscribeAgent;
using SpeakerRegistration;
using SpeakerRegistration.Data;
using SendGrid.Helpers.Mail;
using System.IO;

namespace Main
{
    class Program
    {
        /*Temporary DB connection string. In production, this will be a different connection string. */
        public static readonly string dbConnectionStr = "Server=tcp:dbcs319discribe.database.windows.net,1433;" +
            "Initial Catalog=db_cs319_discribe;" +
            "Persist Security Info=False;User ID=obiermann;" +
            "Password=JKm3rQ~t9sBiemann;" +
            "MultipleActiveResultSets=True;" +
            "Encrypt=True;TrustServerCertificate=False;" +
            "Connection Timeout=30";

        private static readonly string speakerIDKeySub = "7fb70665af5b4770a94bb097e15b8ae0";


        static void Main(string[] args)
        {
            // TODO dial in
            // TODO record the meeting
            // TODO download the recording
            // transcribe the meeting

            Console.WriteLine(">\tBeginning transcriber...");
            /*List of users that are known to be in database*/
            List<string> knownEmails = new List<string> { "B.Kernighan@Example.com",
                "J.Shane@Example.com",
                "N.Smith@Example.com",
                "P.Shyu@Example.com" };

            FileInfo pseudo_recording = new FileInfo(@"../../../../Record/MultipleSpeakers.wav");

            /*Load all the profiles by email address for registered users */
            RegistrationController regController = RegistrationController.BuildController(dbConnectionStr, knownEmails, speakerIDKeySub);
            List<User> voiceprints = regController.UserProfiles;                      

            TranscribeController transcribeController = new TranscribeController(pseudo_recording, voiceprints) ;
            
            // send meeting minutes to recipients
            // TODO how are we going to know the recipients
            var recipients = new List<EmailAddress>
            {
                new EmailAddress("jinhuang696@gmail.com", "Gmail")
            };

            /*Do the transcription */
            Boolean success = transcribeController.Perform();



            if (success)
            {
                transcribeController.WriteTranscriptionFile();
                EmailController.EmailController.Initialize();
                EmailController.EmailController.SendMinutes(recipients);
            }
            else
            {
                EmailController.EmailController.Initialize();
                EmailController.EmailController.SendMail(recipients, "Failed To Generate Meeting Transcription");
            }

            Console.WriteLine(">\tTasks Complete!");
        }
    }
}
