using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;
using Transcriber.TranscribeAgent;
using System.Collections.Generic;
using SpeakerRegistration.Data;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;
using NAudio.Wave;
using SpeakerRegistration;

namespace Transcriber.TranscribeAgent
{
    public class TranscribeTest
    {
        private static readonly FileInfo TestRecording = new FileInfo(@"../../../../Record/MultipleSpeakers.wav");

    

        /// <summary>
        /// Method for test purposes to get voice samples from a WAV file
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public static List<User> TestRegistration()
        {
            Console.WriteLine(">\tGenerating Test Voice prints...");

            /*Set result with List<Voiceprint> containing both voiceprint objects */
            string user1Email = "B.Kernighan@example.com";
            string user2Email = "J.Shane@example.com";
            string user3Email = "M.Tour@example.com";
            string user4Email = "P.Shyu@example.com";

         
            /*Create registration controller with initially empty set of users */
            RegistrationController regController = 
                RegistrationController.BuildController(dbConnStr, userEmails,



                );

            /*Pre-registered profiles.*/
            Guid user1GUID = new Guid("87aed609-b072-4fc5-bca6-87f8caa6dea9");
            Guid user2GUID = new Guid("0135a034-f9dc-45ed-84e6-94f35caf4617");
            Guid user3GUID = new Guid("5483bd0c-55b7-457d-9924-a3b0e76096dd");
            Guid user4GUID = new Guid("c00243a8-cba8-4a64-910b-8a9973c6c9c6");

            

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

            List<User> voiceprints = new List<User>()
            {
                new User(user1Audio, user1, user1GUID),
                new User(user2Audio, user2, user2GUID),
                new User(user3Audio, user3, user3GUID),
                new User(user4Audio, user4, user4GUID)
            };

            return voiceprints;
        }
    }
}
