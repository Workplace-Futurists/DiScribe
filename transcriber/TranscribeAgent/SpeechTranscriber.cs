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
using NAudio.Wave;
using transcriber.Data;
using transcriber.TranscribeAgent;

namespace transcriber.TranscribeAgent
{
    /// <summary>
    /// Provides transcription of a set of an AudioSegment representing meeting audio to create a formatted text file
    /// of meeting minutes.
    /// Also supports speaker recognition to output names of meeting participants in meeting minutes.
    /// <para>See <see cref="TranscribeAgent.AudioSegment"></see> documentation for more info on AudioSegment.</para>
    /// <para>Uses the Microsoft Azure Cognitive Services Speech SDK to perform transcription of audio streams
    /// within each AudioSegment. </para>
    /// </summary>
    public class SpeechTranscriber
    {
        public SpeechTranscriber(SpeechConfig config, string speakerSubKey, FileInfo audioFile, FileInfo outFile, List<Voiceprint> voiceprints)
        {
            FileSplitter = new AudioFileSplitter(audioFile);
            SpeakerSubKey = speakerSubKey;
            MeetingMinutes = outFile;
            Config = config;
            Voiceprints = voiceprints;
            TranscriptionOutputs = new SortedList<long, TranscriptionOutput>();
        }


        /// <summary>
        /// FileSplitter to allow access to specific segments of audio.
        /// </summary>
        public AudioFileSplitter FileSplitter { get; private set; }

        /// <summary>
        /// Voiceprints for users in this transcription
        /// </summary>
        public List<Voiceprint> Voiceprints { get; set; }

        /// <summary>
        /// Subscription Key for the speaker recogniton API
        /// </summary>
        public String SpeakerSubKey { get; set; }

        /// <summary>
        /// Outputs created by transcription. Represents sentences of speech.
        /// Includes data for transcription text and speaker.
        /// </summary>
        SortedList<long, TranscriptionOutput> TranscriptionOutputs { get; set; }

        /// <summary>
        /// The meeting minutes text output file.
        /// </summary>
        public FileInfo MeetingMinutes { get; set; }

        /// <summary>
        /// Configuration for the Azure Cognitive Speech Services resource.
        /// </summary>
        public SpeechConfig Config { get; set; }

        /// <summary>
        /// Lock object for synchronized access to transcription output collection.
        /// </summary>
        private static readonly object _lockObj = new object();


        /// <summary>
        /// The transcript contains speaker names,
        /// timestamps, and the contents of what each speaker said.
        ///
        /// <para> The transcription follows the the correct order, so that
        /// the beginning of the meeting is at the start of the file, and the last
        /// speech around the end of the meeting is at the end of the file.</para>
        /// </summary>
        /// <returns></returns>
        public async Task CreateTranscription(int lineLength = 120)
        {
            /*Transcribe audio to create a set of TransciptionOutputs to represent sentences. 
             * with the speakers identified
             */
            await MakeTranscriptionOutputs();       

            /*Task failed if no TranscriptionOutput was added to sharedList*/
            if (TranscriptionOutputs.Count == 0)
            {
                throw new AggregateException(new List<Exception> { new Exception("Transcription failed. Empty result.") });
            }

            Console.Write("Writing transcription to file...");
            /*Write transcription to text file */
            WriteTranscriptionFile(lineLength);
        
        }


        /// <summary>
        /// Creates a set of TranscriptionOutput objects which contain transcribed sentences
        /// from MeetingAudio. Also performs speaker recognition using the audio
        /// within each TranscriptionOutput.
        /// </summary>
        /// <returns>Task for transcription flow</returns>
        private async Task MakeTranscriptionOutputs()
        {
            Console.WriteLine("Transcribing audio...");
           /*Divide audio into sentences which are stored in transcriptOutputs as TranscriptionOutput objects */
            await RecognitionWithPullAudioStreamAsync();

            Console.Write("Performing speaker recognition...");
            /*Do speaker recognition concurrently for each TranscriptionOutput. */
            await DoSpeakerRecognition();

        }



        private async Task RecognitionWithPullAudioStreamAsync()
        {
            var stopRecognition = new TaskCompletionSource<int>();
            var entireAudio = FileSplitter.GetEntireAudio();
            int errorCounter = 0;                                                //Number of failed recognitions to detect if recognizer gets stuck
            const int ERROR_MAX = 10;

            using (var audioInput = AudioConfig.FromStreamInput(entireAudio.AudioStream))
            {
                // Creates a speech recognizer using audio stream input.
                using (var recognizer = new SpeechRecognizer(Config, audioInput))
                {
                    // Subscribes to events. Subscription is important, otherwise recognition events aren't handled.
                    recognizer.Recognizing += (s, e) =>
                    {
                            //
                    };
                    recognizer.Recognized += (s, e) =>
                    {
                        string transcribedText = "";

                        Boolean resultAvailable = false;
                        Boolean success = false;
                        

                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            resultAvailable = true;
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                            transcribedText = e.Result.Text;                                      //Write transcription text to result
                            success = true;                                                       //Set flag to indicate that transcription succeeded.
                            errorCounter = 0;                                                     //Reset error counter
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            resultAvailable = true;
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            transcribedText = $"NOMATCH: Speech could not be recognized.";        //Write fail message to result
                            errorCounter++;                                                       //Increment error counter

                            if (errorCounter > ERROR_MAX)                                         //If ERROR_MAX failures occur in a row, stop recognition
                            {
                                recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                            }
                        }

                        
                        if (resultAvailable)
                        {
                            /*Start and end offsets in milliseconds from 0, which is beginning of audio. Note
                             * conversion from ticks to milliseconds.*/
                            long startOffset = e.Result.OffsetInTicks/10000L;          
                            long endOffset = startOffset + (long)e.Result.Duration.TotalMilliseconds;

                            
                            //CRITICAL section. Add the result to transcriptionOutputs wrapped in a TranscriptionOutput object.
                            lock (_lockObj)
                            {
                                /*Split the audio based on start and end offset of the identified phrase. Note access to shared stream. */
                                AudioSegment segment = FileSplitter.SplitAudio((ulong)startOffset, (ulong)endOffset);
                                TranscriptionOutputs.Add(startOffset, new TranscriptionOutput(transcribedText, success, segment));
                            }//END CRITICAL section.
                        }
                   };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\nSession stopped event."); 
                        Console.WriteLine("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    Console.Write("Awaiting recognition completion");
                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    Console.Write("Awaiting recognition stop");

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
        }



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
        private async Task DoSpeakerRecognition(int apiDelayInterval = 3000)
        {
            var recognitionComplete = new TaskCompletionSource<int>();

            /*Create REST client for enrolling users */
            SpeakerIdentificationServiceClient idClient = new SpeakerIdentificationServiceClient(SpeakerSubKey);

            /*Dictionary for efficient voiceprint lookup by enrollment GUID*/
            Dictionary<Guid, Voiceprint> voiceprintDictionary = new Dictionary<Guid, Voiceprint>();
            Guid[] userIDs = new Guid[Voiceprints.Count];

            /*Add all voiceprints to the dictionary*/
            foreach (var voiceprint in Voiceprints)
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



        private void WriteTranscriptionFile(int lineLength = 120)
        {
            StringBuilder output = new StringBuilder();

            /*Iterate over the list of TranscrtiptionOutputs in order and add them to
             * output that will be written to file.
             * Order is by start offset. 
             * Uses format set by TranscriptionOutput.ToString(). Also does text wrapping
             * if width goes over limit of chars per line.
             */
            foreach (var curNode in TranscriptionOutputs)
            {
                string curSegmentText = curNode.Value.ToString();
                if (curSegmentText.Length > lineLength)
                {
                    curSegmentText = WrapText(curSegmentText, lineLength);
                }

                output.AppendLine(curSegmentText + "\n");
            }

            /*Overwrite any existing MeetingMinutes file with the same name,
             * else create file. Output results to text file.*/
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(MeetingMinutes.FullName, false))
            {
                file.Write(output.ToString());
            }
        }




        private static string WrapText(string text, int lineLength)
        {
            if (text.Length < lineLength)
                return text;

            StringBuilder outcome = new StringBuilder();

            int i;
            int startingIndex = 0;
            int lastSplitEnd = 0;
            Boolean savedLastLine = false;


            for (i = lineLength; i < text.Length; i += lineLength)
            {
                Boolean foundSpace = false;
                startingIndex = i;                          //Starting index for this line.

                while (i < text.Length && !foundSpace)
                {
                    if (text[i] == ' ')                        //Find a space and split there
                    {
                        foundSpace = true;
                        outcome.AppendLine(text.Substring(startingIndex-lineLength, lineLength + (i-startingIndex)));
                    }

                    i++;
                }

                /*If we never found a space before reaching end of text, append line anyway,
                 * otherwise it will be lost.*/
                if (!foundSpace)
                {
                    outcome.AppendLine(text.Substring(startingIndex, text.Length - startingIndex));
                    savedLastLine = true;
                }

                lastSplitEnd = i;
            }

            /*Ensure that remaining characters are also appended */
            if (!savedLastLine && text.Length % lineLength != 0)
            {
                outcome.AppendLine(text.Substring(lastSplitEnd, text.Length - lastSplitEnd));
            }

            return outcome.ToString();
       }

        

              

    }

    
}
