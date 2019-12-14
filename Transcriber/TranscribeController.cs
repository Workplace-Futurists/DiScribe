using DiScribe.DatabaseManager.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CognitiveServices.Speech;
using DiScribe.DatabaseManager;
using DiScribe.AudioHandling;

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
        public TranscribeController(FileInfo meetingRecording, List<User> voiceprints, 
            SpeechConfig speechConfig, string speakerIDSubKey = "7fb70665af5b4770a94bb097e15b8ae0")
        {
            SpeechConfig = speechConfig;
            SpeakerIDSubKey = speakerIDSubKey;

            Voiceprints = voiceprints;
            FileSplitter = new AudioFileSplitter(meetingRecording);
            Transcriber = new SpeechTranscriber(this);
            Recognizer = new Recognizer(this);


            // Changes location of stored meeting minutes in release mode
            #if (DEBUG)
                MeetingMinutesFile = new FileInfo(@"../../../../Transcripts/minutes.txt");
            #else
                MeetingMinutesFile = new FileInfo(@"Transcripts/minutes.txt");
            #endif


            Console.WriteLine(">\tTranscription Controller initialized \n\t" +
                "on Audio Recording [" + meetingRecording.FullName + "].");
        }

        public List<User> Voiceprints { get; set; }

        internal AudioFileSplitter FileSplitter { get; set; }

        SpeechTranscriber Transcriber { get; set; }

        Recognizer Recognizer { get; set; }

        public SpeechConfig SpeechConfig { get; private set; }

        public string SpeakerIDSubKey { get; private set; }


        public FileInfo MeetingMinutesFile { get; private set; }

        public string Transcription { get; private set; }

        /// <summary>
        /// Uses Voiceprints to perform speaker recognition while transcribing the audio file MeetingRecording.
        /// Creates a formatted text output file holding the transcription.
        /// </summary>
        /// <returns>A FileInfo instance holding information about the transcript file that was created.</returns>
        public Boolean Perform(int lineLength = 120)
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

            MakeTranscription(lineLength);

            Console.WriteLine(">\tTranscription && Recognition Complete");
            return true;
        }



        public FileInfo WriteTranscriptionFile(string rid = "")
        {
           
            FileInfo transcript;
            if (rid.Equals(""))
                transcript = MeetingMinutesFile;
            else
                transcript = new FileInfo(MeetingMinutesFile.
                    FullName.Replace("minutes.txt", "minutes_" + rid + ".txt"));

            Console.WriteLine(">\tBegin Writing Transcription " +
                "& Speaker Recognition Result into File \n\t[" + transcript.Name + "]");
            

            try
            {
                 /* Overwrite any existing MeetingMinutes file with the same name,
                 * else create file. Output results to text file.
                 */
                if (!transcript.Directory.Exists)
                {
                    Console.WriteLine(">\tFile Path \n[" + transcript.Directory.FullName + "]\n Does Not Exist\n\t " +
                        "Creating the Directory name \t [" + transcript.Directory.Name + "]");
                    transcript.Directory.Create();
                }
                transcript.Create().Close();

                 /*Write transcription to file */
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(transcript.FullName, false))
                {
                    file.Write(Transcription);
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


        private void MakeTranscription(int lineLength = 120)
        {

            /*Iterate over the list of TranscrtiptionOutputs in order and add them to
             * output that will be written to file.
             * Order is by start offset.
             * Uses format set by TranscriptionOutput.ToString(). Also does text wrapping
             * if width goes over limit of chars per line.
             */
            StringBuilder output = new StringBuilder();
            bool last_was_nomatch = false;
            foreach (var curNode in Transcriber.TranscriptionOutputs)
            {
                if (!curNode.Value.TranscriptionSuccess
                    && last_was_nomatch)
                    continue;

                string curSegmentText = curNode.Value.ToString();
                if (curSegmentText.Length > lineLength)
                {
                    curSegmentText = Helper.WrapText(curSegmentText, lineLength);
                }
                output.AppendLine(curSegmentText + "\n");

                if (!curNode.Value.TranscriptionSuccess)
                    last_was_nomatch = true;
                else
                    last_was_nomatch = false;
            }

            Transcription = output.ToString();


        }
    }
}
