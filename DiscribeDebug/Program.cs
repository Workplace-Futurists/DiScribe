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

            User testUser = RegAudioTest.TestLoadUser("testad12@gmail.com");

            Console.WriteLine(testUser.FirstName);

            TrancriptionTest.TestTranscription(@"REb3a5300a60b1641db4c633564c5673aa.wav");


        }
    }
}
