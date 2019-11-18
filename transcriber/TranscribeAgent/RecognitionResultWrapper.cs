﻿using Microsoft.Cognitive.SpeakerRecognition.Streaming.Result;
using System;
using System.Collections.Generic;
using System.Text;

namespace transcriber.TranscribeAgent
{
    public class RecognitionResultWrapper
    {
        public RecognitionResultWrapper(long start, long end, RecognitionResult result)
        {
            Start = start;
            End = end;
            Result = result;
        }

        /// <summary>
        /// Offset in milliseconds from the beginning of audio where the speaker
        /// in this result started speaking.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Offset in milliseconds from the beginning of audio where the speaker
        /// in this result stopped speaking.
        /// </summary>
        public long End { get; set; }

        /// <summary>
        /// The <see cref="RecognitionResult"/> for the speaker that was recognized.
        /// Gives access to the GUID for this speaker.
        /// </summary>
        public RecognitionResult Result { get; set; }



    }
}