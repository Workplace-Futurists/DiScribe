﻿using System;
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

namespace EmailControllers
{
    public static class MeetingController
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
            string strXML = XMLHelper.GenerateXML(accessCode);

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
            //Console.WriteLine(responseFromServer);
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
            string strXML = XMLHelper.GenerateMeetingXML(names, emails, startDate, duration);

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

            Console.WriteLine(responseFromServer);

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            Console.WriteLine("\tMeeting has been successfully created");
            return accessCode;
        }

        private static List<EmailAddress> GetEmails(string myXML)
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
                //Console.WriteLine(nameNode.InnerText);
                names.Add(nameNode.InnerText);
            }

            return names;
        }

        public static List<string> FromEmailAddressListToStringList(List<EmailAddress> emails)
        {
            List<string> emailsAsString = new List<String>();
            foreach (EmailAddress email in emails)
            {
                emailsAsString.Add(email.Email);
            }
            return emailsAsString;
        }
    }
}
