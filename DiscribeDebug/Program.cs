using System;
using System.IO;
using DiScribe.DatabaseManager.Data;
using DiScribe.DiScribeDebug;


namespace DiScribe.DiScribeDebug
{
    class Program
    {
        static void Main(string[] args)
        {
            //var userParams = RegAudioTest.MakeTestVoiceprints(new FileInfo(@"../../../../Record/RE1653360c6857790dd9ebe854b79e1b86.wav"));


            //userParams[0].Email = "somethingelse@example.com";



            //TrancriptionTest.TestTranscription(@"../../../../Record/RE1653360c6857790dd9ebe854b79e1b86.wav");



            //Guid enrolled = RegAudioTest.TestEnroll(userParams[0]);

            //Console.WriteLine("Enrolled " + enrolled);


            //User existing = RegAudioTest.TestLoadUser(userParams[0].Email);
            //Console.WriteLine("Did load " + existing.Email);



            User test = RegAudioTest.TestLoadUser("kengqiangmk@gmail.com");

            if (test == null)
                Console.WriteLine("Fake User not exist");

            else
                Console.WriteLine(test.ToString());

        }
    }
}
