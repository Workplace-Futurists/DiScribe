using DiScribe.DatabaseManager.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CognitiveServices.Speech;
using DiScribe.Transcriber.Audio;
using DiScribe.DatabaseManager;

namespace DiScribe.Transcriber
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
        public TranscribeController(FileInfo meetingRecording, List<User> voiceprints)
        {
            SpeechConfig = SpeechConfig.FromSubscription("1558a08d9f6246ffaa1b31def4c2d85f", "centralus");
            SpeakerIDSubKey = "7fb70665af5b4770a94bb097e15b8ae0";

            Voiceprints = voiceprints;
            FileSplitter = new AudioFileSplitter(meetingRecording);
            Transcriber = new SpeechTranscriber(this);
            Recognizer = new Recognizer(this);

            Console.WriteLine(">\tTranscription Controller initialized \n\t" +
                "on Audio Recording [" + meetingRecording.FullName + "].");
        }

        public List<User> Voiceprints { get; set; }

        internal AudioFileSplitter FileSplitter { get; set; }

        SpeechTranscriber Transcriber { get; set; }

        Recognizer Recognizer { get; set; }

        public SpeechConfig SpeechConfig { get; private set; }

        public string SpeakerIDSubKey { get; private set; }

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
            }
            catch (Exception transcribeEx)
            {
                Console.Error.Write(">\tTranscription Failed: " + transcribeEx.Message);
                return false;
            }


            /*Do speaker recognition concurrently for each TranscriptionOutput. */
            Recognizer.DoSpeakerRecognition(Transcriber.TranscriptionOutputs).Wait();

            Console.WriteLine(">\tTranscription && Recognition Complete");
            return true;
        }

        public FileInfo WriteTranscriptionFile(string rid = "", int lineLength = 120)
        {
            FileInfo meetingMinutes;

            // Changes location of stored meeting minutes in release mode
            #if (DEBUG)
                meetingMinutes = new FileInfo(@"../../../../Transcripts/minutes.txt");
            #else
                meetingMinutes = new FileInfo(@"Transcripts/minutes.txt");
            #endif

            FileInfo transcript;
            if (rid.Equals(""))
                transcript = meetingMinutes;
            else
                transcript = new FileInfo(meetingMinutes.
                    FullName.Replace("minutes.txt", "minutes_" + rid + ".txt"));

            Console.WriteLine(">\tBegin Writing Transcription " +
                "& Speaker Recognition Result into File \n\t[" + transcript.Name + "]");
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
                if (!transcript.Exists)
                {
                    Console.WriteLine(">\tFile [" + transcript.Name + "] Does Not Exist\n\t " +
                        "Creating the File Under the Directory \n\t [" + transcript.DirectoryName + "]");
                    transcript.Directory.Create();
                    transcript.Create().Close();
                }
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(transcript.FullName, false))
                {
                    file.Write(output.ToString());
                }
                Console.WriteLine(">\tTranscript Successfully Written.");
                return transcript;
            }
            catch (Exception WriteException)
            {
                Console.Error.WriteLine("Error occurred during Write: " + WriteException.Message);
                return null;
            }
        }
    }
}
