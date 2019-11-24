using System;
using System.Collections.Generic;
using Transcriber.TranscribeAgent;
using SendGrid.Helpers.Mail;
using System.IO;
using twilio_caller;
using Microsoft.Extensions.Configuration;
using SpeakerRegistration.Data;
using SpeakerRegistration;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Main
{
    class Program
    {
        private static readonly string dbConnStr = "Server=tcp:dbcs319discribe.database.windows.net,1433;" +
             "Initial Catalog=db_cs319_discribe;" +
             "Persist Security Info=False;" +
             "User ID=obiermann;" +
             "Password=JKm3rQ~t9sBiemann;" +
             "MultipleActiveResultSets=True;" +
             "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        private static readonly string speakerIDSubKey = "7fb70665af5b4770a94bb097e15b8ae0";

        static void Main(string[] args)
        {
            /* Deserialize the meeting init data passed on command line. This includes the meeting access code,
             * and the list of participants
             */
            DiScribeInitData initData =  (DiScribeInitData)(JsonConvert.DeserializeObject(args[0]));



            ////-------------------------------------- 
            //// FOR TESTING 
            //List<string> names = new List<string>();
            //names.Add("Workplace-futurists");

            //List<string> emails = new List<string>();
            //names.Add("workplace-futurists@hotmail.com");

            //string startDate = "11/26/2019 18:00:00";
            //string duration = "30";
            ////-------------------------------------- 

            ////
            ////
            //string accessCode = EmailController.XMLHelper.CreateWebExMeeting(names, emails, startDate, duration);

            
            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();



            // new dialer manager
            var dialManager = new twilio_caller.dialer.dialerManager(appConfig);
            // new recording download manager
            var recManager = new twilio_caller.dialer.RecordingManager(appConfig);


            // dial into and record the meeting
            var dialTask = dialManager.CallMeetingAsync(initData.MeetingAccessCode);
            dialTask.Wait();
            var rid = dialTask.Result;

            // download the recording to the file
            var meetingDLTask = recManager.DownloadRecordingAsync(rid);
            meetingDLTask.Wait();
            FileInfo audioFileLoc = new FileInfo(meetingDLTask.Result);

            /*Load all the profiles by email address for registered users */
            RegistrationController regController = RegistrationController.BuildController(dbConnStr, new List<string>(initData.KnownParticipants), speakerIDSubKey);
            List<User> voiceprints = regController.UserProfiles;                    //Get loaded profiles.

            /*Do the transcription */
            var controller = new TranscribeController(audioFileLoc, voiceprints);
           
            // If Transcribing is done, send minutes to every attendees of the meeting
            Boolean success = controller.Perform();
            if (success)
            {
                controller.WriteTranscriptionFile();
                EmailController.EmailController.Initialize();
                //EmailController.EmailController.SendMinutes(recipients);
            }
            else
            {
                EmailController.EmailController.Initialize();
                //EmailController.EmailController.SendMail(recipients, "Failed To Generate Meeting Transcription");
            }

            Console.WriteLine(">\tTasks Complete!");
        }
    }
}
