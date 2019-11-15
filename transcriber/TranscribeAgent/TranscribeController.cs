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
        public TranscribeController(SpeechConfig speechConfig, string speakerIDKey, FileInfo meetingRecording, List<Voiceprint> voiceprints, FileInfo meetingMinutes)
        {
            MeetingRecording = meetingRecording;
            MeetingMinutes = meetingMinutes;

            Transcriber = new SpeechTranscriber(speechConfig, speakerIDKey, meetingRecording, meetingMinutes, voiceprints);

        }

        /// <summary>
        /// File details for audio file containing meeting recording.
        /// </summary>
        public FileInfo MeetingRecording { get; set; }

        public SpeechTranscriber Transcriber {get; private set;}
        
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
               Transcriber.CreateTranscription().Wait();                 //Wait synchronously for transcript to be finished and written to minutes file.
                           
            } catch (Exception transcribeEx)
              {
                  Console.Error.Write("Mission failed. No transcription could be created from audio segments. " + transcribeEx.Message);
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
