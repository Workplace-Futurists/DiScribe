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
using twilio_caller.dialer;
using System.IO;
using Microsoft.Graph;
using HtmlAgilityPack;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;
using twilio_caller.SendEmailCsharp;

namespace twilio_caller
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
            var events = Graph.GraphHelper.GetEventsAsync().Result;
            Console.WriteLine("Events:");

            foreach (var calendarEvent in events)
            {
                Console.WriteLine($"Subject: {calendarEvent.Subject}");
                Console.WriteLine($"  Organizer: {calendarEvent.Organizer.EmailAddress.Name}");
                Console.WriteLine($"  Start: {FormatDateTimeTimeZone(calendarEvent.Start)}");
                Console.WriteLine($"  End: {FormatDateTimeTimeZone(calendarEvent.End)}");
            }
        }
        static string parseEmail(string email)
        {
            return "";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("DiScribe Dialer\n");

            // loads appsettings file
            var appConfig = LoadAppSettings();

            // Throws warning if no appsettings.json exists
            if (appConfig == null)
            {
                Console.WriteLine("Missing or invalid appsettings.json...exiting");
                return;
            }

            // assign appsetting values to variables
            var appId = appConfig["appId"];
            // outlook 
            string mailUser = appConfig["mailUser"];
            // add password from privatesettings.json into secure string
            SecureString mailPass = new SecureString();
            foreach (char c in appConfig["mailPass"])
            {
                mailPass.AppendChar(c);
            }
            // add azure ad app scopes and tenant id
            var scopes = appConfig.GetSection("scopes").Get<string[]>();
            var tenantId = appConfig["tenantId"];
            // add twilio authentication values
            string twilioSid = appConfig["TWILIO_ACCOUNT_SID"];
            string twilioAuthToken = appConfig["TWILIO_AUTH_TOKEN"];

            // Initialize the auth provider with values from appsettings.json
            var authProvider = new GraphAuthentication.UserPassAuthProvider(appId, mailUser, mailPass, scopes,tenantId);
           
            // Request a token to sign in the user
            var accessToken = authProvider.GetAccessToken().Result;

            // Initialize Graph client
            Graph.GraphHelper.Initialize(authProvider);

            // Get signed in user
            var user = Graph.GraphHelper.GetMeAsync().Result;
            Console.WriteLine($"Welcome {user.DisplayName}!\n");
     
            //// Get meeting number in email inbox
            //try
            //{
            //    string meetingNum = Graph.GraphHelper.GetEmailMeetingNumAsync().Result;
            //    Console.WriteLine($"The meeting number retrieved was {meetingNum};");
            //}
            //catch (Exception ex)  //Exceptions here or in the function will be caught here
            //{
            //    Console.WriteLine("Exception: " + ex.Message);

            //}
            // Try making subscription
            try
            {
                Subscription subscription = Graph.GraphHelper.AddMailSubscription().Result;
                Console.WriteLine($"The subscriptioncreated, change type {subscription.ChangeType};");
                Console.WriteLine($"The subscription is subscribed to {subscription.Resource};");
                Console.WriteLine($"The subscription retrieved will expire at {subscription.ExpirationDateTime};");
            }
            catch (Exception ex)  //Exceptions here or in the function will be caught here
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            string sendGridAPI = appConfig["SENDGRID_API_KEY"];

            // Event Handler for Meeting Minute creation
            // Once the meeting minute is created in the desired folder,
            // emails are sent to participants
            SendEmailCsharp.Watcher.Run(sendGridAPI);

            // Send email using SendGrid
            // GOT TO FIND A WAY TO RETRIEVE MEETING INFORMATION
            // string sendGridAPI = appConfig["SENDGRID_API_KEY"];
            // SendEmailCsharp.SendEmailCsharp.Initialize(sendGridAPI);
            // SendEmailCsharp.SendEmailCsharp.sendEmail().Wait();
            

            // Graph.GraphHelper.sendMail();

            //int choice = -1;

            //while (choice != 0)
            //{
            //    Console.WriteLine("Please choose one of the following options:");
            //    Console.WriteLine("0. Exit");
            //    Console.WriteLine("1. Display access token");
            //    Console.WriteLine("2. List calendar events");

            //    try
            //    {
            //        choice = int.Parse(Console.ReadLine());
            //    }
            //    catch (System.FormatException)
            //    {
            //        // Set to invalid value
            //        choice = -1;
            //    }

            //    switch (choice)
            //    {
            //        case 0:
            //            // Exit the program
            //            Console.WriteLine("Goodbye...");
            //            break;
            //        case 1:
            //            // Display access token
            //            Console.WriteLine($"Access token: {accessToken}\n");
            //            break;
            //        case 2:
            //            // List the calendar
            //            ListCalendarEvents();
            //            break;
            //        default:
            //            Console.WriteLine("Invalid choice! Please try again.");
            //            break;
            //    }
            //}

        }
        }
}
