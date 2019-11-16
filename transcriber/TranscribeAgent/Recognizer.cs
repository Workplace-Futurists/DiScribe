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
using transcriber.TranscribeAgent;


namespace transcriber.TranscribeAgent
{
    public class Recognizer
    {
        public Recognizer(TranscribeController controller)
        {
            Controller = controller;
        }

        private static TranscribeController Controller;

        /// <summary>
        /// Performs speaker recognition on TranscriberOutputs to set
        /// the Speaker property.
        /// set set their User property representing the speaker.
        /// </summary>
        /// <param name="transcription"></param>
        public async Task DoSpeakerRecognition(SortedList<long, TranscriptionOutput> TranscriptionOutputs)
        {
            var recognitionComplete = new TaskCompletionSource<int>();

            /*Create REST client for enrolling users */
            SpeakerIdentificationServiceClient idClient = new SpeakerIdentificationServiceClient(Controller.SpeakerIDKey);

            /*Dictionary for efficient voiceprint lookup */
            Dictionary<Guid, Voiceprint> voiceprintDictionary = new Dictionary<Guid, Voiceprint>();
            Guid[] userIDs = new Guid[Controller.Voiceprints.Count];

            /*Add all voiceprints to the dictionary*/
            foreach (var voiceprint in Controller.Voiceprints)
            {
                voiceprintDictionary.Add(voiceprint.UserGUID, voiceprint);
            }

            voiceprintDictionary.Keys.CopyTo(userIDs, 0);


            /*Iterate over each phrase and attempt to identify the user.
             Passes the audio data as a stream and the user GUID associated with the
             Azure SpeakerRecogniztion API profile to the API via the IdentifyAsync() method.*/
            try
            {
                foreach (var curPhrase in TranscriptionOutputs)
                {
                    MemoryStream audioStream = Controller.FileSplitter.SplitAudioGetMemStream((ulong)curPhrase.Value.StartOffset, (ulong)curPhrase.Value.EndOffset);
                    Task<OperationLocation> idTask = idClient.IdentifyAsync(audioStream, userIDs, true);

                    await idTask;

                    var resultLoc = idTask.Result;

                    /*Continue to check task status until it is completed */
                    Task<IdentificationOperation> idOutcomeCheck;
                    Boolean done = false;
                    do
                    {
                        idOutcomeCheck = idClient.CheckIdentificationStatusAsync(resultLoc);

                        await idOutcomeCheck;

                        done = (idOutcomeCheck.Result.Status == Status.Succeeded || idOutcomeCheck.Result.Status == Status.Failed);
                    } while (!done);

                    Guid profileID = idOutcomeCheck.Result.ProcessingResult.IdentifiedProfileId;           //Get profile ID for this identification.


                    /*No user could be recognized */
                    if (idOutcomeCheck.Result.Status == Status.Succeeded
                        && profileID.ToString() == "00000000-0000-0000-0000-000000000000")
                    {
                        curPhrase.Value.Speaker = new User("Not recognized", "", -1);
                    }

                    else if (idOutcomeCheck.Result.Status == Status.Succeeded)
                    {
                        curPhrase.Value.Speaker = voiceprintDictionary[profileID].AssociatedUser;
                    }

                    else
                    {
                        Console.Error.WriteLine("Recognition operation failed");
                    }
                }
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine("Id failed: " + ex.Message);
            }
            recognitionComplete.SetResult(0);
        }
    }
}
