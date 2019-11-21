using SpeakerRegistration.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace transcriber.TranscribeAgent
{
    /// <summary>
    /// Contains data required to begin the transcription process.
    /// This data gives access to the location of the meeting audio recording file, the set of participant voiceprints,
    /// and the target email to send the transcription.
    /// </summary>
    public class TranscriptionInitData
    {
        public TranscriptionInitData(FileInfo meetingRecording, List<User> voiceprints, string targetEmail)
        {
            Console.WriteLine(">\tInitializing Transcription");
            MeetingRecording = meetingRecording;
            Voiceprints = voiceprints;
            TargetEmail = targetEmail;
        }

        public FileInfo MeetingRecording {get; set;}

        public List<User> Voiceprints { get; set; }

        public string TargetEmail { get; set; }
    }
}
