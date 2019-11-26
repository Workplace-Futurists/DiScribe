using System;
using DiscribeDebug;


namespace DiscribeDebug
{
    class Program
    {
        static void Main(string[] args)
        {

            /*Do the test with MultipleSpeakers.wav*/
            //TranscriptionTest.TestTranscription(@"../../../../Record/MultipleSpeakers.wav");

            /*Do testing to see if reognition was successful */
            RegAudioTest.TestRegAudio("b.kernighan@example.com");
            Console.WriteLine("Finished writing audio file");






        }
    }
}
