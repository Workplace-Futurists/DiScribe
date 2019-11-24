using System;
using System.Collections.Generic;
using Transcriber.TranscribeAgent;
using SendGrid.Helpers.Mail;
using System.IO;
using twilio_caller;
using Microsoft.Extensions.Configuration;

namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO retrieve accessCode correctly
            string accessCode = EmailController.GraphHelper.GetEmailMeetingNumAsync().Result;

            // send registration emails to whom did not register their voice into the system yet
            List<EmailAddress> recipients = EmailController.EmailController.GetAttendeeEmails(accessCode);
            EmailController.EmailController.SendEmailForVoiceRegistration(recipients);

            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            // new dialer manager
            var dialManager = new twilio_caller.dialer.dialerManager(appConfig);
            // new recording download manager
            var recManager = new twilio_caller.dialer.RecordingManager(appConfig);
            
            // dial into and record the meeting
            var rid = dialManager.CallMeetingAsync(accessCode).Result;
            // download the recording to the file
            recManager.DownloadRecordingHandler(rid);
            // transcribe the meeting
            FileInfo pseudo_recording = new FileInfo(@"../../../../Record/MultipleSpeakers.wav");
            var voiceprints = Transcriber.TranscribeAgent.Program.MakeTestVoiceprints(pseudo_recording);
            var controller = new TranscribeController(pseudo_recording, voiceprints);
            controller.EnrollVoiceProfiles();

            // If Transcribing is done, send minutes to every attendees of the meeting
            Boolean success = controller.Perform();
            if (success)
            {
                controller.WriteTranscriptionFile();
                EmailController.EmailController.Initialize();
                EmailController.EmailController.SendMinutes(recipients);
            }
            else
            {
                EmailController.EmailController.Initialize();
                EmailController.EmailController.SendMail(recipients, "Failed To Generate Meeting Transcription");
            }
 
            Console.WriteLine(">\tTasks Complete!");
        }
    }
}
