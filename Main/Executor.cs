using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DiScribe.Email;
using DiScribe.Transcriber;
using DiScribe.DatabaseManager;
using DiScribe.Dialer;
using DiScribe.Meeting;
using DiScribe.Scheduler;


namespace DiScribe.Main
{
    static class Executor
    {
        public static void Execute()
        {
            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            EmailListener.Initialize(
                appConfig["appId"], //
                appConfig["tenantId"],
                appConfig["clientSecret"],
                appConfig["mailUser"] // bot's email account
                ).Wait();

            MeetingController.BOT_EMAIL = appConfig["mailUser"];

            /*Main application loop */
            while (true)
            {
                ListenForInvitations(appConfig).Wait();
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
            Console.WriteLine(">\tBot is Listening for meeting invites...");

            try
            {
                /*Attempt latest email from bot's inbox every 3 seconds. 
                 * If inbox is empty, no meeting will be scheduled. */
                var message = EmailListener.GetEmailAsync().Result;                      

                if (!EmailListener.IsValidWebexInvitation(message))
                {
                    EmailListener.DeleteEmailAsync(message).Wait();                     //Deletes the email that was read if it is invalid
                    throw new Exception(">\tNot a valid WebEx Invitation. Deleting Email...");
                }

                var meeting_info = EmailListener.GetMeetingInfo(message);               //Get access code from bot's invite email

                Console.WriteLine(">\tNew Meeting Found at: " +
                    meeting_info.StartTime.ToLocalTime());

                try
                {
                    var emails = MeetingController.GetAttendeeEmails(meeting_info.AccessCode);
                    
                    //MeetingController.SendEmailsToAnyUnregisteredUsers(
                     //   );
                } catch (Exception ex)
                {
                    Console.WriteLine($">\tNo emails sent to unregistered users. Reason: {ex.Message}");
                }


                Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meeting_info.StartTime}");

                await SchedulerController.Schedule(Run,
                    meeting_info.AccessCode, appConfig, meeting_info.StartTime);       //Schedule dialer-transcriber workflow as separate task

                EmailListener.DeleteEmailAsync(message).Wait();                        //Deletes the email that was read
            }
            catch (AggregateException exs)
            {
                foreach (var ex in exs.InnerExceptions)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }

            await Task.Delay(seconds * 3000);            
        }

        /// <summary>
        /// Runs when DiScribe bot dials in to Webex meeting. Performs transcription and speaker
        /// recognition, and emails meeting transcript to all participants.
        /// </summary>
        /// <param name="accessCode"></param>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        static int Run(string accessCode, IConfigurationRoot appConfig)
        {
            try
            {
                // dialing & recording
                var rid = new DialerController(appConfig).CallMeetingAsync(accessCode).Result;
                var recording = new RecordingController(appConfig).DownloadRecordingAsync(rid).Result;

                // retrieving all attendees' emails as a List
                var invitedUsers = MeetingController.GetAttendeeEmails(accessCode);

                // Make controller for accessing registered user profiles in Azure Speaker Recognition endpoint
                var regController = RegistrationController.BuildController(
                    EmailHelper.FromEmailAddressListToStringList(invitedUsers));

                // initializing the transcribe controller
                var transcribeController = new TranscribeController(recording, regController.UserProfiles);

                // Performs transcription and speaker recognition. If success, then send email minutes to all participants
                if (transcribeController.Perform())
                {
                    EmailSender.SendMinutes(invitedUsers, transcribeController.WriteTranscriptionFile(rid), accessCode);
                    Console.WriteLine(">\tTask Complete!");
                    return 0;
                }
                else
                {
                    EmailSender.SendEmail(invitedUsers, "Failed To Generate Meeting Transcription", "");
                    Console.WriteLine(">\tFailed to generate!");
                    return -1;
                }
            }
            catch (AggregateException exs)
            {
                foreach (var ex in exs.InnerExceptions)
                {
                    Console.Error.WriteLine(ex.Message);
                }
                return -1;
            }
        }
    }
}
