using System;
using System.Collections.Generic;
using System.IO;
using DiScribe.DatabaseManager;
using DiScribe.DatabaseManager.Data;
using DiScribe.DiScribeDebug;
using Microsoft.ProjectOxford.SpeakerRecognition;

namespace DiScribe.DiScribeDebug
{
    class Program
    {
        static void Main(string[] args)
        {

            string connStr = "Server = tcp:dbcs319discribe.database.windows.net, 1433; Initial Catalog = db_cs319_discribe; Persist Security Info = False; User ID = obiermann; Password = JKm3rQ~t9sBiemann; MultipleActiveResultSets = True; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30";

            DatabaseController.Initialize(connStr);

            User testUser1 =  DatabaseController.LoadUser("kengqiangmk@gmail.com");
            User testUser2 = DatabaseController.LoadUser("oloff8@hotmail.com");

            var testUsers = new List<User>() { testUser1, testUser2 };

            Console.WriteLine($"loaded test users {testUser1.Email} and {testUser2.Email}");

            Meeting tm = DatabaseController.CreateMeeting(new List<string> { testUser1.Email, testUser2.Email },
                DateTime.Now,
                DateTime.Now.AddMinutes(45.0),
                "382688282",
                "another subject");

            Console.WriteLine("Created meeting with row id " + tm.MeetingId);

            tm.MeetingMinutes = "Some \r\n meeting \r\n minutes \r\n test";
            tm.MeetingFileLocation = @"C:\something\file.txt";


            if (DatabaseController.UpdateMeeting(tm))
                Console.WriteLine("Successfully updated meeting record");

            else
                Console.WriteLine("Updating meeting record failed");



        }
    }
}
