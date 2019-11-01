using transcriber.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CognitiveServices.Speech;

namespace transcriber.TranscribeAgent
{
    class TranscribeController
    {
        /// <summary>
        /// Presents an interface to the speaker-recognition based transcription functionality.
        /// Allows the use of a set of Voiceprints to perform transcription of a meeting audio file. 
        /// Also supports emailing of a transcription file.
        /// </summary>
        /// <param name="meetingRecording"></param>
        /// <param name="voiceprints"></param>
        public TranscribeController(SpeechConfig config, FileInfo meetingRecording, List<Voiceprint> voiceprints, FileInfo outFile)
        {
            MeetingRecording = meetingRecording;
            Voiceprints = voiceprints;
            OutFile = outFile;
            Config = config;
        }

        /// <summary>
        /// File details for audio file containing meeting recording.
        /// </summary>
        public FileInfo MeetingRecording { get; set; }

        /// <summary>
        /// List of voiceprints for users involved in this meeting.
        /// </summary>
        public List<Voiceprint> Voiceprints{ get; set;}

        public FileInfo OutFile { get; set; }

        SpeechConfig Config { get; set; }

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
            AudioFileSplitter splitter = new AudioFileSplitter(Voiceprints, MeetingRecording);
            SortedList<AudioSegment, AudioSegment> audioSegments;
            try
            {
                //Split audio using speaker recognition. List of AudioSegments is sorted by timestamp
                audioSegments = splitter.SplitAudio();
                            
            } catch(Exception splitEx)
              {
                  Console.Error.WriteLine("Splitting meeting audio failed. \n" + splitEx.Message);
                  return false;
              }

            try
            {
                var transcriber = new SpeechTranscriber(Config, audioSegments, OutFile);
                transcriber.CreateTranscription();                 //Create transcription, update MeetingMinutes property with file location.
            }catch(Exception transcribeEx)
             {
                Console.Error.Write("Mission failed. No transcription could be created from audio segments.\n" + transcribeEx.Message);
                return false;
             }

            return true;
        }






        public Boolean SendEmail(string targetEmail, string subject = "")
        {

            return false;
        }
    }
}
