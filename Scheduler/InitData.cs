﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Scheduler
{
    public class InitData
    {
        public InitData(string meetingAccessCode, Boolean debug)
        {
            MeetingAccessCode = meetingAccessCode;
            Debug = debug;
        }

        public string MeetingAccessCode { get; set; }

        public Boolean Debug { get; set; }
    }
}
