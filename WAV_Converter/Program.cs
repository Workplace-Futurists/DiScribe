using System;
using Transcriber;
using DatabaseController;
using System.Collections.Generic;

namespace WAV_Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start To Convert");
            byte[] bytes = System.IO.File.ReadAllBytes(@"C:\Users\Kay\Desktop\CS319\Recordings\Kevin.wav");
            DatabaseController.Data.UserParams user = new DatabaseController.Data.UserParams(bytes, "Kevin","a", "seungwook.l95@gmail.com");
            var controller = RegistrationController.BuildController(new List<string>());
            controller.CreateUserProfile(user).Wait();
            //Console.Write(bytes);
            Console.ReadLine();
        }
    }
}
