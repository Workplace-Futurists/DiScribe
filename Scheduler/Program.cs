using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

namespace Scheduler
{
    /// <summary>
    /// Testing the Scheduler
    /// </summary>
	class Program
	{
		static void Main(string[] args)
		{
            InitData initData = new InitData("000000", false);
            string jsonString = JsonConvert.SerializeObject(initData);

            /*Start the dialer/transcriber in 5 seconds from now */
			TranscribeScheduler.ScheduleTask(jsonString, DateTime.Now.AddSeconds(5),
				"Main.exe", "C:\\CPSC319\\cs319-2019w1-hsbc\\Main\\bin\\Debug\\netcoreapp3.0");
		}
	}
}
