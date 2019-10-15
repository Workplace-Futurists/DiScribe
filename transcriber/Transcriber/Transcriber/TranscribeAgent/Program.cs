using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;

namespace FuturistTranscriber.TranscribeAgent
{
    class Program
    {

        // Continuous intent recognition using file input.
        public static async Task ContinuousRecognitionWithFileAsync()
        {
            // <intentContinuousRecognitionWithFile>
            // Creates an instance of a speech config with specified subscription key
            // and service region. Note that in contrast to other services supported by
            // the Cognitive Services Speech SDK, the Language Understanding service
            // requires a specific subscription key from https://www.luis.ai/.
            // The Language Understanding service calls the required key 'endpoint key'.
            // Once you've obtained it, replace with below with your own Language Understanding subscription key
            // and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("c9b69428770c48bc871e23ae97490a63", "centralus");

            // Creates an intent recognizer using file as audio input.
            // Replace with your own audio file name.
            using (var audioInput = AudioConfig.FromWavFileInput("whatstheweatherlike.wav"))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    // The TaskCompletionSource to stop recognition.
                    var stopRecognition = new TaskCompletionSource<int>();

                    // Creates a Language Understanding model using the app id, and adds specific intents from your model
                    //var model = LanguageUnderstandingModel.FromAppId("4a6f4d1b-9d29-4c66-b9d3-ef047f16f3f9");
                    //recognizer.AddIntent(model, "YourLanguageUnderstandingIntentName1", "id1");
                    //recognizer.AddIntent(model, "YourLanguageUnderstandingIntentName2", "id2");
                    //recognizer.AddIntent(model, "YourLanguageUnderstandingIntentName3", "any-IntentId-here");
                    string text = "";
                    // Subscribes to events.
                    recognizer.Recognizing += (s, e) => {
                        //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                        text = e.Result.Text;
                    };

                    /*recognizer.Recognized += (s, e) => {
                        if (e.Result.Reason == ResultReason.RecognizedIntent)
                        {
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                            Console.WriteLine($"    Intent Id: {e.Result.IntentId}.");
                            Console.WriteLine($"    Language Understanding JSON: {e.Result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult)}.");
                        }
                        else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                            Console.WriteLine($"    Intent not recognized.");
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };*/

                    recognizer.Canceled += (s, e) => {
                        //Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) => {
                        Console.WriteLine("Audio reaing started......");
                    };

                    recognizer.SessionStopped += (s, e) => {
                        Console.WriteLine("Audio reading completed......");
                        Console.WriteLine("The following text was recognized: "+ text);
                        stopRecognition.TrySetResult(0);
                    };


                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    //Console.WriteLine("Transcribing started...");
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(true);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
            // </intentContinuousRecognitionWithFile>
        }

        public static async Task RecognizeSpeechAsync()
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("c9b69428770c48bc871e23ae97490a63", "centralus");

            // Creates a speech recognizer.
            using (var recognizer = new SpeechRecognizer(config))
            {
                Console.WriteLine("Say something...");

                // Starts speech recognition, and returns after a single utterance is recognized. The end of a
                // single utterance is determined by listening for silence at the end or until a maximum of 15
                // seconds of audio is processed.  The task returns the recognition text as result. 
                // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
                // shot recognition like command or query. 
                // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
                var result = await recognizer.RecognizeOnceAsync();

                // Checks result.
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"We recognized: {result.Text}");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Creating transcript...");

            ContinuousRecognitionWithFileAsync().Wait();

            //RecognizeSpeechAsync().Wait();

            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }
    }
}
