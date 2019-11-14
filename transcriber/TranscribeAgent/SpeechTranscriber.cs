using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
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
        public SpeechTranscriber(SpeechConfig config, FileInfo audioFile, FileInfo outFile, List<Voiceprint> voiceprints)
        {
            FileSplitter = new AudioFileSplitter(audioFile);
            MeetingMinutes = outFile;
            Config = config;
            Voiceprints = voiceprints;
            TranscriptionOutputs = new SortedList<long, TranscriptionOutput>();
        }



        public AudioFileSplitter FileSplitter { get; private set; }

        public List<Voiceprint> Voiceprints { get; set; }

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
           /*Divide audio into sentences which are stored in transcriptOutputs as TranscriptionOutput objects */
            await RecognitionWithPullAudioStreamAsync();

            /*Do speaker recognition concurrently for each TranscriptionOutput. */
            await DoSpeakerRecognition();

            //await Task.WhenAll(taskList.ToArray());       //Asynchronously wait for all speakers to be recognized
        }



        private async Task RecognitionWithPullAudioStreamAsync()
        {
           var stopRecognition = new TaskCompletionSource<int>();
           var entireAudio = FileSplitter.GetEntireAudio();

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
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            resultAvailable = true;
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            transcribedText = $"NOMATCH: Speech could not be recognized.";        //Write fail message to result
                        }

                        if (resultAvailable)
                        {
                            /*Start and end offsets in milliseconds from 0, which is beginning of audio. Note
                             * conversion from ticks to milliseconds.*/
                            long startOffset = e.Result.OffsetInTicks/10000L;          
                            long endOffset = startOffset + (long)e.Result.Duration.TotalMilliseconds;

                            /*Split the audio based on start and end offset of the identified phrase */
                            AudioSegment segment = FileSplitter.SplitAudio((ulong)startOffset, (ulong)endOffset);

                            //CRITICAL section. Add the result to transcriptionOutputs wrapped in a TranscriptionOutput object.
                            lock (_lockObj)
                            {
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

                    Console.Write("Awaiting recognition completeion");
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
        /// </summary>
        /// <param name="transcription"></param>
        private Task DoSpeakerRecognition()
        {
            var recognitionComplete = new TaskCompletionSource<int>();
            foreach (var curSentence in TranscriptionOutputs)
            {
                curSentence.Value.Speaker = RecognizeUser(curSentence.Value.Segment);
            }

            recognitionComplete.SetResult(0);

            return recognitionComplete.Task;
            
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

        

        private User RecognizeUser(AudioSegment segment)
        {
            return new User("USER_" + new System.Random().Next(), "TEST@EXAMPLE.COM", new System.Guid());
        }







    }

    
}
