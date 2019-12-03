using System;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;



namespace DiScribe.AudioHandling
{
    /// <summary>
    /// Represents a segment of an audio recording which has an offset in milliseconds from the beginning
    /// of the recording. Provides access to the audio data via an audio stream. Also includes a
    /// representation of the user who is speaking in the segment.
    /// </summary>
    public class AudioSegment : System.IComparable
    {
        public AudioSegment(byte[] audioData, long startOffset, long endOffset,
            uint sampleRate = SAMPLE_RATE, byte bitsPerSample = BITS_PER_SAMPLE, byte channels = CHANNELS)
        {
            MemoryStream tempStream = new MemoryStream(audioData);
            AudioStreamFormat streamFormat = AudioStreamFormat.GetWaveFormatPCM(sampleRate, bitsPerSample, channels);

            AudioStream = AudioInputStream.CreatePullStream(new BinaryAudioStreamReader(tempStream), streamFormat);

            AudioData = audioData;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        const uint SAMPLE_RATE = 16000;
        const byte BITS_PER_SAMPLE = 16;
        const byte CHANNELS = 1;

        /// <summary>
        /// Stream providing access to the audio data for this instance.
        /// </summary>
        public PullAudioInputStream AudioStream { get; set; }

        /// <summary>
        /// Data used by AudioStream
        /// </summary>
        public byte[] AudioData { get; private set; }

        /// <summary>
        /// Offset of audio segment from the beginning of the recording.
        /// </summary>
        public long StartOffset { get; set; }

        public long EndOffset { get; set; }

        public int CompareTo(object obj)
        {
            AudioSegment otherSegment = obj as AudioSegment;

            return StartOffset.CompareTo(otherSegment.StartOffset);
        }
    }
}
