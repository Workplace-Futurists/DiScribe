using System;
using System.Threading.Tasks;
using System.IO;
using Transcriber.Audio;
using System.Collections.Generic;
using DatabaseController.Data;

using Transcriber;
using DatabaseController;

namespace Main.Test
{
    public class TranscriptionTest
    {
        private const string TestRecording = @"../../../../Record/MultipleSpeakers.wav";


        /// <summary>
        /// Method for test purposes to get voice samples from a WAV file
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public static List<User> TestTranscription(string recordingLoc = TestRecording)
        {
            Console.WriteLine(">\tGenerating Test Voice prints...");


            FileInfo recording = new FileInfo(recordingLoc);


            List<string> emails = new List<string>
           {
               "B.Kernighan@Example.com",
                "J.Shane@Example.com",
                "N.Smith@Example.com",
                "P.Shyu@Example.com"
           };

            /*Create registration controller where thare are no profiles loaded initially */
            RegistrationController regController =
                RegistrationController.BuildController(emails);



            var profiles = regController.UserProfiles;


            TranscribeController controller = new TranscribeController(recording, profiles);

            if(controller.Perform())                                //Perform the transcription.
            {
                controller.WriteTranscriptionFile();
            }


            return profiles;

            



        }
    }
}
