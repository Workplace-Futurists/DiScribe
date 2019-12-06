using System;

namespace DiScribe.Meeting
{
    public class MeetingInfo
    {
        public MeetingInfo()
        {

        }

        public MeetingInfo(string meeting_access_code,
            string password,
            DateTime start_time)
        {
            AccessCode = meeting_access_code;
            Password = password;
            StartTime = start_time;
        }

        public bool MissingField()
        {
            return AccessCode == "" || Password == "" || StartTime.Equals(new DateTime());
        }

        public string AccessCode { get; set; }

        public string Password { get; set; }

        public DateTime StartTime { get; set; }

        public string Subject { get; set; }
    }
}
