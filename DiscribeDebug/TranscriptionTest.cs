using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;
using System.Collections.Generic;
using DiScribe.DatabaseManager.Data;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;
using NAudio.Wave;
using DiScribe.Transcriber;
using DiScribe.Transcriber.Audio;

namespace DiScribe.DiscribeDebug
{
    public static class TrancriptionTest
    {
        const int SPEAKER_RECOGNITION_API_INTERVAL = 3000;                    //Min time allowed between requests to speaker recognition API.    

        public static void TestTranscription(string audioFileLoc)
        {
            /*Subscription key for Azure SpeakerRecognition service. */
            var speakerIDKey = "7fb70665af5b4770a94bb097e15b8ae0";

            FileInfo testRecording = new FileInfo(audioFileLoc);
            FileInfo meetingMinutes = new FileInfo(@"../transcript/minutes.txt");

            var voiceprints = MakeTestVoiceprints(testRecording);                   //Make a test set of voiceprint objects

            EnrollUsers(speakerIDKey, voiceprints).Wait();

            /*Setup the TranscribeController instance which manages the details of the transcription procedure */
            var controller = new TranscribeController(testRecording, voiceprints);

            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();

            // performs transcription and speaker recognition
            if (controller.Perform())
                controller.WriteTranscriptionFile();
        }

        /// <summary>
        /// Function which enrolls 2 users for testing purposes. In final system, enrollment will
        /// be done by users.
        /// </summary>
        /// <param name="speakerIDKey"></param>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public static async Task EnrollUsers(string speakerIDKey, List<User> voiceprints, string enrollmentLocale = "en-us")
        {
            /*Create REST client for enrolling users */
            SpeakerIdentificationServiceClient enrollmentClient = new SpeakerIdentificationServiceClient(speakerIDKey);

            /*Create new enrollment profile for each user */
            foreach (User curUser in voiceprints)
            {
                await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                var profileCreateTask = CreateUserProfile(enrollmentClient, curUser, enrollmentLocale);
                await profileCreateTask;
                curUser.ProfileGUID = profileCreateTask.Result;
            }

            var enrollmentTasks = new List<Task<OperationLocation>>();

            /*Start enrollment tasks for all user voiceprints */
            for (int i = 0; i < voiceprints.Count; i++)
            {
                await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                enrollmentTasks.Add(enrollmentClient.EnrollAsync(voiceprints[i].AudioStream,
                    voiceprints[i].ProfileGUID, true));
            }

            /*Async wait for all speaker voiceprints to be submitted in request for enrollment */
            await Task.WhenAll(enrollmentTasks.ToArray());

            /*Async wait for all enrollments to be in an enrolled state */
            await ConfirmEnrollment(enrollmentTasks, enrollmentClient);
        }

        /// <summary>
        /// Creates a new user profile for a User and returns the GUID for that profile. 
        /// In the full system, this method should include a check to find out
        /// if the user is already registered in persistent storage (i.e. database).
        /// </summary>
        /// <param name="client"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        public static async Task<Guid> CreateUserProfile(SpeakerIdentificationServiceClient client, User user, string locale = "en-us")
        {
            var taskComplete = new TaskCompletionSource<Guid>();

            var profileTask = client.CreateProfileAsync(locale);
            await profileTask;

            taskComplete.SetResult(profileTask.Result.ProfileId);

            return profileTask.Result.ProfileId;
        }

        /// <summary>
        /// Confirms that enrollment was successful for all the profiles
        /// associated with the enrollment tasks in enrollmentOps.
        /// </summary>
        /// <returns></returns>
        private static async Task ConfirmEnrollment(List<Task<OperationLocation>> enrollmentTasks, SpeakerIdentificationServiceClient enrollmentClient)
        {
            foreach (var curTask in enrollmentTasks)
            {
                Boolean done = false;
                do
                {
                    await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);

                    var enrollmentCheck = enrollmentClient.CheckEnrollmentStatusAsync(curTask.Result);
                    await enrollmentCheck;

                    /*Check that this profile is enrolled */
                    if (enrollmentCheck.Result.ProcessingResult.EnrollmentStatus == EnrollmentStatus.Enrolled)
                    {
                        done = true;
                    }
                } while (!done);
            }
        }

        /// <summary>
        /// Method for test purposes to get voice samples from a WAV file
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        private static List<User> MakeTestVoiceprints(FileInfo audioFile)
        {
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

            var format = new WaveFormat(16000, 16, 1);

            List<User> voiceprints = new List<User>()
            {
                 new User(user1Audio, "Brian", "Kernighan", "B.Kernighan@example.com"),
                 new User(user2Audio, "Janelle", "Shane", "J.Shane@example.com"),
                 new User(user3Audio, "Nick",  "Smith", "N.Smith@example.com"),
                 new User(user4Audio, "Patrick", "Shyu", "P.Shyu@example.com")
            };
            return voiceprints;
        }
    }
}


































