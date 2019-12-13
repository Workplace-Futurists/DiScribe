﻿using System;
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
        public static void Execute()
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

            /*Main application loop */
            while (true)
            {
                Console.WriteLine(">\tBot is Listening for meeting invites...");

                try
                {
                    ListenForInvitations(appConfig).Wait();
                }
                catch (AggregateException errors)
                {
                    Console.Error.WriteLine($">\tError in listener. Reason: {errors.InnerException.Message} \tRestarting listener...");
                }
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
            try
            {
                /*Attempt latest email from bot's inbox every 3 seconds. 
                 * If inbox is empty, no meeting will be scheduled. */
                var message = EmailListener.GetEmailAsync().Result;

                MeetingInfo meetingInfo;
                try
                {
                    meetingInfo = EmailListener.GetMeetingInfo(message, appConfig);               //Get access code from bot's invite email                    messageRead = true;
                }
                catch (Exception readMessageEx)
                {
                    EmailListener.DeleteEmailAsync(message).Wait();
                    Console.Error.WriteLine(">\tCould not read invite email. Reason: " + readMessageEx.Message);
                    throw new Exception("Unable to continue, as invite email acoult not be read...");
                }
                Console.WriteLine(">\tNew Meeting Found at: " +
                    meetingInfo.StartTime.ToLocalTime());

                MeetingController.SendEmailsToAnyUnregisteredUsers(meetingInfo.AttendeesEmails, appConfig["DB_CONN_STR"]);

                EmailSender.SendEmailForStartURL(meetingInfo);

                Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meetingInfo.StartTime}");

                await SchedulerController.Schedule(Run,
                    meetingInfo, appConfig, meetingInfo.StartTime);       //Schedule dialer-transcriber workflow as separate task

                EmailListener.DeleteEmailAsync(message).Wait();                        //Deletes the email that was read

            }
            catch (AggregateException exs)
            {
                foreach (var ex in exs.InnerExceptions)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }

            await Task.Delay(seconds * 1000);
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
                        + DeleteOldestRecordingIfLargerThan(max_size) +"]");
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
