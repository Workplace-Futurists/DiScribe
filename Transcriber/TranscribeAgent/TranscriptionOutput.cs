using System;
using System.Collections.Generic;
using System.Text;
using DatabaseController.Data;
using Transcriber.Audio;

namespace Transcriber
{
    class TranscriptionOutput : IComparable
    {
        public TranscriptionOutput(string text, Boolean transcriptionSuccess, AudioSegment segment, User speaker = null)
        {
            Text = text;
            StartOffset = (int)segment.StartOffset;
            EndOffset = (int)segment.EndOffset;
            TranscriptionSuccess = transcriptionSuccess;
            Segment = segment;
            Speaker = speaker;
        }

        public string Text { get; set; }

        public long StartOffset { get; set; }

        public long EndOffset { get; set; }

        public AudioSegment Segment { get; set; }

        public User Speaker { get; set; }

        public Boolean TranscriptionSuccess { get; set; }

        public int CompareTo(object obj)
        {
            return StartOffset.CompareTo((obj as TranscriptionOutput).StartOffset);
        }

        public override string ToString()
        {
            string speaker_string;
            if (Speaker is null)
                speaker_string = $"UNRECOGNIZED";
            else
                speaker_string = $"{Speaker.FirstName} {Speaker.LastName}";
            return "[" + speaker_string + $"]\t{FormatTime(StartOffset)}\t{Text}";
        }

        /// <summary>
        /// Formats a time in milliseconds to format h:m:s
        /// </summary>
        /// <param name="offsetMS"></param>
        /// <returns></returns>
        public static string FormatTime(long offsetMS)
        {
            long hours = offsetMS / 3600000;
            long minutes = (offsetMS / 60000) % 60;
            long seconds = (offsetMS / 1000) % 60;

            return $"{hours}:{minutes}:{seconds}";
        }
    }
}
