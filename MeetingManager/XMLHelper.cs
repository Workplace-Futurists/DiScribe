using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

namespace DiScribe.Meeting
{
    static class XMLHelper
    {
        public static string GenerateMeetingXML(string meetingSubject, List<string> names, List<string> emails, string startTime, string duration, 
            WebexHostInfo hostInfo, Microsoft.Graph.EmailAddress hostDelegate = default)
        {
            string strXML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n";
            strXML += "<serv:message xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n";
            strXML += "<header>\r\n";
            strXML += "<securityContext>\r\n";
            strXML += $"<webExID>{hostInfo.ID}</webExID>\r\n";
            strXML += $"<password>{hostInfo.Password}</password>\r\n";
            strXML += $"<siteName>{hostInfo.Company}</siteName>\r\n";
            strXML += $"<email>{hostInfo.Email}</email>\r\n";
            strXML += "</securityContext>\r\n";
            strXML += "</header>\r\n";
            strXML += "<body>\r\n";
            strXML += "<bodyContent xsi:type=\"java:com.webex.service.binding.meeting.CreateMeeting\">\r\n";
            strXML += "<accessControl>\r\n";
            strXML += "<meetingPassword>pZGiw4JU</meetingPassword>\r\n";
            strXML += "</accessControl>\r\n";
            strXML += "<metaData>\r\n";
            strXML += "<confName>" + meetingSubject + "</confName>\r\n";
            strXML += "<agenda>Test</agenda>\r\n";
            strXML += "</metaData>\r\n";
            strXML += "<participants>\r\n";
            strXML += "<maxUserNumber>10</maxUserNumber>\r\n";
            strXML += "<attendees>\r\n";
            strXML += GenerateAttendeeElement(names, emails, hostDelegate.Address);
            strXML += "</attendees>\r\n";
            strXML += "</participants>\r\n";
            strXML += "<enableOptions>\r\n";
            strXML += "<chat>true</chat>\r\n";
            strXML += "<poll>true</poll>\r\n";
            strXML += "<audioVideo>true</audioVideo>\r\n";
            strXML += "<supportE2E>TRUE</supportE2E>\r\n";
            strXML += "<autoRecord>TRUE</autoRecord>\r\n";
            strXML += "</enableOptions>\r\n";
            strXML += "<schedule>\r\n";
            strXML += "<startDate>";
            strXML += startTime;
            strXML += "</startDate>\r\n";
            strXML += "<openTime>900</openTime>\r\n";
            strXML += "<joinTeleconfBeforeHost>true</joinTeleconfBeforeHost>\r\n";
            strXML += "<duration>";
            strXML += duration;
            strXML += "</duration>\r\n";
            strXML += "</schedule>\r\n";
            strXML += "<telephony>\r\n";
            strXML += "<telephonySupport>CALLIN</telephonySupport>\r\n";
            strXML += "<extTelephonyDescription>";
            strXML += "Call 1-800-555-1234, Passcode 98765";
            strXML += "</extTelephonyDescription>\r\n";
            strXML += "</telephony>\r\n";
            strXML += "<attendeeOptions>";
            strXML += "<emailInvitations>TRUE</emailInvitations>\r\n";
            strXML += "</attendeeOptions>\r\n";
            strXML += "</bodyContent>\r\n";
            strXML += "</body>\r\n";
            strXML += "</serv:message>\r\n";

            return strXML;
        }

        public static string GenerateAttendeeElement(List<string> names, List<string> emails, string hostDelegateEmail)
        {
            string xml = "";
            for (int i = 0; i < emails.Count; i++)
            {
                xml += GenerateAttendeeSingleElement(names[i], emails[i], hostDelegateEmail);
            }

            return xml;
        }

        public static string GenerateAttendeeSingleElement(string name, string email, string hostDelegateEmail)
        {
            string strXML = "";
            strXML += "<attendee>\r\n";
            strXML += "<person>\r\n";
            strXML += "<name>";
            strXML += name;
            strXML += "</name>\r\n";
            strXML += "<email>";
            strXML += email;
            if (email == hostDelegateEmail)
            {
                strXML += "<role>";
                strXML += "HOST";
                strXML += "</role>\r\n";
            }

            strXML += "</email>\r\n";
            strXML += "</person>\r\n";
            strXML += "</attendee>\r\n";

            return strXML;
        }

        public static string GenerateXML(string accessCode, WebexHostInfo hostInfo)
        {
            string strXML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n";
            strXML += "<serv:message xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n";
            strXML += "<header>\r\n";
            strXML += "<securityContext>\r\n";
            strXML += $"<webExID>{hostInfo.ID}</webExID>\r\n";
            strXML += $"<password>{hostInfo.Password}</password>\r\n";
            strXML += $"<siteName>{hostInfo.Company}</siteName>\r\n";
            strXML += $"<email>{hostInfo.Email}</email>\r\n";
            strXML += "</securityContext>\r\n";
            strXML += "</header>\r\n";
            strXML += "<body>\r\n";
            strXML += "<bodyContent xsi:type=\"java:com.webex.service.binding.attendee.LstMeetingAttendee\">\r\n";
            strXML += "<meetingKey>";
            strXML += accessCode;
            strXML += "</meetingKey>\r\n";
            strXML += "</bodyContent>\r\n";
            strXML += "</body>\r\n";
            strXML += "</serv:message>\r\n";

            return strXML;
        }

        public static string GenerateInfoXML(string accessCode, WebexHostInfo hostInfo)
        {
            string strXML = "<?xml version=\"1.0\" encoding=\"ISO - 8859 - 1\"?>\r\n";
            strXML += "<serv:message xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:serv=\"http://www.webex.com/schemas/2002/06/service\">\r\n";
            strXML += "<header>\r\n";
            strXML += "<securityContext>\r\n";
            strXML += $"<webExID>{hostInfo.ID}</webExID>\r\n";
            strXML += $"<password>{hostInfo.Password}</password>\r\n";
            strXML += $"<siteName>{hostInfo.Company}</siteName>\r\n";
            strXML += $"<email>{hostInfo.Email}</email>\r\n";
            strXML += "</securityContext>\r\n";
            strXML += "</header>\r\n";
            strXML += "<body>\r\n";
            strXML += "<bodyContent xsi:type=\"java:com.webex.service.binding.meeting.GetMeeting\">\r\n";
            strXML += "<meetingKey>";
            strXML += accessCode;
            strXML += "</meetingKey>\r\n";
            strXML += "</bodyContent>\r\n";
            strXML += "</body>\r\n";
            strXML += "</serv:message>\r\n";


            return strXML;
        }

        public static string GenerateStartURLXML(string accessCode)
        {
            string strXML = "<?xml version=\"1.0\" encoding=\"ISO - 8859 - 1\"?>\r\n";
            strXML += "<serv:message xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:serv=\"http://www.webex.com/schemas/2002/06/service\">\r\n";
            strXML += "<header>\r\n";
            strXML += "<securityContext>\r\n";
            strXML += "<webExID>kengqiangmk</webExID>\r\n";
            strXML += "<password>Cs319_APP</password>\r\n";
            strXML += "<siteName>companykm.my</siteName>\r\n";
            strXML += "<email>kengqiangmk@gmail.com</email>\r\n";
            strXML += "</securityContext>\r\n";
            strXML += "</header>\r\n";
            strXML += "<body>\r\n";
            strXML += "<bodyContent xsi:type=\"java:com.webex.service.binding.meeting.GethosturlMeeting\">\r\n";
            strXML += "<meetingKey>";
            strXML += accessCode;
            strXML += "</meetingKey>\r\n";
            strXML += "</bodyContent>\r\n";
            strXML += "</body>\r\n";
            strXML += "</serv:message>\r\n";


            return strXML;
        }

        public static string RetrieveStartUrl(string accessCode)
        {
            string myXML = GenerateStartURLXML(accessCode);
            string strXMLServer = "https://companykm.my.webex.com/WBXService/XMLService";
            WebRequest request = WebRequest.Create(strXMLServer);
            request.Method = "POST";
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            byte[] byteArray = Encoding.UTF8.GetBytes(myXML);
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
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(responseFromServer);
            XmlNodeList stratURLNode = xml.GetElementsByTagName("meet:hostMeetingURL");
            string startURL = stratURLNode[0].InnerText;

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return startURL;
        }

        public static string RetrieveStartDate(string myXML)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(myXML);

            XmlNodeList stratDateNode = xml.GetElementsByTagName("meet:startDate");
            string startDate = stratDateNode[0].InnerText;

            return startDate;
        }

        public static string RetrieveTimeZone(string myXML)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(myXML);

            XmlNodeList timeZoneNode = xml.GetElementsByTagName("meet:timeZone");
            string timeZone = timeZoneNode[0].InnerText;

            return timeZone;
        }

        public static string RetrieveAccessCode(string myXML)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(myXML);

            XmlNodeList meetingKey = xml.GetElementsByTagName("meet:meetingkey");
            string accessCode = meetingKey[0].InnerText;

            return accessCode;
        }

               
       

    
    }
}
