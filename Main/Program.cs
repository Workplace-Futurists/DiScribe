using System;
using System.Collections.Generic;
using Transcriber.TranscribeAgent;
using SendGrid.Helpers.Mail;
using System.IO;

namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO dial in
            // TODO record the meeting
            // TODO download the recording
            // transcribe the meeting
            FileInfo pseudo_recording = new FileInfo(@"../../../../Record/MultipleSpeakers.wav");
            var voiceprints = Transcriber.TranscribeAgent.Program.MakeTestVoiceprints(pseudo_recording);
            var controller = new TranscribeController(pseudo_recording, voiceprints);
            controller.EnrollVoiceProfiles();

            // send meeting minutes to recipients
            // TODO how are we going to know the recipients
            var recipients = new List<EmailAddress>
            {
                new EmailAddress("jinhuang696@gmail.com", "Gmail")
            };

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
