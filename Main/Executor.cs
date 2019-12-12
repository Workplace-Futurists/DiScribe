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
using System.Collections.Generic;

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
        ///     -> Determine if this is an invite from DiScribe web (webex invite) or an template invite from Outlook
        ///          If invite is from DiScribe web then
        ///             Get meeting access code
        ///             Call Webex API to get meeting time and participant emails using access code
        ///          
        ///          else if invite is template invite from Outlook
        ///             Call Webex API to create meeting and get the meeting access code
        ///             
        ///     -> Schedule the rest of the dialer-transcriber workflow to dial in to meeting at the specified time
        ///
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private static async Task ListenForInvitations(IConfigurationRoot appConfig, int seconds = 10)
        {
            try
            {
               
                MeetingInfo meetingInfo;
                Microsoft.Graph.Message message = null;
                try
                {
                    /*Attempt to get. latest email from bot's inbox. 
                        * If inbox is empty, no meeting will be scheduled. */
                    message = EmailListener.GetEmailAsync().Result;

                }
                catch (Exception readMessageEx)
                {
                    EmailListener.DeleteEmailAsync(message).Wait();
                    Console.Error.WriteLine(">\tCould not get invite email. Reason: " + readMessageEx.Message);
                    throw new Exception("Unable to continue, as invite email acount not be read...");
                }

                /*Handle the invite. Accommodate both webex invites from DiScribe AND Outlook template invites.
                  Assign the returned meeting info about the scheduled meeting */
                meetingInfo = HandleInvite(message, appConfig);


                Console.WriteLine($">\tNew Meeting Found at: {meetingInfo.StartTime.ToLocalTime()}");

                /*Send an audio registration email enabling all unregistered users to enroll on DiScribe website */
                MeetingController.SendEmailsToAnyUnregisteredUsers(meetingInfo.AttendeesEmails, appConfig["DB_CONN_STR"]);

                /*Send an email to meeting host and any delegate enabling Webex meeting start */
                EmailSender.SendEmailForStartURL(meetingInfo);

                Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meetingInfo.StartTime}");

                await SchedulerController.Schedule(Run,
                    meetingInfo, appConfig, meetingInfo.StartTime);                    //Schedule dialer-transcriber workflow as separate task

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

        }




        private static Meeting.MeetingInfo HandleInvite(Microsoft.Graph.Message message, IConfigurationRoot appConfig)
        {

            MeetingInfo meetingInfo = null;

            /*If this is a webex-generated email, then parse it as such and
              call Webex API to get meeting time and participant emails using access code. */
            if (EmailListener.IsValidWebexInvitation(message))
            {
                meetingInfo = EmailListener.GetMeetingInfoFromWebexInvite(message, appConfig);
                meetingInfo.AttendeesEmails = MeetingController.GetAttendeeEmails(meetingInfo);
            }

            /*Handle email as a template email from Outlook. Parse email and then schedule
              the meeting at the requested time. */
            else
            {
                meetingInfo = EmailListener.GetMeetingInfoFromOutlookInvite(message);

                WebexHostInfo hostInfo = new WebexHostInfo(appConfig["WEBEX_EMAIL"],
                    appConfig["WEBEX_PW"], appConfig["WEBEX_ID"], appConfig["WEBEX_COMPANY"]);

                

                MeetingController.CreateWebexMeeting(meetingInfo.Subject, meetingInfo.Names,
                    meetingInfo.GetStringEmails(), meetingInfo.StartTime.ToString(), meetingInfo.GetDuration().ToString(), hostInfo);
              

            }

            return meetingInfo;






        }




                                    

    }
}
