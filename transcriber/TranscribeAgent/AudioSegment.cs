using System;
using transcriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;

namespace transcriber.TranscribeAgent
{
    /// <summary>
    /// Represents a segment of an audio recording which has an offset in seconds from the beginning
    /// of the recording. Provides access to the audio data via an audio stream. Also includes a
    /// representation of the user who is speaking in the segment.
    /// </summary>
    class AudioSegment : System.IComparable
    {
        public AudioSegment(PullAudioInputStream audioStream, double startOffset, double endOffset, User speakerInfo)
        {
            AudioStream = audioStream;
            StartOffset = startOffset;
            EndOffset = endOffset;
            SpeakerInfo = speakerInfo;
        }


        /// <summary>
        /// Stream providing access to the audio data for this instance.
        /// </summary>
        public PullAudioInputStream AudioStream { get; set; }

        /// <summary>
        /// Offset of audio segment from the beginning of the recording.
        /// </summary>
        public double StartOffset { get; set; }

        public double EndOffset { get; set; }

        /// <summary>
        /// Info about the speaker in this instance.
        /// </summary>
        public User SpeakerInfo { get; set; }


        public int CompareTo(object obj)
        {
            AudioSegment otherSegment = obj as AudioSegment;

            return StartOffset.CompareTo(otherSegment.StartOffset);
        }
    }
}
