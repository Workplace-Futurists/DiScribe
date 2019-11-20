using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using transcriber.Data;
using NAudio.Wave;


namespace transcriber.TranscribeAgent
{
    public class Recognizer
    {
        public Recognizer(TranscribeController controller)
        {
            Controller = controller;
        }

        private TranscribeController Controller;


        /// <summary>
        /// Performs speaker recognition on TranscriberOutputs to set
        /// the Speaker property.
        /// set set their User property representing the speaker.
        ///
        /// Note that apiDelayInterval allows the time between API requests in MS to be set.
        /// It is set to 3000 by default
        /// </summary>
        /// <param name="transcription"></param>
        /// <param name="apiDelayInterval"></param>
        public async Task DoSpeakerRecognition(SortedList<long, TranscriptionOutput> TranscriptionOutputs, int apiDelayInterval = 3000)
        {
            Console.WriteLine(">\tBegin Speaker Recognition...");
            var recognitionComplete = new TaskCompletionSource<int>();

            /*Create REST client for enrolling users */
            SpeakerIdentificationServiceClient idClient = new SpeakerIdentificationServiceClient(Controller.SpeakerIDSubKey);

            /*Dictionary for efficient voiceprint lookup by enrollment GUID*/
            Dictionary<Guid, Voiceprint> voiceprintDictionary = new Dictionary<Guid, Voiceprint>();
            Guid[] userIDs = new Guid[Controller.Voiceprints.Count];

            /*Add all voiceprints to the dictionary*/
            foreach (var voiceprint in Controller.Voiceprints)
            {
                voiceprintDictionary.Add(voiceprint.UserGUID, voiceprint);
            }

            voiceprintDictionary.Keys.CopyTo(userIDs, 0);                  //Hold GUIDs in userIDs array

            int p = 0;
            /*Iterate over each phrase and attempt to identify the user.
             Passes the audio data as a stream and the user GUID associated with the
             Azure SpeakerRecogniztion API profile to the API via the IdentifyAsync() method.
             Sets the User property in each TranscriptionOutput object in TrancriptionOutputs*/
            try
            {
                foreach (var curPhrase in TranscriptionOutputs)
                {
                    p++;
                    /*Write audio data in segment to a buffer containing wav file header */
                    byte[] wavBuf = AudioFileSplitter.WriteWavToBuf(curPhrase.Value.Segment.AudioData);

                    var format = new WaveFormat(16000, 16, 1);
                    using (WaveFileWriter testWriter = new WaveFileWriter($"{p}.wav", format))
                    {
                        testWriter.Write(wavBuf);
                    }

                        await Task.Delay(apiDelayInterval);

                    /*Create the task which submits the request to begin speaker recognition to the Speaker Recognition API.
                     Request contains the stream of this phrase and the GUIDs of users that may be present.*/
                    Task<OperationLocation> idTask = idClient.IdentifyAsync(new MemoryStream(wavBuf), userIDs, true);

                    await idTask;


                    var resultLoc = idTask.Result;                                      //URL wrapper to check recognition status

                    /*Continue to check task status until it is completed */
                    Task<IdentificationOperation> idOutcomeCheck;
                    Boolean done = false;
                    Status outcome;
                    do
                    {
                        await Task.Delay(apiDelayInterval);

                        idOutcomeCheck = idClient.CheckIdentificationStatusAsync(resultLoc);
                        await idOutcomeCheck;

                        outcome = idOutcomeCheck.Result.Status;


                        /*If recognition is complete or failed, stop checking for status*/
                        done = (outcome == Status.Succeeded || outcome == Status.Failed);
                    } while (!done);

                    User speaker = null;

                    /*Set user as unrecognizable if API request resonse indicates failure */
                    if (outcome == Status.Failed)
                    {
                        speaker = new User("Not recognized", "", -1);
                        Console.Error.WriteLine("Recognition operation failed for this phrase.");
                    }

                    else
                    {
                        Guid profileID = idOutcomeCheck.Result.ProcessingResult.IdentifiedProfileId;           //Get profile ID for this identification.

                        /*If the recognition request succeeded but no user could be recognized */
                        if (outcome == Status.Succeeded
                            && profileID.ToString() == "00000000-0000-0000-0000-000000000000")
                        {
                            speaker = new User("Not recognized", "", -1);
                        }

                        /*If task suceeded and the profile ID does match an ID in
                         * the set of known user profiles then set associated user */
                        else if (idOutcomeCheck.Result.Status == Status.Succeeded
                            && voiceprintDictionary.ContainsKey(profileID))
                        {
                            speaker = voiceprintDictionary[profileID].AssociatedUser;
                        }
                    }

                    curPhrase.Value.Speaker = speaker;                     //Set speaker property in TranscriptionOutput object based on result.

                }//End-foreach

            } catch (AggregateException ex)
            {
                Console.Error.WriteLine("Id failed: " + ex.Message);
            }


            recognitionComplete.SetResult(0);

        }
    }
}
