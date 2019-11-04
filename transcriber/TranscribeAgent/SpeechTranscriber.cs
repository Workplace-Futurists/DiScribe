using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public FileInfo MeetingMinutes { get; set; }

        public SpeechConfig Config { get; set; }


        /// <summary>
        /// The transcript contains speaker names,
        /// timestamps, and the contents of what each speaker said.
        ///
        /// <para> The transcription follows the the correct order, so that
        /// the beginning of the meeting is at the start of the file, and the last
        /// speech around the end of the meeting is at the end of the file.</para>
        /// </summary>
        /// <returns></returns>
        public async Task CreateTranscription()
        {
            var transcriptionFinished = new TaskCompletionSource<int>();

            SortedList<int, TranscriptionOutput> sharedList = new SortedList<int, TranscriptionOutput>();
            await MakeTranscriptionOutputs(sharedList);                  //Transcribe all segments of audio to get TranscriptionOutput SortedList

            /*Task failed if no TranscriptionOutput was added to sharedList*/
            if (sharedList.Count == 0)
                transcriptionFinished.SetException(new List<Exception> { new Exception("Transcription failed. Empty result.") });

            else
            {
                using (System.IO.StreamWriter file = 
                    new System.IO.StreamWriter(MeetingMinutes.FullName, true))
                {
                    /*Iterate over the list of TranscrtiptionOutputs in order and write output to MeetingMinutes file. 
                     * Order is by start offset. Uses format set by TranscriptionOutput.ToString()*/
                    foreach (var curNode in sharedList)
                    {
                        file.Write(curNode.Value.ToString());
                    }
                }

            }
            

        }


        /// <summary>
        /// Creates a set of TranscriptionOutput objects.
        /// </summary>
        /// <returns>FileInfo object for the transcription output text file.</returns>
        private async Task MakeTranscriptionOutputs(SortedList<int, TranscriptionOutput> transcriptionOutputs)
        {
            FileInfo outFile = new FileInfo(@"../../../transcript/minutes.txt");

            /*Do transcription concurrently for each AudioSegment. */
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(outFile.FullName, true))
            {
                foreach (var curElem in AudioSegments)
                {
                    RecognitionWithPullAudioStreamAsync(Config, curElem.Value, transcriptionOutputs);
                }
            }

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
                        // Subscribes to events.
                        recognizer.Recognizing += (s, e) =>
                        {
                            //
                        };
                        recognizer.Recognized += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                                result.Append($"RECOGNIZED: Text={e.Result.Text}");                   //Write transcription text to result
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
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

                       //Add the result to transcriptionOutputs wrapped in a TranscriptionOutput object.
                        transcriptionOutputs.Add(segment.Offset, new TranscriptionOutput(result.ToString(), segment.SpeakerInfo, segment.Offset));
                        Console.Write("Awaiting recognition stop");

                        // Stops recognition.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    }
                }
            }
    }
    
}
