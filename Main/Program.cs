using System;
using System.Collections.Generic;
using Transcriber.TranscribeAgent;
using SpeakerRegistration;
using SendGrid.Helpers.Mail;
using System.IO;

namespace Main
{
    class Program
    {
        /*Temporary DB connection string. In production, this will be a different connection string. */
        public static readonly string dbConnectionStr = "Server=tcp:dbcs319discribe.database.windows.net,1433;" +
            "Initial Catalog=db_cs319_discribe;" +
            "Persist Security Info=False;User ID={your id};" +
            "Password={your_password};" +
            "MultipleActiveResultSets=True;" +
            "Encrypt=True;TrustServerCertificate=False;" +
            "Connection Timeout=30"; 
        
        static void Main(string[] args)
        {
            // TODO dial in
            // TODO record the meeting
            // TODO download the recording
            // transcribe the meeting
            FileInfo pseudo_recording = new FileInfo(@"../../../../Record/MultipleSpeakers.wav");

            /*Make fake voice profiles and register those users to test user registration
             *in SpeakerRegistration.RegistrationController */
            var voiceprints = Test.RegistrationTest.TestRegistration();

            TranscribeController transcribeController = new TranscribeController(pseudo_recording, voiceprints);
            
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
