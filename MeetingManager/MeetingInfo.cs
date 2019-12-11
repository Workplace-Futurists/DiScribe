using System;
using System.Collections.Generic;
using SendGrid.Helpers.Mail;

namespace DiScribe.Meeting
{
    /// <summary>
    /// Represents meeting metadata. Specifically, meeting subject, participants, 
    /// start datetime, end datetime.
    /// </summary>
    public class MeetingInfo
    {
        public MeetingInfo()
        {

        }

        public MeetingInfo(string subject, List<EmailAddress> attendeesEmails,
            DateTime startTime, DateTime endTime = new DateTime(), string accessCode = "", string password = "", WebexHostInfo hostInfo = null)

        {
            Subject = subject;
            AttendeesEmails = attendeesEmails;
            StartTime = startTime;
            EndTime = endTime;
            AccessCode = accessCode;
            HostInfo = hostInfo;
        }

        /// <summary>
        /// Determine if there are any misisng fields in this instance.
        /// </summary>
        /// <returns></returns>
        public bool MissingField()
        {
            return Subject == "" || AttendeesEmails is null || AttendeesEmails.Count == 0 ||
                StartTime.Equals(new DateTime()) || EndTime.Equals(new DateTime())
                || AccessCode == "" || Password == "";
        }


        /// <summary>
        /// Determine if essential meeting access info is missing from this instace.
        /// </summary>
        /// <returns></returns>
        public bool MissingAccessInfo()
        {
            return AccessCode == ""
                || StartTime.Equals(new DateTime());
        }


        public string Subject { get; set; }

        public List<EmailAddress> AttendeesEmails { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string AccessCode { get; set; }

        public string Password { get; set; }

        public WebexHostInfo HostInfo { get; set; }
    }
}
