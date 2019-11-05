using Microsoft.Cognitive.SpeakerRecognition.Streaming.Result;
using System;
using System.Collections.Generic;
using System.Text;

namespace transcriber.TranscribeAgent
{
    class RecognitionResultWrapper
    {
        public RecognitionResultWrapper(int start, int end, RecognitionResult result)
        {
            Start = start;
            End = end;
            Result = result;
        }

        /// <summary>
        /// Offset in seconds from the beginning of audio where the speaker
        /// in this result started speaking.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Offset in seconds from the beginning of audio where the speaker
        /// in this result stopped speaking.
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// The <see cref="RecognitionResult"/> for the speaker that was recognized.
        /// Gives access to the GUID for this speaker.
        /// </summary>
        public RecognitionResult Result { get; set; }



    }
}
