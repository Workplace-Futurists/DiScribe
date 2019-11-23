using System;
using System.Threading.Tasks;
using EmailController;

namespace Scheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeSpan runInterval = new TimeSpan(30);
            try
            {
                while (true)
                {
                    // check if theres new emails
                    if (GraphHelper.ifNewEmail().Result)
                    {
                        // TODO get accesscode
                        var access_code = GraphHelper.GetEmailMeetingNumAsync();
                        // TODO get attendees email list
                        // TODO check with database if any of them arent registered
                        // TODO sends email to unregistered users with registration link
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
