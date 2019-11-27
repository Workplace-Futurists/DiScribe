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
            TrancriptionTest.TestTranscription(@"../../../../Record/REb3041d034af93ae9386f76f7bf78a687.wav");


            Console.WriteLine("Finished writing audio file");






        }
    }
}
