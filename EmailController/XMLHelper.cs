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

namespace EmailController
{
    public class XMLHelper
    {
        public static List<EmailAddress> GetMeetingAttendee(string accessCode)
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

            List<string> emails = RetrieveEmails(responseFromServer);
            List<string> names = RetrieveNames(responseFromServer);

            List<EmailAddress> emailAddresses = GetEmails(emails, names);
            foreach (EmailAddress email in emailAddresses)
            {
                Console.WriteLine(email);
            }
        
            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return emailAddresses;
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

        public static List<string> RetrieveEmails(string myXML)
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

        public static List<string> RetrieveNames(string myXML)
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

        public static List<EmailAddress> GetEmails(List<string> emails, List<string> names)
        {
            List<EmailAddress> emailAddresses = new List<EmailAddress>();

            for (int i = 0; i < emails.Count; i++)
            {
                emailAddresses.Add(new EmailAddress(emails[i], names[i]));
            }

            return emailAddresses;
        }


    }
}
