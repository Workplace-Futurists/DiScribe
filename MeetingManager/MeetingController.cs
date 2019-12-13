using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using DiScribe.Email;
using EmailAddress = SendGrid.Helpers.Mail.EmailAddress;
using DiScribe.DatabaseManager;
using System.Text.RegularExpressions;
using Microsoft.Graph;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

namespace DiScribe.Meeting
{
    public static class MeetingController
    {
        public static string BOT_EMAIL;




        /// <summary>
        /// Handles meeting invite and returns a MeetingInfo object representing the meeting.
        /// Separate cases for a Webex invite event and Outlook invites
        /// </summary>
        /// <param name="inviteEvent"></param>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        public static async Task<Meeting.MeetingInfo> HandleInvite(Microsoft.Graph.Event inviteEvent, WebexHostInfo hostInfo, string botEmail)
        {
            MeetingInfo meetingInfo = new MeetingInfo();

            /*If invite is a Webex from DiScribe website or from Webex website, parse email and use Webex API 
             to get meeting metadata.*/
            if (EmailListener.IsValidWebexInvitation(inviteEvent))
            {
                meetingInfo = EmailListener.GetMeetingInfoFromWebexInvite(inviteEvent, hostInfo);
            }

            /*Otherwise, handle invite event as an Outlook invite. Here, a new meeting is created
             * at the time requested in the event received. Metadata can be obtained from the event
             in this case.*/
            else
            {

                var something = inviteEvent.Start;


                /*Get start and end time in UTC */
                DateTime meetingStartUTC = DateTime.Parse(inviteEvent.Start.DateTime);
                DateTime meetingEndUTC = DateTime.Parse(inviteEvent.End.DateTime);

                /*Convert UTC start and end times to bot local system time */
                DateTime meetingStart = TimeZoneInfo.ConvertTimeFromUtc(meetingStartUTC, TimeZoneInfo.Local);
                DateTime meetingEnd = TimeZoneInfo.ConvertTimeFromUtc(meetingEndUTC, TimeZoneInfo.Local);

                var meetingDuration = meetingEnd.Subtract(meetingStart);
                string meetingDurationStr = meetingDuration.TotalMinutes.ToString();

                var attendeeNames = EmailListener.GetAttendeeNames(inviteEvent);
                var attendeeEmails = EmailListener.GetAttendeeEmails(inviteEvent).Distinct().ToList();

                /*Remove the bot email, as the bot must not receive another event. */
                foreach(var curEmail in attendeeEmails)
                {
                    if (curEmail == botEmail)
                    {
                        attendeeEmails.Remove(curEmail);
                        break;
                    }
                }

                meetingInfo = MeetingController.CreateWebexMeeting(inviteEvent.Subject, attendeeNames, attendeeEmails,
                    meetingStart, meetingDurationStr, hostInfo, inviteEvent.Organizer.EmailAddress);

            }


            return meetingInfo;



        }

















        /// <summary>
        /// Creates a webex meeting with the specified parameters. Note that duration is in minutes.
        /// </summary>
        /// <param name="meetingSubject"></param>
        /// <param name="names"></param>
        /// <param name="emails"></param>
        /// <param name="startDate"></param>
        /// <param name="duration"></param>
        /// <param name="hostInfo"></param>
        /// <returns></returns>
        public static MeetingInfo CreateWebexMeeting(string meetingSubject, List<string> names, List<string> emails, DateTime startTime, string duration, WebexHostInfo hostInfo,
            Microsoft.Graph.EmailAddress hostDelegate = default, string password = "")
        {
            string strXMLServer = "https://companykm.my.webex.com/WBXService/XMLService";

            WebRequest request = WebRequest.Create(strXMLServer);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Create POST data and convert it to a byte array.
            // string strXML = GenerateXMLCreateMeeting();

            // string strXML = File.ReadAllText(@"createMeeting.xml");


            string formattedStartTime = startTime.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            string strXML = XMLHelper.GenerateMeetingXML(meetingSubject, names, emails, formattedStartTime, duration, hostInfo, hostDelegate);

            byte[] byteArray = Encoding.UTF8.GetBytes(strXML);

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();

            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();


            // Display the content.
            string accessCode = XMLHelper.RetrieveAccessCode(responseFromServer);

            
            Console.WriteLine(accessCode);



            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();


                        
            var sendGridEmails = EmailListener.parseEmailList(emails);


            Console.WriteLine("\tMeeting has been successfully created");
            return new MeetingInfo(meetingSubject, sendGridEmails, startTime, startTime.AddMinutes(Double.Parse(duration)), 
                accessCode, password, hostInfo);
        }

        /// <summary>
        /// Sends email to all unregistered users given all the meeting attendees.
        ///
        /// </summary>
        /// <param name="attendees"></param>
        /// <param name="dbConnectionString"></param>
        public static void SendEmailsToAnyUnregisteredUsers(List<EmailAddress> attendees, 
            string dbConnectionString = 
            "Server=tcp:dbcs319discribe.database.windows.net, 1433; " +
            "Initial Catalog=db_cs319_discribe; " +
            "Persist Security Info=False;User ID=obiermann; " +
            "Password=JKm3rQ~t9sBiemann; " +
            "MultipleActiveResultSets=True; " +
            "Encrypt=True;TrustServerCertificate=False; " +
            "Connection Timeout=30")
        {
            try
            {
                DatabaseController.Initialize(dbConnectionString);

                var unregistered = DatabaseController.GetUnregisteredUsersFrom(
                    EmailHelper.FromEmailAddressListToStringList(attendees));

                EmailSender.SendEmailForVoiceRegistration(
                    EmailHelper.FromStringListToEmailAddressList(unregistered));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }



        /// <summary>
        /// Get attendees via the Webex Meetings API. Uses the access info in meetingInfo
        /// as parameters to API call.
        /// </summary>
        /// <param name="meetingInfo"></param>
        /// <returns></returns>
        public static List<EmailAddress> GetAttendeeEmails(MeetingInfo meetingInfo)
        {
            Console.WriteLine(">\tRetrieving All Attendees' Emails...");
            string strXMLServer = "https://companykm.my.webex.com/WBXService/XMLService";

            WebRequest request = WebRequest.Create(strXMLServer);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Create POST data and convert it to a byte array.
            string strXML = XMLHelper.GenerateXML(meetingInfo.AccessCode, meetingInfo.HostInfo);
                                    
            byte[] byteArray = Encoding.UTF8.GetBytes(strXML);

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();

            // Get the response.
            WebResponse response = request.GetResponse();

            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseStr = reader.ReadToEnd();

            // Display the content.         
            List<EmailAddress> emailAddresses = GetEmails(responseStr);
          
            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return emailAddresses;
        }


       


        
        [ObsoleteAttribute("This method is depricated and does not work in all cases.")]
        public static DateTime GetMeetingTimeByXML(string accessCode, WebexHostInfo meetingInfo)
        {
            Console.WriteLine(">\tRetrieving Meeting Info...");
            string strXMLServer = "https://companykm.my.webex.com/WBXService/XMLService";

            WebRequest request = WebRequest.Create(strXMLServer);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Create POST data and convert it to a byte array.
            string strXML = XMLHelper.GenerateInfoXML(accessCode, meetingInfo);

            byte[] byteArray = Encoding.UTF8.GetBytes(strXML);

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();

            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();

            // Display the content.
            string startDate = XMLHelper.RetrieveStartDate(responseFromServer);

            //Time zone format is like ABC-H:MM, Common (Specific). Just take zone code.
            string timeZone = XMLHelper.RetrieveTimeZone(responseFromServer).Split(" ")[0];
            timeZone = Regex.Split(timeZone, @"-?\d+")[0];

            Console.WriteLine("startTime: " + startDate);
            Console.WriteLine("timeZone: " + timeZone);

            DateTime meetingTime;
            DateTime.TryParse($"{startDate}", out meetingTime);

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return meetingTime;
        }

        private static List<EmailAddress> GetEmails(string myXML)
        {
            var emails = RetrieveEmails(myXML);
            var names = RetrieveNames(myXML);
            List<EmailAddress> emailAddresses = new List<EmailAddress>();

            for (int i = 0; i < emails.Count; i++)
            {
                if (BOT_EMAIL.Equals(""))
                    throw new Exception("BOT EMAIL missing");

                if (emails[i].Equals(BOT_EMAIL, StringComparison.CurrentCultureIgnoreCase))
                    continue;
                emailAddresses.Add(new EmailAddress(emails[i], names[i]));
            }

            return emailAddresses;
        }

        private static List<string> RetrieveEmails(string myXML)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(myXML);

            XmlNodeList emailNodes = xml.GetElementsByTagName("com:email");

            List<string> emails = new List<string>();

            foreach (XmlNode emailNode in emailNodes)
            {
                emails.Add(emailNode.InnerText);
            }

            return emails;
        }

        private static List<string> RetrieveNames(string myXML)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(myXML);

            XmlNodeList nameNodes = xml.GetElementsByTagName("com:name");

            List<string> names = new List<string>();

            foreach (XmlNode nameNode in nameNodes)
            {
                names.Add(nameNode.InnerText);
            }

            return names;
        }
    }
}
