using System;


namespace DiScribe.DatabaseManager.Data
{
    public class Meeting : DataElement
    {
        public Meeting(string MeetingSubject,
            DateTime StartTime,
            DateTime EndTime,
            string MeetingCode)
        {
            this.MeetingSubject = MeetingSubject;
            this.StartTime = StartTime;
            this.EndTime = EndTime;
            this.MeetingCode = MeetingCode;
        }

        public string MeetingSubject { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string MeetingCode { get; set; }

        public override bool Delete()
        {
            throw new NotImplementedException();
        }

        public override bool Update(string lookup)
        {
            throw new NotImplementedException();
        }
    }
}
