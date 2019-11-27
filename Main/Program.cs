using System;
using System.Collections.Generic;
using Transcriber;
using DatabaseManager;
using DatabaseManager.Data;
using SendGrid.Helpers.Mail;
using System.IO;
using Dialer;
using MeetingManager;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Main
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the meeting access code now: ");
            Run(Console.ReadLine());
        }        

        public static void Run(string accessCode, bool release = false)
        {
            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            // dialing & recording
            var rid = new DialerController(appConfig).CallMeetingAsync(accessCode).Result;
            var recording = new RecordingController(appConfig).DownloadRecordingAsync(rid, release).Result;

            // retrieving all attendees' emails as a List
            List<EmailAddress> invitedUsers = MeetingController.GetAttendeeEmails(accessCode);
            
            // Make controller for accessing registered user profiles in Azure Speaker Recognition endpoint
            var regController = RegistrationController.BuildController(
                EmailController.FromEmailAddressListToStringList(invitedUsers));

            // initializing the transcribe controller 
            var transcribeController = new TranscribeController(recording, regController.UserProfiles);

            // performs transcription and speaker recognition
            if (transcribeController.Perform())
                EmailController.SendMinutes(invitedUsers, transcribeController.WriteTranscriptionFile(rid, release));            
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
