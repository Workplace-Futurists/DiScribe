using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using EmailAddress = SendGrid.Helpers.Mail.EmailAddress;
using File = System.IO.File;

namespace EmailController
{
    public static class XMLHelper
    {
        public static List<EmailAddress> GetAttendeeEmails(string accessCode)
        {
            string strXMLServer = "https://companykm.my.webex.com/WBXService/XMLService";

            WebRequest request = WebRequest.Create(strXMLServer);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Create POST data and convert it to a byte array.
            string strXML = GenerateXML(accessCode);

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
            Console.WriteLine(responseFromServer);
            List<EmailAddress> emailAddresses = GetEmails(responseFromServer);

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return emailAddresses;
        }


        public static string CreateWebExMeeting(List<string> names, List<string> emails, string startDate, string duration)
        {
            string strXMLServer = "https://companykm.my.webex.com/WBXService/XMLService";

            WebRequest request = WebRequest.Create(strXMLServer);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Create POST data and convert it to a byte array.
            // string strXML = GenerateXMLCreateMeeting();

            // String strXML = File.ReadAllText(@"createMeeting.xml");
            string strXML = GenerateMeetingXML(names, emails, startDate, duration);

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
            string accessCode = RetrieveAccessCode(responseFromServer);

            Console.WriteLine(responseFromServer);

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            Console.WriteLine("\tMeeting has been successfully created");
            return accessCode;
        }

        public static string GenerateMeetingXML(List<string> names, List<string> emails, string startDate, string duration)
        {
            string strXML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n";
            strXML += "<serv:message xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n";
            strXML += "<header>\r\n";
            strXML += "<securityContext>\r\n";
            strXML += "<webExID>kengqiangmk</webExID>\r\n";
            strXML += "<password>Cs319_A</password>\r\n";
            strXML += "<siteName>companykm.my</siteName>\r\n";
            strXML += "<email>kengqiangmk@gmail.com</email>\r\n";
            strXML += "</securityContext>\r\n";
            strXML += "</header>\r\n";
            strXML += "<body>\r\n";
            strXML += "<bodyContent xsi:type=\"java:com.webex.service.binding.meeting.CreateMeeting\">\r\n";
            strXML += "<accessControl>\r\n";
            strXML += "<meetingPassword>pZGiw4JU</meetingPassword>\r\n";
            strXML += "</accessControl>\r\n";
            strXML += "<metaData>\r\n";
            strXML += "<confName>Sample Meeting</confName>\r\n";
            strXML += "<agenda>Test</agenda>\r\n";
            strXML += "</metaData>\r\n";
            strXML += "<participants>\r\n";
            strXML += "<maxUserNumber>10</maxUserNumber>\r\n";
            strXML += "<attendees>\r\n";
            strXML += GenerateAttendeeElement(names, emails);
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
            strXML += startDate;
            strXML += "</startDate>\r\n";
            strXML += "<openTime>900</openTime>\r\n";
            strXML += "<joinTeleconfBeforeHost>true</joinTeleconfBeforeHost>\r\n";
            strXML += "<duration>";
            strXML += duration;
            strXML += "</duration>\r\n";
            strXML += "<timeZoneID>4</timeZoneID>\r\n";
            strXML += "</schedule>\r\n";
            strXML += "<telephony>\r\n";
            strXML += "<telephonySupport>CALLIN</telephonySupport>\r\n";
            strXML += "<extTelephonyDescription>";
            strXML += "Call 1-800-555-1234, Passcode 98765";
            strXML += "</extTelephonyDescription>\r\n";
            strXML += "</telephony>\r\n";
            strXML += "</bodyContent>\r\n";
            strXML += "</body>\r\n";
            strXML += "</serv:message>\r\n";

            return strXML;
        }

        public static string GenerateAttendeeElement(List<string> names, List<string> emails)
        {
            string xml = "";
            for (int i = 0; i < emails.Count; i++)
            {
                xml += GenerateAttendeeSingleElement(names[i], emails[i]);
            }
            return xml;
        }

        public static string GenerateAttendeeSingleElement(string name, string email)
        {
            string strXML = "";
            strXML += "<attendee>\r\n";
            strXML += "<person>\r\n";
            strXML += "<name>";
            strXML += name;
            strXML += "</name>\r\n";
            strXML += "<email>";
            strXML += email;
            strXML += "</email>\r\n";
            strXML += "</person>\r\n";
            strXML += "</attendee>\r\n";

            return strXML;
        }
        

        public static string GenerateXML(string accessCode)
        {
            string strXML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n";
            strXML += "<serv:message xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n";
            strXML += "<header>\r\n";
            strXML += "<securityContext>\r\n";
            strXML += "<webExID>kengqiangmk</webExID>\r\n";
            strXML += "<password>Cs319_A</password>\r\n";
            strXML += "<siteName>companykm.my</siteName>\r\n";
            strXML += "<email>kengqiangmk@gmail.com</email>\r\n";
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

        public static List<EmailAddress> GetEmails(string myXML)
        {
            var emails = RetrieveEmails(myXML);
            var names = RetrieveNames(myXML);
            List<EmailAddress> emailAddresses = new List<EmailAddress>();

            for (int i = 0; i < emails.Count; i++)
            {
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
                Console.WriteLine(emailNode.InnerText);
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
                Console.WriteLine(nameNode.InnerText);
                names.Add(nameNode.InnerText);
            }

            return names;
        }

        private static string RetrieveAccessCode(string myXML)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(myXML);

            XmlNodeList meetingKey = xml.GetElementsByTagName("meet:meetingkey");
            string accessCode = meetingKey[0].InnerText;

            return accessCode;
        }
    }
}
