using System;
using Microsoft.Win32.TaskScheduler;


namespace DiScribe.Scheduler
{
	public static class TranscribeScheduler
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

			TaskService.Instance.AddTask(taskName,
			new TimeTrigger()
			{
				StartBoundary = startTime,
				Enabled = true
			},
				new ExecAction(appName, meetingID, rootDir),
                "obiermann", "JKm3rQ~t9sOB"
                );
			return true;
		}
	}
}
