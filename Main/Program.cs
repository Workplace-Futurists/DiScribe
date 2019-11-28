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

        const bool RELEASE = true;


        public static void Main(string[] args)
        {
            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            if (appConfig == null)
            {
                Console.Error.WriteLine("Could not load appsetings");
                return;
            }

            /*Main application loop */
            while (true)
            {
                try
                {
                    ListenForInvitations(RELEASE, appConfig).Wait();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error in transcription task. Continuing listening for invitations...");
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
        /// <param name="release"></param>
        /// <returns></returns>
        private static async Task ListenForInvitations(Boolean release, IConfigurationRoot appConfig)
        {
            while (true)
            {
                Console.WriteLine("Bot is Listening for meeting invites...");

                await GraphHelper.Initialize(appConfig["appId"], appConfig["tenantId"], appConfig["clientSecret"], appConfig["mailUser"]);
                
                var message = await GraphHelper.GetEmailAsync();                                     //Get latest email from bot's inbox.
                               
                string accessCode = await GraphHelper.GetEmailMeetingNumAsync(message);               //Get access code from bot's invite email

                //await GraphHelper.DeleteEmailAsync(message);                                       //Delete the email in bot's inbox.

                //TODO: Lookup meeting start time in database instead
                //      OR from custom scheduling email from DiScribe web.
                //     Otherwise, only the webex host (person who scheduled the meeting)
                //     can use the bot due to authentication issues.
                
                DateTime meetingTime = MeetingController.GetMeetingTime(accessCode);                 //Meeting start time from meeting info.

                Console.WriteLine(meetingTime.ToLocalTime());
                
                MeetingController.SendEmailsToAnyUnregisteredUsers(MeetingController.GetAttendeeEmails(accessCode));
                
                await SchedulerController.Schedule(Run, accessCode, appConfig, meetingTime);            //Schedule dialer-transcriber workflow

                GraphHelper.DeleteEmailAsync(message).Wait();       // deletes the email that was read

                await Task.Delay(10000);

            }
        }

       






    }
}
