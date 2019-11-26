using System;
using System.Collections.Generic;
using Transcriber;
using DatabaseController;
using DatabaseController.Data;
using SendGrid.Helpers.Mail;
using System.IO;
using twilio_caller;
using Scheduler;
using MeetingControllers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Main
{
    public static class Program
    {
        static void Main(string[] args)
        {
            //Deserialize the init data for dialing in to meeting 
            InitData init = JsonConvert.DeserializeObject<InitData>(args[0]);

            if (!init.Debug)
                Run(init.MeetingAccessCode);
        }        

        public static void Run(string accessCode)
        {
            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            // dialing & recording
            var rid = new DialerManager(appConfig).CallMeetingAsync(accessCode).Result;
            var recording = new RecordingManager(appConfig).DownloadRecordingAsync(rid).Result;

            LineBreak();

            // retrieving all attendees' emails as a List
            List<EmailAddress> invitedUsers = MeetingController.GetAttendeeEmails(accessCode);

            // Make controller for accessing registered user profiles in Azure Speaker Recognition endpoint
            var regController = RegistrationController.BuildController(
                EmailController.FromEmailAddressListToStringList(invitedUsers));

            // initializing the transcribe controller 
            var transcribeController = new TranscribeController(recording, regController.UserProfiles);

            LineBreak();

            // performs transcription and speaker recognition
            if (transcribeController.Perform())
            {
                // writes the transcription result if successful
                var file = transcribeController.WriteTranscriptionFile(rid);

                LineBreak();

                // sends email to all meeting attendees
                EmailController.SendMinutes(invitedUsers, file);
            }
            else
                EmailController.SendEmail(invitedUsers, "Failed To Generate Meeting Transcription", "");

            Console.WriteLine(">\tTask Complete!");
        }

        private static void LineBreak()
        {
            Console.Write("Press <return> to continue");
            Console.ReadLine();
        }
    }
}
