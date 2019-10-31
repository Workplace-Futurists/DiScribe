using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;
using transcriber.TranscribeAgent;

namespace transcriber.TranscribeAgent
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Creating transcript...");

            string path = @"../../../record/test_meeting.wav";
            FileInfo test = new FileInfo(path);
            var x = new AudioFileSplitter(null, test);

            var list = x.SplitAudio();                                   //Split audio into segments (only 1 in this case).

            var segment = list[list.Keys[0]];                            //Get the 1 segment.

            Speechtranscriber.RecognitionWithPullAudioStreamAsync(segment.AudioStream).Wait();

            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }

    }
}
