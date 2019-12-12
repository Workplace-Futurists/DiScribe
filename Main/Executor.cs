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

            /*Main application loop */
            while (true)
            {
                Console.WriteLine(">\tBot is Listening for meeting invites...");

                try
                {
                    await ListenForInvitations(appConfig);

                }
                 catch (AggregateException exs)
                {
                    foreach (var ex in exs.InnerExceptions)
                    {
                        Console.Error.WriteLine($">\t{ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Listens for a new WebEx invitation to the DiScribe bot email account.
        /// Logic:
        ///     -> Every 10 seconds, read events
        ///     -> Call Webex API to create meeting and get the meeting access code
        ///             
        ///     -> Schedule the rest of the dialer-transcriber workflow to dial in to meeting at the specified time
        ///
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private static async Task ListenForInvitations(IConfigurationRoot appConfig, int seconds = 10)
        {

            MeetingInfo meetingInfo;
            Microsoft.Graph.Event inviteEvent = null;

            
            try
            {
               
                try
                {
                    /*Attempt to get. latest event from bot's Outlook account. 
                     If there are no events, nothing will be scheduled. */
                    inviteEvent = await EmailListener.GetEventAsync();
                    
                    
                }
                catch (Exception readMessageEx)
                {
                    //EmailListener.DeleteEmailAsync(message).Wait();
                    throw new Exception("No meeting invites read from Outlook inbox. Reason: " + readMessageEx.Message);
                    
                }

                /*Handle the invite.
                  Assign the returned meeting info about the scheduled meeting */
                meetingInfo = HandleInvite(inviteEvent, appConfig);


                Console.WriteLine($">\tNew Meeting Found at: {meetingInfo.StartTime.ToLocalTime()}");

                /*Send an audio registration email enabling all unregistered users to enroll on DiScribe website */
                //MeetingController.SendEmailsToAnyUnregisteredUsers(meetingInfo.AttendeesEmails, appConfig["DB_CONN_STR"]);

                /*Send an email to meeting host and any delegate enabling Webex meeting start */
                //EmailSender.SendEmailForStartURL(meetingInfo);

                //Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meetingInfo.StartTime}");

                //await SchedulerController.Schedule(Run,
                    //meetingInfo, appConfig, meetingInfo.StartTime);                    //Schedule dialer-transcriber workflow as separate task
            }
          
            finally
            {
                EmailListener.DeleteEventAsync(inviteEvent).Wait();                        //Deletes the event that was read

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




        private static Meeting.MeetingInfo HandleInvite(Microsoft.Graph.Event inviteEvent, IConfigurationRoot appConfig)
        {
            MeetingInfo meetingInfo = new MeetingInfo();

            if (EmailListener.IsValidWebexInvitation(inviteEvent))
            {
                meetingInfo = EmailListener.GetMeetingInfoFromWebexInvite(inviteEvent, appConfig);
            }

            else
            {
                WebexHostInfo hostInfo = new WebexHostInfo(appConfig["WEBEX_EMAIL"],
                 appConfig["WEBEX_PW"], appConfig["WEBEX_ID"], appConfig["WEBEX_COMPANY"]);

                var something = inviteEvent.Start;

                inviteEvent.Start.TimeZone = System.TimeZoneInfo.Local.ToString();             //Set timezone of meeting start to local system time of bot
                inviteEvent.End.TimeZone = System.TimeZoneInfo.Local.ToString();                  

                string meetingStart = inviteEvent.Start.DateTime;                               //Get this start time in local time.
                string meetingEnd = inviteEvent.End.DateTime;

                string meetingDuration = (DateTime.Parse(meetingEnd).Subtract(DateTime.Parse(meetingStart))).ToString();          //Calc meeting duration.


                meetingInfo = MeetingController.CreateWebexMeeting(inviteEvent.Subject, EmailListener.GetAttendeeEmails(inviteEvent),
                EmailListener.GetAttendeeNames(inviteEvent), meetingStart, meetingDuration, hostInfo);

            }


            return meetingInfo;
                

           // return meetingInfo;






        }




                                    

    }
}
