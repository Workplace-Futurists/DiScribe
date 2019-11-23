using System;

namespace EmailController
{
    class Program
    {
        static void Main(string[] args)
        {
            XMLHelper.GetMeetingAttendee("https://companykm.my.webex.com/WBXService/XMLService", "623686431");
            // XMLHelper.PostXMLRequest("https://companykm.my.webex.com/WBXService/XMLService");
        }
    }
}
