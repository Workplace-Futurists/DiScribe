using System;
using EmailController;


namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            EmailController.EmailController.Initialize();
            EmailController.EmailController.SendMail();
        }
    }
}
