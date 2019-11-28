using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace DiScribe.Scheduler
{
    public static class SchedulerController
    {
        public static void CreateMeeting()
        {
            // TODOs
            // check if any invited users in the webex meeting are unregistered
            // send emails to them
            // schedule the task
        }

        /// <summary>
        /// Runs the meeting function at the specified time using the access code as param to the function. Waits
        /// asynchronously for meeting time.
        /// </summary>
        /// <param name="meetingFunction"></param>
        /// <param name="meetingAccessCode"></param>
        /// <param name="dateTime"></param>
        public static async void Schedule(Func<string, IConfigurationRoot, int> meetingFunction, string meetingAccessCode, IConfigurationRoot appConfig, DateTime dateTime)
        {
            Task meetingTask = ScheduleHelperAsync(meetingFunction, meetingAccessCode, appConfig, dateTime);
            await meetingTask;
        }


        /// <summary>
        /// Runs the meeting function at the scheduled time. Waits asynchronously until the meeting time occurs.
        /// </summary>
        /// <param name="meetingFunction"></param>
        /// <param name="meetingAccessCode"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static async Task ScheduleHelperAsync(Func<string, IConfigurationRoot, int> meetingFunction, string meetingAccessCode, IConfigurationRoot appConfig, DateTime dateTime)
        {
            var difference = (int)(dateTime - DateTime.Now).TotalMilliseconds;
            if (difference > 0)
                await Task.Delay(difference);                                  //Wait async until the meeting time.


            Task meetingTask = Task.Run(() =>
            {
                meetingFunction(meetingAccessCode, appConfig);
            });

            await meetingTask;
        }
    }
}