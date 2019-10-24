using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;

namespace FuturistTranscriber.TranscribeAgent
{
    class Program
    {


        public static async Task RecognitionWithPullAudioStreamAsync(PullAudioInputStream theStream)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("c9b69428770c48bc871e23ae97490a63", "centralus");

            var stopRecognition = new TaskCompletionSource<int>();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"record\minutes.txt", true))
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


        

        
        
        static void Main(string[] args)
        {
            Console.WriteLine("Creating transcript...");

            //string path = @"..\..\..\record\test_meeting_02.wav";
            string path = @"record\test_meeting_02.wav";
            FileInfo test = new FileInfo(path);
            var x = new AudioFileSplitter(null, test);

            var list = x.SplitAudio();                                   //Split audio into segments (only 1 in this case).

            var segment = list[list.Keys[0]];                            //Get the 1 segment.
           
            RecognitionWithPullAudioStreamAsync(segment.AudioStream).Wait();
            
            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }
        
    }
}
