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
            FileInfo meetingRecording, List<Voiceprint> voiceprints, FileInfo meetingMinutes)
        {
            MeetingRecording = meetingRecording;
            MeetingMinutes = meetingMinutes;

            Transcriber = new SpeechTranscriber(speechConfig, meetingRecording, meetingMinutes);
            Recognizer = new Recognizer(meetingRecording, speakerIDKey, voiceprints);
        }

        /// <summary>
        /// File details for audio file containing meeting recording.
        /// </summary>
        public FileInfo MeetingRecording { get; set; }

        public SpeechTranscriber Transcriber { get; private set; }

        public Recognizer Recognizer { get; private set; }

        /// <summary>
        /// File details for text output file of meeting minutes.
        /// </summary>
        public FileInfo MeetingMinutes { get; private set; }


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

                /*Write transcription to text file */
                WriteTranscriptionFile();
            }
            catch (Exception transcribeEx)
            {
                Console.Error.Write("Mission failed. No transcription could be created from audio segments. " + transcribeEx.Message);
                return false;
            }

            return true;
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
                new System.IO.StreamWriter(MeetingMinutes.FullName, false))
            {
                file.Write(output.ToString());
            }
        }

    }
}
