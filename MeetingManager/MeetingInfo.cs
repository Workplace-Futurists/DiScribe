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
            Meeting = new DatabaseManager.Data.Meeting();
            Names = new List<string>();
            HostInfo = new WebexHostInfo();
            AttendeesEmails = new List<SendGrid.Helpers.Mail.EmailAddress>();
            Meeting.MeetingStartDateTime = new DateTime();
            Meeting.MeetingEndDateTime = new DateTime();

        }


        public MeetingInfo(DatabaseManager.Data.Meeting meeting, List<EmailAddress> attendeesEmails, 
            string password = "", WebexHostInfo hostInfo = null)

        {
            Meeting = meeting;
            AttendeesEmails = attendeesEmails;
            HostInfo = hostInfo;

            Names = GetNames();
        }


        /// <summary>
        /// Determine if there are any misisng fields in this instance. Password
        /// is considered optional, so it is not checked
        /// </summary>
        /// <returns></returns>
        public bool MissingField()
        {
            return Subject == "" || AttendeesEmails.Count == 0 ||
                StartTime.Equals(new DateTime()) || EndTime.Equals(new DateTime())
                || AccessCode == "";
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


        /// <summary>
        /// Get attendee emails as a list of strings with addresses.
        /// </summary>
        /// <returns></returns>
        public List<string> GetStringEmails()
        {
            var emails = new List<string>();
            foreach (var email in AttendeesEmails)
            {
                emails.Add(email.Email);
            }

            return emails;
        }


        /// <summary>
        /// Get the meeting duration in minutes.
        /// </summary>
        /// <returns></returns>
        public double GetDuration()
        {
            return StartTime.Subtract(EndTime).TotalMinutes;

        }

        private List<string> GetNames()
        {
            List<string> names = new List<string>();
            foreach (var email in AttendeesEmails)
            {
                names.Add(email.Name);
            }

            return names;
        }



        public string Subject
        {
            get
            {
                return Meeting.MeetingSubject;
            }

            set
            {
                Meeting.MeetingSubject = value;
            }
        }

        /// <summary>
        /// The emails of meeting attendees. Note that if emails are updated, then names are also updated.
        /// </summary>
        public List<EmailAddress> AttendeesEmails 
        { 
                get { return _AttendeeEmails; } 
                set {       
                         _AttendeeEmails = value;
                        foreach (var curEmail in value)
                        {
                            if (curEmail == null || curEmail.Name == null)
                                continue;
                             Names.Add(curEmail.Name);
                        }
                } 
        }

        public DateTime StartTime 
        {
            get
            {
                return Meeting.MeetingStartDateTime;
            }
                
            set
            {
                Meeting.MeetingStartDateTime = value;
            }
        }

        public DateTime EndTime
        {
            get
            {
                return Meeting.MeetingEndDateTime;
            }

            set
            {
                Meeting.MeetingEndDateTime = value;
            }
        }


        public string AccessCode
        {
            get
            {
                return Meeting.WebExID;
            }

            set
            {
                Meeting.WebExID = value;
            }
        }

        public string Password { get; set; }


        public DatabaseManager.Data.Meeting Meeting { get; set; }


        public List<string> Names { get; set; }




        public WebexHostInfo HostInfo { get; set; }

        private List<EmailAddress> _AttendeeEmails; 
    }
}
