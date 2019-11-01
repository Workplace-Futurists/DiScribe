using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;

namespace transcriber.TranscribeAgent
{
    /// <summary>
    /// Provides transcription of a set of AudioSegments to create a formatted text file.
    /// <para>See <see cref="TranscribeAgent.AudioSegment"></see> documentation for more info on AudioSegment.</para>
    /// <para>Uses the Microsoft Azure Cognitive Services Speech SDK to perform transcription of audio streams
    /// within each AudioSegment. </para>
    /// </summary>
    class Speechtranscriber
    {
        public SpeechTranscriber(SortedList<AudioSegment, AudioSegment> audioSegments, FileInfo outFile)
        {
            AudioSegments = audioSegments;
            MeetingMinutes = outFile;
        }

        /// <summary>
        /// SortedList where the the AudioSegments are sorted by their Offset property.
        /// This supports transcription in the correct order.
        /// </summary>
        public SortedList<AudioSegment, AudioSegment> AudioSegments { get; set; }

        public FileInfo MeetingMinutes { get; set; }


        /// <summary>
        /// Creates an audio transcript text file. The transcript contains speaker names,
        /// timestamps, and the contents of what each speaker said.
        ///
        /// <para> The transcription follows the the correct order, so that
        /// the beginning of the meeting is at the start of the file, and the last
        /// speech around the end of the meeting is at the end of the file.</para>
        /// </summary>
        /// <returns>FileInfo object for the transcription output text file.</returns>
        public async void CreateTranscription()
        {
            FileInfo outFile = new FileInfo(@"record\minutes.txt");

            //foreach (var segment in AudioSegments)

            RecognitionWithPullAudioStreamAsync(AudioSegments[AudioSegments.Keys[0]].AudioStream, outFile).Wait();

        }

        public static async Task RecognitionWithPullAudioStreamAsync(PullAudioInputStream theStream, FileInfo outFile)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("c9b69428770c48bc871e23ae97490a63", "centralus");

            var stopRecognition = new TaskCompletionSource<int>();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(outFile.FullName, true))
            {
                using (var audioInput = AudioConfig.FromStreamInput(theStream))
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
                                file.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                                file.WriteLine($"NOMATCH: Speech could not be recognized.");
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

                        Console.Write("Awaiting recogniotion stop");
                        // Stops recognition.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
