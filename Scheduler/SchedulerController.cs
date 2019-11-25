using System;
using System.Threading;
using System.Threading.Tasks;


namespace Scheduler
{
    public static class SchedulerController
    {
        public static void Schedule(string meetingAccessCode, DateTime dateTime)
        {
            Thread thread = new Thread(() => ScheduleHelperAsync(meetingAccessCode, dateTime));
            thread.Start();
        }

        private static void ScheduleHelperAsync(string meetingAccessCode, DateTime dateTime)
        {
            var difference = (int)(dateTime - DateTime.Now).TotalMilliseconds;
            if (difference > 0)
                Task.Delay(difference).Wait();

            //Main.Program.Run(meetingAccessCode);
        }
    }
}
