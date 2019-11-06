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
    class Program
    {

        static void Main(string[] args)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("1558a08d9f6246ffaa1b31def4c2d85f", "centralus");
            //config.RequestWordLevelTimestamps(); //Kay: this is a useful method if want to track duration of each word, but not needed for now. 

            string path = @"../../../record/test_meeting.wav";
            FileInfo testRecording = new FileInfo(path);

            /*This TranscriptionInitData instance will be received from the Dialer bot process 
             * via a named pipe in when the two components are integrated. */
            var initData = new TranscriptionInitData(testRecording, new List<Data.Voiceprint>(), "");

            Console.WriteLine("Creating transcript...");

            /*Setup the TranscribeController instance which manages the details of the transcription procedure */
            var controller = new TranscribeController(config, initData.MeetingRecording, initData.Voiceprints, testRecording);

            /*Start the transcription of all audio segments to produce the meeting minutes file*/
            Boolean success = controller.DoTranscription();

            Boolean emailSent = false;

            if (success)
            {
                Console.WriteLine("Transcription completed");

                string emailSubject = "Meeting minutes for " + DateTime.Now.ToLocalTime().ToString();
                emailSent = controller.SendEmail(initData.TargetEmail, emailSubject);
            }

            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();

        }

    }
}
