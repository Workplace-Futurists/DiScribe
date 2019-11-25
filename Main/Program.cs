using System;
using System.Collections.Generic;
using Transcriber.TranscribeAgent;
using DatabaseController;
using DatabaseController.Data;
using SendGrid.Helpers.Mail;
using System.IO;
using twilio_caller;
using MeetingControllers;
using Microsoft.Extensions.Configuration;

namespace Main
{
    static class Program
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
            Run("628576562");
        }

        public static void Run(string accessCode)
        {
            EmailController.Initialize();
            // send registration emails to whom did not register their voice into the system yet
            List<EmailAddress> recipients = MeetingController.GetAttendeeEmails(accessCode);
            //EmailController.SendEmailForVoiceRegistration(recipients);

            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            // new dialer manager
            var dialManager = new twilio_caller.dialer.dialerManager(appConfig);
            // new recording download manager
            var recManager = new twilio_caller.dialer.RecordingManager(appConfig);

            // dial into and record the meeting
            var rid = dialManager.CallMeetingAsync(accessCode).Result;
            // download the recording to the file
            var recording = recManager.DownloadRecordingAsync(accessCode).Result;

            // transcribe the meeting
            Console.WriteLine(">\tBeginning Transcribing...");

            /*Load all the profiles by email address for registered users */
            RegistrationController regController = RegistrationController.BuildController(dbConnectionStr,
                EmailController.FromEmailAddressListToStringList(recipients), speakerIDKeySub);
            List<User> voiceprints = regController.UserProfiles;

            TranscribeController transcribeController = new TranscribeController(recording, voiceprints);

            /*Do the transcription */
            if (transcribeController.Perform())
            {
                transcribeController.WriteTranscriptionFile();
                EmailController.Initialize();
                EmailController.SendMinutes(recipients);
            }
            else
            {
                EmailController.Initialize();
                EmailController.SendMail(recipients, "Failed To Generate Meeting Transcription");
            }

            Console.WriteLine(">\tTasks Complete!");
        }
    }
}
