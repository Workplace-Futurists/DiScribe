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
        public SpeechTranscriber(TranscribeController controller)
        {
            Controller = controller;
            TranscriptionOutputs = new SortedList<long, TranscriptionOutput>();
        }

        private static TranscribeController Controller;

        /// <summary>
        /// Outputs created by transcription. Represents sentences of speech.
        /// Includes data for transcription text and speaker.
        /// </summary>
        public SortedList<long, TranscriptionOutput> TranscriptionOutputs { get; set; }

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
        public async Task DoTranscription()
        {
            /*Transcribe audio to create a set of TransciptionOutputs to represent sentences.
             * with the speakers identified
             */
            await getTranscriptionOutputs();

            /*Task failed if no TranscriptionOutput was added to sharedList*/
            if (TranscriptionOutputs.Count == 0)
            {
                throw new AggregateException(new List<Exception> { new Exception("Transcription failed. Empty result.") });
            }
        }

        /// <summary>
        /// Creates a set of TranscriptionOutput objects which contain transcribed sentences
        /// from MeetingAudio. Also performs speaker recognition using the audio
        /// within each TranscriptionOutput.
        /// </summary>
        /// <returns>Task for transcription flow</returns>
        private async Task getTranscriptionOutputs()
        {
            Console.WriteLine(">\tBegin Transcription...");
            /*Divide audio into sentences which are stored in transcriptOutputs as TranscriptionOutput objects */
            await RecognitionWithPullAudioStreamAsync();
        }

        private async Task RecognitionWithPullAudioStreamAsync()
        {
            var stopRecognition = new TaskCompletionSource<int>();
            var entireAudio = Controller.FileSplitter.GetEntireAudio();
            int errorCounter = 0;                                                //Number of failed recognitions to detect if recognizer gets stuck
            const int ERROR_MAX = 10;

            using (var audioInput = AudioConfig.FromStreamInput(entireAudio.AudioStream))
            {
                // Creates a speech recognizer using audio stream input.
                using (var speech_recogniser = new SpeechRecognizer(Program.SpeechConfig, audioInput))
                {
                    // Subscribes to events. Subscription is important, otherwise recognition events aren't handled.
                    speech_recogniser.Recognizing += (s, e) =>
                    {
                        //
                    };
                    speech_recogniser.Recognized += (s, e) =>
                    {
                        string transcribedText = "";

                        Boolean resultAvailable = false;
                        Boolean success = false;

                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            resultAvailable = true;
                            Console.WriteLine($"RECOGNIZED: Text = {e.Result.Text}\n");
                            transcribedText = e.Result.Text;                                      //Write transcription text to result
                            success = true;                                                       //Set flag to indicate that transcription succeeded.
                            errorCounter = 0;                                                     //Reset error counter
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            resultAvailable = true;
                            Console.WriteLine($">\tNOMATCH: Speech could not be recognized.");
                            transcribedText = $"NOMATCH: Speech could not be recognized.";        //Write fail message to result
                            errorCounter++;                                                       //Increment error counter

                            if (errorCounter > ERROR_MAX)                                         //If ERROR_MAX failures occur in a row, stop recognition
                            {
                                speech_recogniser.StopContinuousRecognitionAsync().ConfigureAwait(false);
                            }
                        }

                        if (resultAvailable)
                        {
                            /*Start and end offsets in milliseconds from 0, which is beginning of audio. Note
                             * conversion from ticks to milliseconds.*/
                            long startOffset = e.Result.OffsetInTicks / 10000L;
                            long endOffset = startOffset + (long)e.Result.Duration.TotalMilliseconds;

                            //CRITICAL section. Add the result to transcriptionOutputs wrapped in a TranscriptionOutput object.
                            lock (_lockObj)
                            {
                                /*Split the audio based on start and end offset of the identified phrase. Note access to shared stream. */
                                AudioSegment segment = Controller.FileSplitter.SplitAudio((ulong)startOffset, (ulong)endOffset);
                                TranscriptionOutputs.Add(startOffset, new TranscriptionOutput(transcribedText, success, segment));
                            }//END CRITICAL section.
                        }
                    };

                    speech_recogniser.Canceled += (s, e) =>
                    {
                        Console.WriteLine($">\tCANCELED: Reason = {e.Reason}");
                        Console.WriteLine(">\tTranscription Complete.");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($">\tCANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($">\tCANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($">\tCANCELED: Make Sure to Update Subscription Info");
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    speech_recogniser.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine(">\tSession Started.");
                    };

                    speech_recogniser.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine(">\tSession Stopped.");
                        stopRecognition.TrySetResult(0);
                    };

                    Console.WriteLine(">\tPerforming Initial Transcription Process...");
                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await speech_recogniser.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    Console.WriteLine(">\tTranscription Process About to Stop...");

                    // Stops recognition.
                    await speech_recogniser.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
