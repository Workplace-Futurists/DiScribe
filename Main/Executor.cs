using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DiScribe.Email;
using DiScribe.Transcriber;
using DiScribe.DatabaseManager;
using DiScribe.Dialer;
using DiScribe.Meeting;
using DiScribe.Scheduler;
using Microsoft.CognitiveServices.Speech;
using System.IO;
using System.Linq;


namespace DiScribe.Main
{
    static class Executor
    {
        public static async Task Execute()
        {
            Console.WriteLine(">\tDiScribe Initializing...");

            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            EmailListener.Initialize(
                appConfig["appId"], //
                appConfig["tenantId"],
                appConfig["clientSecret"],
                appConfig["BOT_Inbox"] // bot's email account
                ).Wait();

            

            MeetingController.BOT_EMAIL = appConfig["BOT_Inbox"];

            int graphApiDelayInterval = int.Parse(appConfig["graphApiDelayInterval"]);

            /*Main application loop */
            while (true)
            {
                Console.WriteLine(">\tBot is Listening for meeting invites...");

                try
                {
                    StartInvitationListening(appConfig, graphApiDelayInterval).Wait();

                }
                catch (AggregateException exs)
                {
                    foreach (var ex in exs.InnerExceptions)
                    {
                        Console.Error.WriteLine($">\t{ex.Message}");
                    }
                }
                finally
                {
                    await Task.Delay(graphApiDelayInterval * 1000);
                }

            }
        }


        /// <summary>
        /// Listens for graph events. Exceptions will bubble up to caller.
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="delayInterval"></param>
        /// <returns></returns>
        private static async Task StartInvitationListening(IConfigurationRoot appConfig, int delayInterval)
        {
            while (true)
            {
                Console.WriteLine($">\tCheck for Graph Events ... ");
                await CheckForGraphEvents(appConfig);

                Console.WriteLine($">\tCheck for Emails ... ");
                await CheckForEmails(appConfig);

                await Task.Delay(delayInterval * 1000);                   //Resume listening
            }
        }


        /// <summary>
        /// Listens for a new WebEx invitation to the DiScribe bot email account.
        /// Logic:
        ///     -> Get most recent event
        ///
        ///    -> If invite is an Outlook meeting invite then
        ///        - Call Webex API to create a meeting and use the returned meeting info
        ///        
        ///    -> Else ignore event then    
        ///       -  Check bot inbox for any Webex invite emails
        ///       - If Webex email is detected, parse email to get access code
        ///       - Call webex API to get meeting metadata
        ///
        ///    -> Send emails to users with no registered voice profile
        ///    -> Send meeting to the organizer (delegated host) to allow them to start meeting
        ///    -> Schedule the rest of the dialer-transcriber workflow to dial in to meeting at the specified time
        ///
        /// </summary>
        /// <returns></returns>
        private static async Task<object?> CheckForGraphEvents(IConfigurationRoot appConfig)
        {

            MeetingInfo meetingInfo = null;
            Microsoft.Graph.Event inviteEvent;

            try
            {
                /*Attempt to get. latest event from bot's Outlook account.
                If there are no events, nothing will be scheduled. */
                inviteEvent = await EmailListener.GetEventAsync();
                await EmailListener.DeleteEventAsync(inviteEvent);
                 /*Handle the invite.
                  Assign the returned meeting info about the scheduled meeting or
                  null if this is not an Outlook invite*/
                  meetingInfo = await MeetingController.HandleInvite(inviteEvent, appConfig);
                
            }
            catch (Exception ex)
            {
                //throw new Exception($"Could not get any MS Graph events. Reason: {ex.Message}");
                Console.WriteLine($">\tCould not get any MS Graph events. Reason: {ex.Message}");
                return null;
            }

            if (meetingInfo != null)
            {

                Console.WriteLine($">\tNew Meeting Found at: {meetingInfo.StartTime}");

                /*Send an audio registration email enabling all unregistered users to enroll on DiScribe website */
                MeetingController.SendEmailsToAnyUnregisteredUsers(meetingInfo.AttendeesEmails, appConfig["DB_CONN_STR"]);


                Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meetingInfo.StartTime}");

                //Kay: According to Oloff, this should not have an "await" in front, otherwise it will wait until the meeting finish before checking the inbox again. 
                SchedulerController.Schedule(Run, meetingInfo, appConfig, meetingInfo.StartTime);//Schedule dialer-transcriber workflow as separate task
            }
            return meetingInfo;
        }

        private static async Task<object?> CheckForEmails(IConfigurationRoot appConfig)
        {
            MeetingInfo meetingInfo = null;


            try
            {          
              
                    var email = await EmailListener.GetEmailAsync();
                    meetingInfo = MeetingController.HandleEmail(email.Body.Content.ToString(), email.Subject, "", appConfig);
                    await EmailListener.DeleteEmailAsync(email);
                
            }
            catch (Exception emailEx)
            {
                Console.Error.WriteLine($">\tCould not read bot invite email. Reason: {emailEx.Message}");
                return null;
            }

            if (meetingInfo != null)
            {

                Console.WriteLine($">\tNew Meeting Found at: {meetingInfo.StartTime.ToLocalTime()}");

                /*Send an audio registration email enabling all unregistered users to enroll on DiScribe website */
                MeetingController.SendEmailsToAnyUnregisteredUsers(meetingInfo.AttendeesEmails, appConfig["DB_CONN_STR"]);


                Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meetingInfo.StartTime}");

                //Kay: According to Oloff, this should not have an "await" in front, otherwise it will wait until the meeting finish before checking the inbox again. 
                SchedulerController.Schedule(Run, meetingInfo, appConfig, meetingInfo.StartTime);//Schedule dialer-transcriber workflow as separate task
            }

            return meetingInfo;
        }



        /// <summary>
        /// Runs when DiScribe bot dials in to Webex meeting. Dials in to a Webex
        /// meeting and Performs transcription and speaker
        /// recognition. Then emails meeting transcript to all participants
        /// and updates the meeting record in DiScribe DB.
        /// </summary>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        static int Run(MeetingInfo meetingInfo, IConfigurationRoot appConfig)
        {
            try
            {
                /*dialing & recording */
                var rid = new DialerController(appConfig).CallMeetingAsync(meetingInfo.AccessCode).Result;

                var recording = new RecordingController(appConfig).DownloadRecordingAsync(rid).Result;

                /*Make controller for accessing registered user profiles in Azure Speaker Recognition endpoint */
                var regController = RegistrationController.BuildController(appConfig["DB_CONN_STR"],
                    EmailHelper.FromEmailAddressListToStringList(meetingInfo.AttendeesEmails), appConfig["SPEAKER_RECOGNITION_ID_KEY"]);

                /*initializing the transcribe controller */
                SpeechConfig speechConfig = SpeechConfig.FromSubscription(appConfig["SPEECH_RECOGNITION_KEY"], appConfig["SPEECH_RECOGNITION_LOCALE"]);
                var transcribeController = new TranscribeController(recording, regController.UserProfiles, speechConfig, appConfig["SPEAKER_RECOGNITION_ID_KEY"]);

                /*Performs transcription and speaker recognition. If success, then send email minutes to all participants */
                if (transcribeController.Perform())
                {
                    EmailSender.SendMinutes(meetingInfo, transcribeController.WriteTranscriptionFile(rid));
                    Console.WriteLine(">\tTask Complete!");
                }
                else
                {
                    EmailSender.SendEmail(meetingInfo, $"Failed to Generate Meeting Minutes for {meetingInfo.Subject}");
                    Console.WriteLine(">\tFailed to generate!");
                    return -1;
                }

                /*Set meeting minutes contents and file location in the Meeting object */
                meetingInfo.Meeting.MeetingFileLocation = transcribeController.MeetingMinutesFile.FullName;
                meetingInfo.Meeting.MeetingMinutes = transcribeController.Transcription;

                /*Sync meeting object with DiScribe DB */
                meetingInfo.Meeting.Update();                                                  



                return 0;


            }
            catch (AggregateException exs)
            {
                foreach (var ex in exs.InnerExceptions)
                {
                    Console.Error.WriteLine(ex.Message);
                }
                return -1;
            }
            finally
            {
                try
                {
                    int max_size = 100;
                    Console.WriteLine("Recordings in total exceeds "
                        + max_size + "Mb in size. Removing Oldest Recording\n["
                        + DeleteOldestRecordingIfLargerThan(max_size) + "]");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Could not remove oldest recording Reason: " + ex.Message);
                }
            }
        }



        /*  Deletes the oldest recording
         *  If the Record folder is larger than
         *  <param name="max_size"></param> Mb
         */
        static string DeleteOldestRecordingIfLargerThan(long max_size)
        {
            string directoryPath;
            #if (DEBUG)
                directoryPath = (@"../../../../Record/");
            #else
                directoryPath = (@"Record/");
            #endif

            long dir_size = 0;
            foreach (var file_ in new DirectoryInfo(directoryPath).GetFiles())
            {
                dir_size += file_.Length;
            }

            if (dir_size < max_size * 1024)
                throw new Exception();

            FileSystemInfo fileInfo = new DirectoryInfo(directoryPath).GetFileSystemInfos()
                    .OrderBy(fi => fi.CreationTime).First();
            var file = new FileInfo(fileInfo.FullName);
            file.Delete();
            return file.DirectoryName + "/" + file.Name;
        }
    }
}
