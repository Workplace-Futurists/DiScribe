using System;
using System.Collections.Generic;
using Transcriber;
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
        static void Main(string[] args)
        {
            Run("628576562");
        }

        public static void Run(string accessCode)
        {// Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            // new dialer manager
            var dialManager = new twilio_caller.dialer.dialerManager(appConfig);
            // new recording download manager
            var recManager = new twilio_caller.dialer.RecordingManager(appConfig);

            // dial into and record the meeting
            var rid = dialManager.CallMeetingAsync(accessCode).Result;
            // download the recording to the file
            var recording = recManager.DownloadRecordingAsync(accessCode).Result;

            // send registration emails to whom did not register their voice into the system yet
            List<EmailAddress> recipients = MeetingController.GetAttendeeEmails(accessCode);

            // transcribe the meeting
            Console.WriteLine(">\tBeginning Transcribing...");

            /*Load all the profiles by email address for registered users */
            var emails = EmailController.FromEmailAddressListToStringList(recipients);
            RegistrationController regController = RegistrationController.BuildController(emails);
            List<User> voiceprints = regController.UserProfiles;

            TranscribeController transcribeController = new TranscribeController(recording, voiceprints);

            /*Do the transcription */
            if (transcribeController.Perform())
            {
                transcribeController.WriteTranscriptionFile();
                EmailController.SendMinutes(recipients);
            }
            else
            {
                EmailController.SendEMail(recipients, "Failed To Generate Meeting Transcription", "");
            }

            Console.WriteLine(">\tTasks Complete!");
        }
    }
}
