using transcriber.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CognitiveServices.Speech;

namespace transcriber.TranscribeAgent
{
    public class TranscribeController
    {
        /// <summary>
        /// Presents an interface to the speaker-recognition based transcription functionality.
        /// Allows the use of a set of Voiceprints to perform transcription of a meeting audio file. 
        /// Also supports emailing of a transcription file.
        /// </summary>
        /// <param name="meetingRecording"></param>
        /// <param name="voiceprints"></param>
        public TranscribeController(SpeechConfig speechConfig, string speakerIDKey,
            FileInfo meetingRecording, List<Voiceprint> voiceprints)
        {
            SpeechConfig = speechConfig;
            SpeakerIDKey = speakerIDKey;
            MeetingRecording = meetingRecording;
            Voiceprints = voiceprints;

            FileSplitter = new AudioFileSplitter(meetingRecording);

            Transcriber = new SpeechTranscriber(this);
            Recognizer = new Recognizer(this);
        }

        public SpeechConfig SpeechConfig { get; set; }

        /// <summary>
        /// File details for audio file containing meeting recording.
        /// </summary>
        public FileInfo MeetingRecording { get; set; }

        public string SpeakerIDKey { get; set; }

        public List<Voiceprint> Voiceprints { get; set; }

        public AudioFileSplitter FileSplitter { get; private set; }

        /// <summary>
        /// Configuration for the Azure Cognitive Speech Services resource.
        /// </summary>
        public SpeechConfig Config { get; set; }

        public SpeechTranscriber Transcriber { get; private set; }

        public Recognizer Recognizer { get; private set; }

        /// <summary>
        /// Uses Voiceprints to perform speaker recognition while transcribing the audio file MeetingRecording.
        /// Creates a formatted text output file holding the transcription.
        /// </summary>
        /// <returns>A FileInfo instance holding information about the transcript file that was created.</returns>
        public Boolean DoTranscription()
        {
            try
            {
                //Wait synchronously for transcript to be finished and written to minutes file.
                Transcriber.CreateTranscription().Wait();
                Recognizer.DoSpeakerRecognition(Transcriber.TranscriptionOutputs).Wait();
            }
            catch (Exception transcribeEx)
            {
                Console.Error.Write("Mission failed. No transcription could be created from audio segments. " + transcribeEx.Message);
                return false;
            }

            return true;
        }

        public bool WriteTranscriptionFile(FileInfo meetingMinutes, int lineLength = 120)
        {
            StringBuilder output = new StringBuilder();

            /*Iterate over the list of TranscrtiptionOutputs in order and add them to
             * output that will be written to file.
             * Order is by start offset. 
             * Uses format set by TranscriptionOutput.ToString(). Also does text wrapping
             * if width goes over limit of chars per line.
             */
            try
            {
                foreach (var curNode in Transcriber.TranscriptionOutputs)
                {
                    string curSegmentText = curNode.Value.ToString();
                    if (curSegmentText.Length > lineLength)
                    {
                        curSegmentText = Helper.WrapText(curSegmentText, lineLength);
                    }

                    output.AppendLine(curSegmentText + "\n");
                }

                /*Overwrite any existing MeetingMinutes file with the same name,
                 * else create file. Output results to text file.*/
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(meetingMinutes.FullName, false))
                {
                    file.Write(output.ToString());
                }
            }
            catch (Exception WriteException)
            {
                Console.Error.Write("Error occurred during Write" + WriteException.Message);
                return false;
            }
            return true;
        }

    }
}
