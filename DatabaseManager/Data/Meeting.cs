using System;
using System.Collections.Generic;
using System.Text;

namespace DiScribe.DatabaseManager.Data
{
    public class Meeting : DataElement
    {
        public Meeting()
        {
            MeetingId = -1;
            MeetingSubject = "";
            MeetingMinutes = "";
            MeetingStartDateTime = default;
            MeetingEndDateTime = default;
            MeetingFileLocation = "";

        }


        public Meeting(int meetingId,
            string meetingSubject = "",
            string meetingMinutes = "",
            DateTime meetingStartDateTime = default,
            DateTime meetingEndDateTime = default,
            string meetingFileLocation = "",
            string webExID = "")
        {

            MeetingId = meetingId;
            MeetingSubject = (meetingSubject is null ? "" : meetingSubject);
            MeetingMinutes = (meetingMinutes is null ? "" : meetingMinutes);
            MeetingStartDateTime = meetingStartDateTime;
            MeetingEndDateTime = meetingEndDateTime;
            MeetingFileLocation = (meetingFileLocation is null ? "" : meetingFileLocation);
            WebExID = (webExID is null ? "" : webExID);

        }


        override public Boolean Update(string lookupId = "")
        {
            return DatabaseController.UpdateMeeting(this);
                       

        }

        override public Boolean Delete()
        {
            throw new NotImplementedException("Meeting does not support delete");
        }



        public int MeetingId { get; private set; }
        public string MeetingSubject { get; set; }
        public string MeetingMinutes { get; set; }
        public DateTime MeetingStartDateTime { get; set; }
        public DateTime MeetingEndDateTime { get; set; }
        public string MeetingFileLocation { get; set; }
        public string WebExID { get; set; }







    }
}
