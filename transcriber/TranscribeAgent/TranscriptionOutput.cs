using System;
using System.Collections.Generic;
using System.Text;
using transcriber.Data;

namespace transcriber.TranscribeAgent
{
    class TranscriptionOutput : IComparable
    {
        public TranscriptionOutput(string text, User speaker, int startOffset)
        {
            Text = text;
            Speaker = speaker;
            StartOffset = startOffset;
        }

        public string Text { get; set; }

        public User Speaker { get; set; }

        public int StartOffset { get; set; }

        public int CompareTo(object obj)
        {
            return StartOffset.CompareTo((obj as TranscriptionOutput).StartOffset);
        }

        public override string ToString()
        {
            return Speaker.Name + "\t" + formatTime(StartOffset) + Text;
        }

        public static string formatTime(int offsetSeconds)
        {
            int hours = offsetSeconds / 3600;
            int secondsLeft = offsetSeconds - hours * 3600;

            int minutes = (secondsLeft > 0) ? secondsLeft / 60 : 0;
            secondsLeft = secondsLeft - minutes * 60;

            return $"{hours}:{minutes}:{secondsLeft}";
        }


    }
}
