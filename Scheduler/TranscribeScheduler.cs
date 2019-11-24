using System;
using Microsoft.Win32.TaskScheduler;


namespace Scheduler
{
    class TranscribeScheduler
    {
        public TranscribeScheduler()
        {
        }

        /// <summary>
        /// Schedule a Task through Windows Task Scheduler
        /// </summary>
        /// <param name="meetingID"></param>
        /// <param name="startTime"></param>
        /// <returns></returns>
        public static Boolean ScheduleTask(string meetingID, DateTime startTime, string appName, string rootDir = "")
        {
            // Create a new task
            string taskName = "Dial into webex meeting at " +startTime.ToLongDateString();
            
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
