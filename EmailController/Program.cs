using System;
using System.Collections.Generic;
using SendGrid.Helpers.Mail;

namespace EmailController
{
    class Program
    {
        static void Main(string[] args)
        {
            // string accessCode = GraphHelper.GetEmailMeetingNumAsync().Result;
            //List<EmailAddress> emails = EmailController.GetAttendeeEmails("624308408");
            //foreach (EmailAddress email in emails)
            //{
            //    Console.WriteLine(email.Email);
            //}

            //Console.WriteLine();

            XMLHelper.createWebExMeeting();
        }
    }
}
