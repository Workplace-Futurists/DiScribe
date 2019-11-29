using System;
using System.Collections.Generic;
using DiScribe.Transcriber;
using DiScribe.DatabaseManager;
using DiScribe.DatabaseManager.Data;
using SendGrid.Helpers.Mail;
using System.IO;
using DiScribe.Dialer;
using DiScribe.MeetingManager;
using Microsoft.Extensions.Configuration;
using DiScribe.Scheduler;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace DiScribe.Main
{
    public static class Program
    {
        private static bool RELEASE;

        public static void Main(string[] args)
        {
            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            if (appConfig == null)
            {
                Console.Error.WriteLine("Could not load appsetings");
                return;
            }
            RELEASE = appConfig["RELEASE"] == "true";

            /*Main application loop */
            while (true)
            {
                try
                {
                    ListenForInvitations(appConfig).Wait();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Runs when DiScribe bot dials in to Webex meeting. Performs transcription and speaker
        /// recognition, and emails meeting transcript to all participants.
        /// </summary>
        /// <param name="accessCode"></param>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        public static int Run(string accessCode, IConfigurationRoot appConfig)
        {
            // dialing & recording
            var rid = new DialerController(appConfig).CallMeetingAsync(accessCode).Result;
            var recording = new RecordingController(appConfig).DownloadRecordingAsync(rid, RELEASE).Result;

            // retrieving all attendees' emails as a List
            var invitedUsers = MeetingController.GetAttendeeEmails(accessCode);

            // Make controller for accessing registered user profiles in Azure Speaker Recognition endpoint
            var regController = RegistrationController.BuildController(
                EmailController.FromEmailAddressListToStringList(invitedUsers));

            // initializing the transcribe controller 
            var transcribeController = new TranscribeController(recording, regController.UserProfiles);

            // performs transcription and speaker recognition
            if (transcribeController.Perform())
            {
                EmailController.SendMinutes(invitedUsers, transcribeController.WriteTranscriptionFile(rid, RELEASE));
                Console.WriteLine(">\tTask Complete!");
                return 0;
            }
            else
            {
                EmailController.SendEmail(invitedUsers, "Failed To Generate Meeting Transcription", "");
                Console.WriteLine(">\tFailed to generat!");
                return -1;
            }
        }

        /// <summary>
        /// Listens for a new WebEx invitation to the DiScribe bot email account.
        /// Logic:
        ///     -> Every 10 seconds, read inbox
        ///     -> If there is a message, get access code from it
        ///     -> Call webex API to get meeting time from access code
        ///     -> Schedule the rest of the dial to transcribe workflow
        ///         
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private static async Task ListenForInvitations(IConfigurationRoot appConfig, int seconds = 10)
        {
            Console.WriteLine("Bot is Listening for meeting invites...");

            await GraphHelper.Initialize(appConfig["appId"],
                appConfig["tenantId"], appConfig["clientSecret"], appConfig["mailUser"]);

            var message = GraphHelper.GetEmailAsync().Result; //Get latest email from bot's inbox.

            var meeting_info = GraphHelper.GetMeetingInfo(message); //Get access code from bot's invite email

            Console.WriteLine("New Meeting Found at: " +
                meeting_info.StartTime.ToLocalTime());

            MeetingController.SendEmailsToAnyUnregisteredUsers(
                MeetingController.GetAttendeeEmails(meeting_info.AccessCode));

            await SchedulerController.Schedule(Run,
                meeting_info.AccessCode, appConfig, meeting_info.StartTime);            //Schedule dialer-transcriber workflow

            GraphHelper.DeleteEmailAsync(message).Wait(); // deletes the email that was read

            await Task.Delay(seconds * 1000);
        }
    }
}
