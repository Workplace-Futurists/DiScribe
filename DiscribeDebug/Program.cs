using System;
using System.IO;
using DiScribe.DatabaseManager.Data;
using DiScribe.DiScribeDebug;
using Microsoft.ProjectOxford.SpeakerRecognition;

namespace DiScribe.DiScribeDebug
{
    class Program
    {
        static void Main(string[] args)
        {
            

            TrancriptionTest.TestTranscription(@"test_meeting.wav");


        }
    }
}
