using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using transcriber.TranscribeAgent;

namespace transcriber.TranscribeAgent
{
    /// <summary>
    /// Provides transcription of a set of AudioSegments to create a formatted text file.
    /// <para>See <see cref="TranscribeAgent.AudioSegment"></see> documentation for more info on AudioSegment.</para>
    /// <para>Uses the Microsoft Azure Cognitive Services Speech SDK to perform transcription of audio streams
    /// within each AudioSegment. </para>
    /// </summary>
    class SpeechTranscriber
    {
        public SpeechTranscriber(SpeechConfig config, SortedList<AudioSegment, AudioSegment> audioSegments, FileInfo outFile)
        {
            AudioSegments = audioSegments;
            MeetingMinutes = outFile;
            Config = config;
        }

        /// <summary>
        /// SortedList where the the AudioSegments are sorted by their Offset property.
        /// This supports transcription in the correct order.
        /// </summary>
        public SortedList<AudioSegment, AudioSegment> AudioSegments { get; set; }

        /// <summary>
        /// The meeting minutes text output file.
        /// </summary>
        public FileInfo MeetingMinutes { get; set; }

        /// <summary>
        /// Configuration for the Azure Cognitive Speech Services resource.
        /// </summary>
        public SpeechConfig Config { get; set; }

        public List<Tuple<double, double>> valid_user_offsetList { get; set; }
        public List<Tuple<double, double>> unrecognized_offsetList { get; set; }

        private double curOffset = 0;

        
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
             SortedList<int, TranscriptionOutput> sharedList = new SortedList<int, TranscriptionOutput>();
            await MakeTranscriptionOutputs(sharedList);                  //Transcribe all segments of audio to get TranscriptionOutput SortedList in parallel

            /*Task failed if no TranscriptionOutput was added to sharedList*/
            if (sharedList.Count == 0)
            {
                throw new AggregateException(new List<Exception> { new Exception("Transcription failed. Empty result.") });
            }

            else
            {
                StringBuilder output = new StringBuilder();
                
                /*Iterate over the list of TranscrtiptionOutputs in order and add them to
                 * output that will be written to file.
                 * Order is by start offset. 
                 * Uses format set by TranscriptionOutput.ToString(). Also does text wrapping
                 * if width goes over limit of chars per line.
                 */
                foreach (var curNode in sharedList)
                {
                    string curSegmentText = curNode.Value.ToString();
                    if (curSegmentText.Length > lineLength)
                    {
                        curSegmentText = WrapText(curSegmentText, lineLength);
                    }

                    output.AppendLine( curSegmentText + "\n");
                    
                }

                /*Overwrite any existing MeetingMinutes file with the same name.*/
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(MeetingMinutes.FullName, false))
                {
                    file.Write(output.ToString());
                }
                                
            }

        }


        /// <summary>
        /// Creates a set of TranscriptionOutput objects.
        /// </summary>
        /// <returns>FileInfo object for the transcription output text file.</returns>
        private async Task MakeTranscriptionOutputs(SortedList<int, TranscriptionOutput> transcriptionOutputs)
        {
            List<Task> taskList = new List<Task>();
            
            /*Do transcription concurrently for each AudioSegment. */
            foreach (var curElem in AudioSegments)
            {
                Task curTask = RecognitionWithPullAudioStreamAsync(Config, curElem.Value, transcriptionOutputs);
                taskList.Add(curTask);
            }

            await Task.WhenAll(taskList.ToArray());       //Asynchronously wait for all AudioSegments to be transcribed
        }



        private static async Task RecognitionWithPullAudioStreamAsync(SpeechConfig config, AudioSegment segment, 
            SortedList<int, TranscriptionOutput> transcriptionOutputs)
        {
            StringBuilder result = new StringBuilder();
            var stopRecognition = new TaskCompletionSource<int>();

           using (var audioInput = AudioConfig.FromStreamInput(segment.AudioStream))
           {
                    // Creates a speech recognizer using audio stream input.
                    using (var recognizer = new SpeechRecognizer(config, audioInput))
                    {
                        // Subscribes to events. Subscription is important, otherwise recognition events aren't handled.
                        recognizer.Recognizing += (s, e) =>
                        {
                            //
                        };
                        recognizer.Recognized += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                //string curID = recognizer.EndpointId;
                                //Console.WriteLine($"AT Audio Timestamp: Text={e.Result.GetProperty(PropertyId.ConversationTranscribingService_DataBufferTimeStamp)}");
                                double sentDuration = e.Result.Duration.TotalMilliseconds;
                                double startOffset = curOffset+1;
                                double endOffset = curOffset + sentDuration;
                                Tuple<double, double> offsetTuple = new Tuple<double, double>(startOffset, endOffset);
                                valid_user_offsetList.Add(offsetTuple);

                                Console.WriteLine($"Sentence Duration: {sentDuration.ToString()} MilliSeconds.");
                                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                                result.Append(e.Result.Text);                                         //Write transcription text to result
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                double sentDuration = e.Result.Duration.TotalMilliseconds;
                                double startOffset = curOffset + 1;
                                double endOffset = curOffset + sentDuration;
                                Tuple<double, double> offsetTuple = new Tuple<double, double>(startOffset, endOffset);
                                unrecognized_offsetList.Add(offsetTuple);

                                Console.WriteLine($"Unmatched Portion Duration: {sentDuration.ToString()} MilliSeconds.");
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                                result.Append($"NOMATCH: Speech could not be recognized.");           //Write fail message to result
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

                        //CRITICAL section. Add the result to (shared) transcriptionOutputs wrapped in a TranscriptionOutput object.
                        lock (_lockObj)
                        {
                            transcriptionOutputs.Add(segment.Offset, new TranscriptionOutput(result.ToString(), segment.SpeakerInfo, segment.Offset));
                        }//END CRITICAL section.

                        Console.Write("Awaiting recognition stop");

                        // Stops recognition.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    }
                }
            }


        private static string WrapText(string text, int lineLength)
        {
            StringBuilder outcome = new StringBuilder();
            
            for (int i = lineLength; i < text.Length; i += lineLength)
            {
                int actualLength = lineLength;
                Boolean foundSpace = false;

                while (i < text.Length && !foundSpace)
                {
                    if (text[i] == ' ')               //Find a space and split there
                    {
                        foundSpace = true;
                        outcome.AppendLine(text.Substring(i - actualLength, actualLength));
                    }
                    actualLength++;
                    i++;
                }
              
            }

            return outcome.ToString();
            
        }


    }

    
}
