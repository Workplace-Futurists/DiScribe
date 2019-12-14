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
                appConfig["mailUser"] // bot's email account
                ).Wait();

            MeetingController.BOT_EMAIL = appConfig["mailUser"];

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
                await CheckForGraphEvents(appConfig);

                await Task.Delay(delayInterval * 1000);                   //Resume listening
            }
        }

        /// <summary>
        /// Listens for a new WebEx invitation to the DiScribe bot email account.
        /// Logic:
        ///     -> Get most recent event
        ///     -> If invite event is for Webex invite then
        ///         - Parse email to get Webex access code
        ///         - Call Webex API to get meeting info using access code and host info
        ///
        ///    -> Else if invite is an Outlook meeting invite then
        ///        - Call Webex API to create a meeting and use the returned meeting info
        ///
        ///    -> Send emails to users with no registered voice profile
        ///    -> Send meeting to the organizer (delegated host) to allow them to start meeting
        ///    -> Schedule the rest of the dialer-transcriber workflow to dial in to meeting at the specified time
        ///
        /// </summary>
        /// <returns></returns>
        private static async Task CheckForGraphEvents(IConfigurationRoot appConfig)
        {

            MeetingInfo meetingInfo;
            Microsoft.Graph.Event inviteEvent;

            try
            {
                /*Attempt to get. latest event from bot's Outlook account.
                If there are no events, nothing will be scheduled. */
                inviteEvent = await EmailListener.GetEventAsync();
                await EmailListener.DeleteEventAsync(inviteEvent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not get any MS Graph events. Reason: {ex.Message}");
            }

            /*Handle the invite.
              Assign the returned meeting info about the scheduled meeting */
            meetingInfo = await MeetingController.HandleInvite(inviteEvent, appConfig);

            Console.WriteLine($">\tNew Meeting Found at: {meetingInfo.StartTime.ToLocalTime()}");

            /*Send an audio registration email enabling all unregistered users to enroll on DiScribe website */
            MeetingController.SendEmailsToAnyUnregisteredUsers(meetingInfo.AttendeesEmails, appConfig["DB_CONN_STR"]);

            /*Send an email to only meeting host and any delegate enabling Webex meeting start*/
            var organizerEmail = inviteEvent.Organizer.EmailAddress;
            EmailSender.SendEmailForStartURL(meetingInfo,
                new SendGrid.Helpers.Mail.EmailAddress(organizerEmail.Address, organizerEmail.Name));

            Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meetingInfo.StartTime}");

            SchedulerController.Schedule(Run,
                meetingInfo, appConfig, meetingInfo.StartTime).Wait();                    //Schedule dialer-transcriber workflow as separate task
        }

        /// <summary>
        /// Runs when DiScribe bot dials in to Webex meeting. Performs transcription and speaker
        /// recognition, and emails meeting transcript to all participants.
        /// </summary>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        static int Run(MeetingInfo meetingInfo, IConfigurationRoot appConfig)
        {
            try
            {
                // dialing & recording
                var rid = new DialerController(appConfig).CallMeetingAsync(meetingInfo.AccessCode).Result;

                var recording = new RecordingController(appConfig).DownloadRecordingAsync(rid).Result;

                // Make controller for accessing registered user profiles in Azure Speaker Recognition endpoint
                var regController = RegistrationController.BuildController(appConfig["dbConnectionString"],
                    EmailHelper.FromEmailAddressListToStringList(meetingInfo.AttendeesEmails), appConfig["SPEAKER_RECOGNITION_ID_KEY"]);

                // initializing the transcribe controller
                SpeechConfig speechConfig = SpeechConfig.FromSubscription(appConfig["SPEECH_RECOGNITION_KEY"], appConfig["SPEECH_RECOGNITION_LOCALE"]);
                var transcribeController = new TranscribeController(recording, regController.UserProfiles, speechConfig, appConfig["SPEAKER_RECOGNITION_ID_KEY"]);

                // Performs transcription and speaker recognition. If success, then send email minutes to all participants
                if (transcribeController.Perform())
                {
                    EmailSender.SendMinutes(meetingInfo, transcribeController.WriteTranscriptionFile(rid));
                    Console.WriteLine(">\tTask Complete!");
                    return 0;
                }
                else
                {
                    EmailSender.SendEmail(meetingInfo, $"Failed to Generate Meeting Minutes for {meetingInfo.Subject}");
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
            finally
            {
                try
                {
                    int max_size = 100;
                    Console.WriteLine("Recordings in total exceeds"
                        + max_size + "Mb in size. Removing Oldest Recording\n["
                        + DeleteOldestRecordingIfLargerThan(max_size) + "]");
                }
                catch (Exception)
                {
                    //
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
                filePath = (@"Record/");
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
