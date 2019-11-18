using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;
using transcriber.TranscribeAgent;
using System.Collections.Generic;
using transcriber.Data;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;
using NAudio.Wave;

namespace transcriber.TranscribeAgent
{
    public class Program
    {
        
        /* Subscription key for Azure SpeakerRecognition service. */
        private static readonly string SpeakerIDKey = "7fb70665af5b4770a94bb097e15b8ae0";

        private static readonly string SpeakerLocale = "en-us";

        public static readonly int SpeakerRecognitionApiInterval = 3000; //Min time allowed between requests to speaker recognition API.

        /* Creates an instance of a speech config with specified subscription key and service region
         * for Azure Speech Recognition service
         */
        private static readonly SpeechConfig SpeechConfig = SpeechConfig.FromSubscription("1558a08d9f6246ffaa1b31def4c2d85f", "centralus");

        private static readonly FileInfo TestRecording = new FileInfo(@"..\..\..\Record\MultipleSpeakers.wav");

        private static readonly FileInfo MeetingMinutes = new FileInfo(@"..\..\..\transcript\minutes.txt");



        public static void Main(string[] args)
        {
            var voiceprints = MakeTestVoiceprints(TestRecording);                   //Make a test set of voiceprint objects

            /*This TranscriptionInitData instance will be received from the Dialer in method call*/
            var initData = new TranscriptionInitData(TestRecording, voiceprints, "");

            /*Enroll speaker voice profiles with the Speaker Recognition API */
            SpeakerRegistration registration = new SpeakerRegistration(SpeakerIDKey, voiceprints, SpeakerLocale);

            Console.WriteLine(">\tChecking user voice profile enrollment...");
            registration.EnrollUsers().Wait();

            /*Setup the TranscribeController instance which manages the details of the transcription procedure */
            var controller = new TranscribeController(initData.MeetingRecording, initData.Voiceprints, SpeechConfig, SpeakerIDKey);

            /*Start the transcription of all audio segments to produce the meeting minutes file*/
            if (controller.Perform())
            {
                controller.WriteTranscriptionFile(MeetingMinutes);

                string emailSubject = "Meeting minutes for " + DateTime.Now.ToLocalTime().ToString();
                var emailer = new TranscriptionEmailer("someone@ubc.ca", MeetingMinutes);
                emailer.SendEmail(initData.TargetEmail, emailSubject);
            }

            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }

        

        

        /// <summary>
        /// Method for test purposes to get voice samples from a WAV file
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        private static List<Voiceprint> MakeTestVoiceprints(FileInfo audioFile)
        {
            /*Pre-registered profiles.*/
            Guid user1GUID = new Guid("87aed609-b072-4fc5-bca6-87f8caa6dea9");
            Guid user2GUID = new Guid("0135a034-f9dc-45ed-84e6-94f35caf4617");
            Guid user3GUID = new Guid("5483bd0c-55b7-457d-9924-a3b0e76096dd");
            Guid user4GUID = new Guid("c00243a8-cba8-4a64-910b-8a9973c6c9c6");

            /*Set result with List<Voiceprint> containing both voiceprint objects */
            User user1 = new User("Brian Kernighan", "B.Kernighan@example.com", 1);
            User user2 = new User("Janelle Shane", "J.Shane@example.com", 2);
            User user3 = new User("Nick Smith", "N.Smith@example.com", 3);
            User user4 = new User("Patrick Shyu", "P.Shyu@example.com", 4);


            /*Offsets identifying times */
            ulong user1StartOffset = 1 * 1000;
            ulong user1EndOffset = 49 * 1000;

            ulong user2StartOffset = 51 * 1000;
            ulong user2EndOffset = 100 * 1000;

            ulong user3StartOffset = 101 * 1000;
            ulong user3EndOffset = 148 * 1000;

            ulong user4StartOffset = 151 * 1000;
            ulong user4EndOffset = 198 * 1000;


            AudioFileSplitter splitter = new AudioFileSplitter(audioFile);

            var user1Audio = splitter.WriteWavToStream(user1StartOffset, user1EndOffset);
            var user2Audio = splitter.WriteWavToStream(user2StartOffset, user2EndOffset);
            var user3Audio = splitter.WriteWavToStream(user3StartOffset, user3EndOffset);
            var user4Audio = splitter.WriteWavToStream(user4StartOffset, user4EndOffset);


      

            List<Voiceprint> voiceprints = new List<Voiceprint>()
            {
                new Voiceprint(user1Audio, user1, user1GUID),
                new Voiceprint(user2Audio, user2, user2GUID),
                new Voiceprint(user3Audio, user3, user3GUID),
                new Voiceprint(user4Audio, user4, user4GUID)
            };

            return voiceprints;

        }








    }
}
