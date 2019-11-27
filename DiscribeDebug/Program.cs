using System;
using System.IO;
using DiScribe.DiscribeDebug;


namespace DiScribe.DiscribeDebug
{
    class Program
    {
        static void Main(string[] args)
        {
            /*Do the test with MultipleSpeakers.wav*/
            TrancriptionTest.TestTranscription(@"../../../../Record/test_meeting.wav");
        }
    }
}
