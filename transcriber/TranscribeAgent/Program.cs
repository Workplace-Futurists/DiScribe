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
        const int SPEAKER_RECOGNITION_API_INTERVAL = 3000;                    //Min time allowed between requests to speaker recognition API.    

        public static void Main(string[] args)
        {
            /* Creates an instance of a speech config with specified subscription key and service region
               for Azure Speech Recognition service */
            
            var speechConfig = SpeechConfig.FromSubscription("1558a08d9f6246ffaa1b31def4c2d85f", "centralus");

            /*Subscription key for Azure SpeakerRecognition service. */
            var speakerIDKey = "7fb70665af5b4770a94bb097e15b8ae0";

            FileInfo testRecording = new FileInfo(@"../../../Record/MultipleSpeakers.wav");
            FileInfo meetingMinutes = new FileInfo(@"../../../transcript/Minutes.txt");

            var voiceprints = MakeTestVoiceprints(testRecording);                   //Make a test set of voiceprint objects
            
            EnrollUsers(speakerIDKey, voiceprints).Wait();

            
            /*This TranscriptionInitData instance will be received from the Dialer in method call*/
            var initData = new TranscriptionInitData(testRecording, voiceprints, "");

            /*Setup the TranscribeController instance which manages the details of the transcription procedure */
            var controller = new TranscribeController(speechConfig, speakerIDKey, initData.MeetingRecording, initData.Voiceprints, meetingMinutes);
                                                                           

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


        /// <summary>
        /// Function which enrolls 2 users for testing purposes. In final system, enrollment will
        /// be done by users.
        /// </summary>
        /// <param name="speakerIDKey"></param>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public static async Task EnrollUsers(string speakerIDKey, List<Voiceprint> voiceprints, string enrollmentLocale = "en-us")
        {
            /*Create REST client for enrolling users */
            SpeakerIdentificationServiceClient enrollmentClient = new SpeakerIdentificationServiceClient(speakerIDKey);


            /*First check that all profiles in the voiceprint objects actually exist*/
            Profile[] existingProfiles = await enrollmentClient.GetProfilesAsync();

            for (int i = 0; i < voiceprints.Count; i++)
            {
                Boolean profileExists = false;

                int j = 0;
                while (!profileExists && j < existingProfiles.Length)
                {
                    if (voiceprints[i].UserGUID == existingProfiles[j].ProfileId)
                    {
                        profileExists = true;
                    }
                    else
                        j++;
                }

                /*Create a profile if the profile doesn't actually exist. Also change the
                 * profile ID in the voiceprint object to the new ID*/
                if (!profileExists)
                {
                    await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                    var profileCreateTask = CreateUserProfile(enrollmentClient, voiceprints[i].AssociatedUser, enrollmentLocale);
                    await profileCreateTask;
                    voiceprints[i].UserGUID = profileCreateTask.Result;
                }
            }

            var enrollmentTasks = new List<Task<OperationLocation>>();

            /*Start enrollment tasks for all user voiceprints */
            for (int i = 0; i < voiceprints.Count; i++)
            {
                await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                enrollmentTasks.Add(enrollmentClient.EnrollAsync(voiceprints[i].AudioStream,
                    voiceprints[i].UserGUID, true));
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

        




            /// <summary>
            /// Confirms that enrollment was successful for all the profiles
            /// associated with the enrollment tasks in enrollmentOps.
            /// </summary>
            /// <returns></returns>
        private static async Task ConfirmEnrollment(List<Task<OperationLocation>> enrollmentTasks, SpeakerIdentificationServiceClient enrollmentClient)
        {
            foreach(var curTask in enrollmentTasks)
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


    }
}
