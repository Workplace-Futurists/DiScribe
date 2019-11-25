using System;
using System.Collections.Generic;
using System.Text;

namespace Scheduler
{
	class Program
	{
		static void Main(string[] args)
		{
			TranscribeScheduler.ScheduleTask("000000", DateTime.Now.AddSeconds(5),
				"Main.exe", "C:\\CPSC319\\cs319-2019w1-hsbc\\Main\\bin\\Debug\\netcoreapp3.0");
		}
	}
}
