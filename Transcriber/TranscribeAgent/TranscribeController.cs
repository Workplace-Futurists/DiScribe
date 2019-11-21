using SpeakerRegistration.Data;
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
        public TranscribeController(FileInfo meetingRecording, List<User> voiceprints, SpeechConfig speechConfig, string speakerIDSubKey)
        {
            Voiceprints = voiceprints;
            FileSplitter = new AudioFileSplitter(meetingRecording);
            Transcriber = new SpeechTranscriber(this);
            Recognizer = new Recognizer(this);
            SpeechConfig = speechConfig;
            SpeakerIDSubKey = speakerIDSubKey;

            Console.WriteLine(">\tTranscription Controller initialized " +
                "on Audio Recording [" + meetingRecording.FullName + "].");
        }

        public List<User> Voiceprints { get; set; }

        public AudioFileSplitter FileSplitter { get; private set; }

        public SpeechTranscriber Transcriber { get; private set; }

        public Recognizer Recognizer { get; private set; }

        public SpeechConfig SpeechConfig { get; private set; }

        public String SpeakerIDSubKey { get; private set; }

        /// <summary>
        /// Uses Voiceprints to perform speaker recognition while transcribing the audio file MeetingRecording.
        /// Creates a formatted text output file holding the transcription.
        /// </summary>
        /// <returns>A FileInfo instance holding information about the transcript file that was created.</returns>
        public Boolean Perform()
        {
            try
            {
                //Wait synchronously for transcript to be finished and written to minutes file.
                Transcriber.DoTranscription().Wait();

                /*Do speaker recognition concurrently for each TranscriptionOutput. */
                Recognizer.DoSpeakerRecognition(Transcriber.TranscriptionOutputs).Wait();
                Console.WriteLine(">\tTranscription && Recognition = Success");
            }
            catch (Exception transcribeEx)
            {
                Console.Error.Write(">\tTranscription Failed: " + transcribeEx.Message);
                return false;
            }
            return true;
        }

        public void WriteTranscriptionFile(FileInfo meetingMinutes, int lineLength = 120)
        {
            Console.WriteLine(">\tBegin Writing Transcription " +
                "& Speaker Recognition Result into File [" + meetingMinutes.FullName + "]...");
            StringBuilder output = new StringBuilder();

            try
            {
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

                /* Overwrite any existing MeetingMinutes file with the same name,
                 * else create file. Output results to text file.
                 */
                if (!meetingMinutes.Exists)
                {
                    Console.WriteLine(">\tFile [" + meetingMinutes.Name + "] Does Not Exist, " +
                        "Creating the File Under the Directory: " + meetingMinutes.DirectoryName);
                    meetingMinutes.Create().Close();
                }
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(meetingMinutes.FullName, false))
                {
                    file.Write(output.ToString());
                }
                Console.WriteLine(">\tTranscript Successfully Written.");
            }
            catch (Exception WriteException)
            {
                Console.Error.WriteLine("Error occurred during Write: " + WriteException.Message);
            }
        }
    }
}
