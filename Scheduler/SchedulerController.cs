using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using DiScribe.Meeting;


namespace DiScribe.Scheduler
{
    public static class SchedulerController
    {
        /// <summary>
        /// Runs the meeting function at the specified time using the access code as param to the function. Waits
        /// asynchronously for meeting time.
        /// </summary>
        /// <param name="meetingFunction"></param>
        /// <param name="dateTime"></param>
        public static async Task Schedule(Func<MeetingInfo, IConfigurationRoot, int> meetingFunction, MeetingInfo meetingInfo, IConfigurationRoot appConfig, DateTime dateTime)
        {
            Task meetingTask = ScheduleHelperAsync(meetingFunction, meetingInfo, appConfig, dateTime);
            await meetingTask;
        }

        /// <summary>
        /// Runs the meeting function at the scheduled time. Waits asynchronously until the meeting time occurs.
        /// </summary>
        /// <param name="meetingFunction"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static async Task ScheduleHelperAsync(Func<MeetingInfo, IConfigurationRoot, int> meetingFunction, MeetingInfo meetingInfo, IConfigurationRoot appConfig, DateTime dateTime)
        {
            var difference = (int)(dateTime - DateTime.Now).TotalMilliseconds;
            if (difference > 0)
                await Task.Delay(difference);                                  //Wait async until the meeting time.

            Task meetingTask = Task.Run(() =>
            {
                meetingFunction(meetingInfo, appConfig);
            });

            await meetingTask;
        }
    }
}

namespace DiScribe.Scheduler.Windows
{
    using Microsoft.Win32.TaskScheduler;

    public static class SchedulerController
    {
        /// <summary>
        /// Schedule a Task through Windows Task Scheduler
        /// </summary>
        /// <param name="meetingID"></param>
        /// <param name="startTime"></param>
        /// <returns></returns>
        public static Boolean ScheduleTask(string meetingID, DateTime startTime, string appName, string rootDir = "")
        {
            // Create a new task
            string taskName = "Dial into webex meeting at " + startTime.ToLongDateString();

            Task t = TaskService.Instance.AddTask(taskName,
              new TimeTrigger()
              {
                  StartBoundary = startTime,
                  Enabled = true
              },
              new ExecAction(appName, meetingID, rootDir));

            return true;
        }
    }
}