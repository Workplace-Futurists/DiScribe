using System;
using System.Collections.Generic;
using SendGrid.Helpers.Mail;

namespace EmailControllers
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> names = new List<string>();
            names.Add("Kevin");
            names.Add("Workplace-futurists");

            List<string> emails = new List<string>();
            names.Add("seungwook.l95@gmail.com");
            names.Add("workplace-futurists@hotmail.com");

            string startDate = "11/26/2019 18:00:00";
            string duration = "30";

            string accessCode = MeetingController.CreateWebExMeeting(names, emails, startDate, duration);
            Console.WriteLine("AccessCode: " + accessCode);
        }
    }
}
