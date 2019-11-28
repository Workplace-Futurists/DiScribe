using System;
using System.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using DiScribe.Dialer;
using System.IO;
using Microsoft.Graph;
using HtmlAgilityPack;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;
// using twilio_caller.SendEmailCsharp;

namespace DiScribe.Dialer
{
    class Program
    {
        static IConfigurationRoot LoadAppSettings()
        {
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            // Check for required settings
            if (string.IsNullOrEmpty(appConfig["appId"]) ||
            string.IsNullOrEmpty(appConfig["mailUser"]) ||
            string.IsNullOrEmpty(appConfig["mailPass"]) ||
            // Make sure there's at least one value in the scopes array
            string.IsNullOrEmpty(appConfig["scopes:0"]) ||
            string.IsNullOrEmpty(appConfig["TWILIO_ACCOUNT_SID"]) ||
            string.IsNullOrEmpty(appConfig["TWILIO_AUTH_TOKEN"]) ||
            string.IsNullOrEmpty(appConfig["SENDGRID_API_KEY"]))
            {
                return null;
            }

            return appConfig;
        }

        static string FormatDateTimeTimeZone(DateTimeTimeZone value)
        {
            // Get the timezone specified in the Graph value
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZone);
            // Parse the date/time string from Graph into a DateTime
            var dateTime = DateTime.Parse(value.DateTime);

            // Create a DateTimeOffset in the specific timezone indicated by Graph
            var dateTimeWithTZ = new DateTimeOffset(dateTime, timeZone.BaseUtcOffset)
                .ToLocalTime();

            return dateTimeWithTZ.ToString("g");
        }

        static void ListCalendarEvents()
        {
            //var events = Graph.GraphHelper.GetEventsAsync().Result;
            //Console.WriteLine("Events:");

            //foreach (var calendarEvent in events)
            //{
            //    Console.WriteLine($"Subject: {calendarEvent.Subject}");
            //    Console.WriteLine($"  Organizer: {calendarEvent.Organizer.EmailAddress.Name}");
            //    Console.WriteLine($"  Start: {FormatDateTimeTimeZone(calendarEvent.Start)}");
            //    Console.WriteLine($"  End: {FormatDateTimeTimeZone(calendarEvent.End)}");
            //}
        }

        static string parseEmail(string email)
        {
            return "";
        }

       
    }
}
