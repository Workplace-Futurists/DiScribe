using System;
using System.Threading.Tasks;
using EmailController;

namespace Scheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeSpan runInterval = new TimeSpan(100);
            try
            {
                while (true)
                {
                    // check if theres new emails
                    Console.WriteLine(">\tChecking if there is new Email..");
                    if (!GraphHelper.ifNewEmail().Result)
                    {
                        Console.WriteLine(">\tFound new Email");
                        // get accesscode
                        //var access_code = GraphHelper.GetEmailMeetingNumAsync();

                        // get attendees email list
                        Console.WriteLine(">\t Retrieving Access code which is 622784672 [hardcoded]");
                        var attendees_emails = XMLHelper.GetAttendeeEmails("622784672");
                        // TODO check with database if any of them arent registered
                        // sends email to unregistered users with registration link
                        Console.WriteLine(">\tSending emails to ...");
                        EmailController.EmailController.SendEmailForVoiceRegistration(attendees_emails);
                        // TODO refers to database, schedules any meeting that is about to happen within 30 minutes
                    }
                    Task.Delay(runInterval).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error trying to get : " + ex.Message);
            }
        }
    }
}
