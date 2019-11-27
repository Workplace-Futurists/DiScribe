using System;
using System.IO;
using DiScribe.DiScribeDebug;


namespace DiScribe.DiScribeDebug
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
