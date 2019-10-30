using transcriber.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        public TranscribeController(FileInfo meetingRecording, List<Voiceprint> voiceprints)
        {
            MeetingRecording = meetingRecording;
            Voiceprints = voiceprints;

        }

        public FileInfo MeetingRecording { get; set; }

        public List<Voiceprint> Voiceprints{ get; set;}

        /// <summary>
        /// Uses Voiceprints to perform speaker recognition while transcribing the audio file MeetingRecording.
        /// Creates a formatted text output file holding the transcription.
        /// </summary>
        /// <returns>A FileInfo instance holding information about the transcript file that was created.</returns>
        public FileInfo DoTranscription()
        {
            return new FileInfo("");
        }

        public static void SendEmail(FileInfo transcript, string targetEmail, string body = "", string subject = "")
        {

        }
    }
}
