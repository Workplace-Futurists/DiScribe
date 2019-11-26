using System;
using System.IO;
using DiscribeDebug;



namespace DiscribeDebug
{
    class Program
    {
        static void Main(string[] args)
        {
          
            /*Do the test with MultipleSpeakers.wav*/
            TrancriptionTest.TestTranscription(@"../../../../Record/test_meeting.wav");


            Console.WriteLine("Finished writing audio file");






        }
    }
}
