using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;
using transcriber.TranscribeAgent;
using System.Collections.Generic;

namespace transcriber.TranscribeAgent
{
    public class Program
    {

        public static void Main(string[] args)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("1558a08d9f6246ffaa1b31def4c2d85f", "centralus");

            FileInfo testRecording = new FileInfo(@"../../../Record/FakeMeetingShorter.wav");
            FileInfo meetingMinutes = new FileInfo(@"../../../transcript/Minutes.txt");

            /*This TranscriptionInitData instance will be received from the Dialer in method call
             * or pipe (if IPC is used)*/
            var initData = new TranscriptionInitData(testRecording, new List<Data.Voiceprint>(), "");

            /*Setup the TranscribeController instance which manages the details of the transcription procedure */
            var controller = new TranscribeController(config, initData.MeetingRecording, initData.Voiceprints, meetingMinutes);

            /*Start the transcription of all audio segments to produce the meeting minutes file*/
            Console.WriteLine("Creating transcript...");
            Boolean success = controller.DoTranscription();

            Boolean emailSent = false;

            if (success)
            {
                Console.WriteLine("\nTranscription completed");

                string emailSubject = "Meeting minutes for " + DateTime.Now.ToLocalTime().ToString();
                emailSent = controller.SendEmail(initData.TargetEmail, emailSubject);
            }

            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();

        }

    }
}
