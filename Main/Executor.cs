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
            while(true)
            {
                await CheckForGraphEvents(appConfig);
                          

                await Task.Delay(delayInterval * 1000);
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
        /// <param name="seconds"></param>
        /// <returns></returns>
        private static async Task CheckForGraphEvents(IConfigurationRoot appConfig)
        {

            MeetingInfo meetingInfo;
            Microsoft.Graph.Event inviteEvent = null;


            try
            {
                 /*Attempt to get. latest event from bot's Outlook account. 
                 If there are no events, nothing will be scheduled. */
                inviteEvent = await EmailListener.GetEventAsync();


            } catch (Exception ex)
            {
                throw new Exception($"Could not get any MS Graph events. Reason: {ex.Message}");

            }

            finally
            {
                if (inviteEvent != null)
                   EmailListener.DeleteEventAsync(inviteEvent).Wait();                        //Deletes any matching event that was read.
            }

            /*Handle the invite.
              Assign the returned meeting info about the scheduled meeting */
            meetingInfo = HandleInvite(inviteEvent, appConfig);


            Console.WriteLine($">\tNew Meeting Found at: {meetingInfo.StartTime.ToLocalTime()}");

            /*Send an audio registration email enabling all unregistered users to enroll on DiScribe website */
            //MeetingController.SendEmailsToAnyUnregisteredUsers(meetingInfo.AttendeesEmails, appConfig["DB_CONN_STR"]);

            var organizerEmail = inviteEvent.Organizer.EmailAddress;

            /*Send an email to meeting host and any delegate enabling Webex meeting start*/
            EmailSender.SendEmailForStartURL(meetingInfo, 
                new SendGrid.Helpers.Mail.EmailAddress(organizerEmail.Address, organizerEmail.Name));

            //Console.WriteLine($">\tScheduling dialer to dial in to meeting at {meetingInfo.StartTime}");

            //SchedulerController.Schedule(Run,
            //meetingInfo, appConfig, meetingInfo.StartTime);                    //Schedule dialer-transcriber workflow as separate task


            

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



        /// <summary>
        /// Handles meeting invite and returns a MeetingInfo object representing the meeting.
        /// Separate cases for a Webex invite event and a
        /// </summary>
        /// <param name="inviteEvent"></param>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        private static Meeting.MeetingInfo HandleInvite(Microsoft.Graph.Event inviteEvent, IConfigurationRoot appConfig)
        {
            MeetingInfo meetingInfo = new MeetingInfo();

            /*If invite is a Webex email, parse email and use Webex API */
            if (EmailListener.IsValidWebexInvitation(inviteEvent))
            {
                meetingInfo = EmailListener.GetMeetingInfoFromWebexInvite(inviteEvent, appConfig);
            }

            else
            {
                WebexHostInfo hostInfo = new WebexHostInfo(appConfig["WEBEX_EMAIL"],
                 appConfig["WEBEX_PW"], appConfig["WEBEX_ID"], appConfig["WEBEX_COMPANY"]);

                var something = inviteEvent.Start;

                
                /*Get start and end time in UTC */
                DateTime meetingStartUTC = DateTime.Parse(inviteEvent.Start.DateTime);          
                DateTime meetingEndUTC = DateTime.Parse(inviteEvent.End.DateTime);

                /*Convert UTC start and end times to bot local system time */
                DateTime meetingStart = TimeZoneInfo.ConvertTimeFromUtc(meetingStartUTC, TimeZoneInfo.Local);
                DateTime meetingEnd = TimeZoneInfo.ConvertTimeFromUtc(meetingEndUTC, TimeZoneInfo.Local);

                var meetingDuration = meetingEnd.Subtract(meetingStart);
                string meetingDurationStr = meetingDuration.TotalMinutes.ToString();

                meetingInfo = MeetingController.CreateWebexMeeting(inviteEvent.Subject, EmailListener.GetAttendeeNames(inviteEvent),
                    EmailListener.GetAttendeeEmails(inviteEvent), meetingStart, 
                    meetingDurationStr, hostInfo, inviteEvent.Organizer.EmailAddress);

            }


            return meetingInfo;
                

           // return meetingInfo;






        }




                                    

    }
}
