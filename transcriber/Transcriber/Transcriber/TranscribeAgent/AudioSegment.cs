using System;
using FuturistTranscriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;

namespace FuturistTranscriber.TranscribeAgent
{
    /// <summary>
    /// Represents a segment of an audio recording which has an offset in seconds from the beginning
    /// of the recording. Provides access to the audio data via an audio stream. Also includes a
    /// representation of the user who is speaking in the segment.
    /// </summary>
    class AudioSegment : System.IComparable
    {
        public AudioSegment(PushAudioInputStream audioStream, int offset, User speakerInfo)
        {
            AudioStream = audioStream;
            Offset = offset;
            SpeakerInfo = speakerInfo;
        }


        /// <summary>
        /// Stream providing access to the audio data for this instance.
        /// </summary>
        public PushAudioInputStream AudioStream { get; set; }

        /// <summary>
        /// Offset of audio segment from the beginning of the recording.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Info about the speaker in this instance.
        /// </summary>
        public User SpeakerInfo { get; set; }


        public int CompareTo(object obj)
        {
            AudioSegment otherSegment = obj as AudioSegment;

            return Offset.CompareTo(otherSegment.Offset);
        }
    }
}
