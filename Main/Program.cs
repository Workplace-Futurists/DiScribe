using System;
using EmailController;

namespace Main
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");


            // FROM THIS LINE IS SENDING EMAIL COMPONENT
            // Initialize SendGrid client and send email
            EmailController.EmailController.Initialize();
            EmailController.EmailController.SendMail();
            
        }
    }
}
