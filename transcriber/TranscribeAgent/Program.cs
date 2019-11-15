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

namespace transcriber.TranscribeAgent
{
    public class Program
    {

        public static void Main(string[] args)
        {
            /* Creates an instance of a speech config with specified subscription key and service region
               for Azure Speech Recognition service */
            
            var speechConfig = SpeechConfig.FromSubscription("1558a08d9f6246ffaa1b31def4c2d85f", "centralus");

            /*Subscription key for Azure SpeakerRecognition service. */
            var speakerIDKey = "7fb70665af5b4770a94bb097e15b8ae0";

            FileInfo testRecording = new FileInfo(@"../../../Record/FakeMeeting.wav");
            FileInfo meetingMinutes = new FileInfo(@"../../../transcript/Minutes.txt");


            /////For testing, enroll 2 users to get speaker profiles directly from the audio.
            var enrollTask = EnrollUsers(speakerIDKey, testRecording);

            enrollTask.Wait();

            var enrollResult = enrollTask.Result;                               //List of enrolled Voiceprints
                                   

            /*This TranscriptionInitData instance will be received from the Dialer in method call*/
            var initData = new TranscriptionInitData(testRecording, enrollResult, "");

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
        public static async Task<List<Voiceprint>> EnrollUsers(string speakerIDKey, FileInfo audioFile, string enrollmentLocale = "en-us")
        {
            TaskCompletionSource<List<Voiceprint>> taskCompletion = new TaskCompletionSource<List<Voiceprint>>();
            AudioFileSplitter splitter = new AudioFileSplitter(audioFile);
            List<Voiceprint> result = new List<Voiceprint>();

            /*Offsets identifying times */
            ulong user1StartOffset = 30 * 1000;
            ulong user1EndOffset = 60 * 1000;
            ulong user2StartOffset = 74 * 1000;
            ulong user2EndOffset = 88 * 1000;


            /*Get byte[] for both users */
            byte[] user1Audio = splitter.SplitAudioGetBuf(user1StartOffset, user1EndOffset);
            byte[] user2Audio = splitter.SplitAudioGetBuf(user2StartOffset, user2EndOffset);


            /*Get memory streams for section of audio containing each user */
            MemoryStream user1Stream = new MemoryStream(AudioFileSplitter.writeWavToBuf(user1Audio));
            MemoryStream user2Stream = new MemoryStream(AudioFileSplitter.writeWavToBuf(user2Audio));

            /*Create REST client for enrolling users */
            SpeakerIdentificationServiceClient enrollmentClient = new SpeakerIdentificationServiceClient(speakerIDKey);

            List<Task> taskList = new List<Task>();


            /*Make profiles for each user*/
            var profileTask1 = enrollmentClient.CreateProfileAsync(enrollmentLocale);
            var profileTask2 = enrollmentClient.CreateProfileAsync(enrollmentLocale);


            taskList.Add(profileTask1);
            taskList.Add(profileTask2);        
            await Task.WhenAll(taskList.ToArray());                                      //Asychronously wait for profiles to be created.

            /*Get GUID for each profile */
            Guid user1GUID = profileTask1.Result.ProfileId;
            Guid user2GUID = profileTask2.Result.ProfileId;

            taskList.Clear();

            taskList.Add(enrollmentClient.EnrollAsync(user1Stream, user1GUID, true));
            taskList.Add(enrollmentClient.EnrollAsync(user2Stream, user2GUID, true));
            await Task.WhenAll(taskList.ToArray());                                       //Asynchronously wait for all speakers to be enrolled


            /*Set result with List<Voiceprint> containing both voiceprint objects */
            User user1 = new User("Tom", "Tom@example.com", 1);
            User user2 = new User("Maya", "Maya@example.com", 2);

            result.Add(new Voiceprint(user1Audio, user1GUID, user1));
            result.Add(new Voiceprint(user2Audio, user2GUID, user2));
            taskCompletion.SetResult(result);

            return taskCompletion.Task.Result;

        }


    }
}
