using System;
using System.Collections.Generic;
using System.Text;
using transcriber.Data;

namespace transcriber.TranscribeAgent
{
    public class TranscriptionOutput : IComparable
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
            return Speaker.Name + "\t" + formatTime(StartOffset) + "\t" + Text;
        }

        /// <summary>
        /// Formats a time in milliseconds to format h:m:s
        /// </summary>
        /// <param name="offsetMS"></param>
        /// <returns></returns>
        public static string formatTime(int offsetMS)
        {
            int hours = offsetMS / 3600000;
            int minutes = (offsetMS / 6000) % 60;
            int seconds = (offsetMS / 1000) % 60;
            
            return $"{hours}:{minutes}:{seconds}";
        }


    }
}
