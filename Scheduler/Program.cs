using System;
using System.Threading.Tasks;
using EmailController;

namespace Scheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var access_code = "627709307";
                // get attendees email list
                Console.WriteLine(">\t Retrieving Access code which is " + access_code + " [hardcoded]");
                var attendees_emails = XMLHelper.GetAttendeeEmails(access_code);
                // TODO check with database if any of them arent registered

                // sends email to unregistered users with registration link
                Console.WriteLine(">\tSending emails to ...");
                EmailController.EmailController.Initialize();
                EmailController.EmailController.SendEmailForVoiceRegistration(attendees_emails);
                // TODO refers to database, schedules any meeting that is about to happen within 30 minutes

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error trying to get : " + ex.Message);
            }
        }
    }
}
