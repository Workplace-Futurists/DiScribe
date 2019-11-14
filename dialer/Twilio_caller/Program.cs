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
            string.IsNullOrEmpty(appConfig["username"]) ||
            string.IsNullOrEmpty(appConfig["password"]) ||
                // Make sure there's at least one value in the scopes array
                string.IsNullOrEmpty(appConfig["scopes:0"]))
            {
                return null;
            }

            return appConfig;
        }

        static string FormatDateTimeTimeZone(Microsoft.Graph.DateTimeTimeZone value)
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

        static async Task<Message> getEmailAsync(GraphServiceClient graph)
        {
            var messages = await graph.Me.Messages
                .Request()
                .Select(e => new {
                    e.Body
                    })
                .GetAsync();
            return messages[0];
        }

        static void Main(string[] args)
        {
            Console.WriteLine(".NET Core Graph Tutorial\n");

            var appConfig = LoadAppSettings();

            if (appConfig == null)
            {
                Console.WriteLine("Missing or invalid PrivateSettings.json...exiting");
                return;
            }

            var appId = appConfig["appId"];
            string username = appConfig["username"];
            SecureString password = new SecureString();
            // add password from privatesettings.json into secure string
            foreach (char c in appConfig["password"])
            {
                 password.AppendChar(c);
            }
            

            var scopes = appConfig.GetSection("scopes").Get<string[]>();
            var tenantId = appConfig.GetSection("tenantId").Get<string>();

            // Initialize the auth provider with values from appsettings.json
            var authProvider = new GraphAuthentication.UserPassAuthProvider(appId, username, password, scopes,tenantId);

           
            // Request a token to sign in the user
            var accessToken = authProvider.GetAccessToken().Result;

            // Initialize Graph client
            Graph.GraphHelper.Initialize(authProvider);

            // Get signed in user
            var user = Graph.GraphHelper.GetMeAsync().Result;
            Console.WriteLine($"Welcome {user.DisplayName}!\n");

            int choice = -1;

            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            var subscription = new Subscription
            {
                ChangeType = "created,updated",
                NotificationUrl = "https://discribefunctionapp.azurewebsites.net/api/OutlookMessageWebhookCreator1?code=oCrAsapgfgt68ChnQMGBmkTsYOdRuEGT2KB3yogU0ML4rLgdgIWMkQ==",
                Resource = "me/mailFolders('Inbox')/messages",
                ExpirationDateTime = DateTimeOffset.Parse("2016-11-20T18:23:45.9356913Z"),
                ClientState = "secretClientValue"
            };

     
                try
                {
                    Task.Run(() => graphClient.Subscriptions
                    .Request()
                    .AddAsync(subscription));

                    // Start a task - calling an async function in this example
                    Task<Message> callTask = Task.Run(() => getEmailAsync(graphClient));
                    // Wait for it to finish
                    callTask.Wait();
                    // Get the result
                    Message message = callTask.Result;
                    // Write it our

                    string accessCode;
                    string meetingStart;
                    Boolean pm = false;
                    string parsedEmail = message.Body.Content;
                    parsedEmail = WebUtility.HtmlDecode(parsedEmail);
                    HtmlDocument htmldoc = new HtmlDocument();
                    htmldoc.LoadHtml(parsedEmail);
                    //htmldoc.DocumentNode.SelectNodes("//comment()")?.Foreach(c => c.Remove());
                    parsedEmail = htmldoc.DocumentNode.InnerText;
                    accessCode = parsedEmail.Substring(parsedEmail.IndexOf("Meeting number (access code):"), 41);
                    accessCode = accessCode.Substring(accessCode.IndexOf(':') + 2, 11);
                    accessCode = accessCode.Replace(" ", "");
                   
                }
                catch (Exception ex)  //Exceptions here or in the function will be caught here
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
                
            

           

            while (choice != 0)
            {
                Console.WriteLine("Please choose one of the following options:");
                Console.WriteLine("0. Exit");
                Console.WriteLine("1. Display access token");
                Console.WriteLine("2. List calendar events");
                
                try
                {
                    choice = int.Parse(Console.ReadLine());
                }
                catch (System.FormatException)
                {
                    // Set to invalid value
                    choice = -1;
                }

                switch (choice)
                {
                    case 0:
                        // Exit the program
                        Console.WriteLine("Goodbye...");
                        break;
                    case 1:
                        // Display access token
                        Console.WriteLine($"Access token: {accessToken}\n");
                        break;
                    case 2:
                        // List the calendar
                        ListCalendarEvents();
                        break;
                    default:
                        Console.WriteLine("Invalid choice! Please try again.");
                        break;
                }
            }
        }
        
    }
}
