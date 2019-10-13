using FuturistTranscriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;

namespace FuturistTranscriber.TranscribeAgent
{
    class AudioSegment
    {
        public AudioSegment()
        {

        }

        public PushAudioInputStream AudioStream { get; set; }

        public int Offset { get; set; }

        public User SpeakerInfo { get; set; }

    }
}
