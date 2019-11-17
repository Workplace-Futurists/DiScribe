using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

namespace twilio_caller.Graph
{
    public class GraphHelper
    {
        private static GraphServiceClient _graphClient;

        private static Subscription _mailSubscription;
        private const int MAX_SUB_EXPIRATION_MINS = 4230;

        public static void Initialize(IAuthenticationProvider authProvider)
        {
            _graphClient = new GraphServiceClient(authProvider);
        }

        public static async Task<User> GetMeAsync()
        {
            try
            {
                // GET /me
                return await _graphClient.Me.Request().GetAsync();
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting signed-in user: {ex.Message}");
                return null;
            }
        }
        public static async Task<IEnumerable<Event>> GetEventsAsync()
        {
            try
            {
                // GET /me/events
                var resultPage = await _graphClient.Me.Events.Request()
                    // Only return the fields used by the application
                    .Select("subject,organizer,start,end")
                    // Sort results by when they were created, newest first
                    .OrderBy("createdDateTime DESC")
                    .GetAsync();

                return resultPage.CurrentPage;
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting events: {ex.Message}");
                return null;
            }
        }

        public static async Task<Message> GetEmailAsync()
        {
            var messages = await _graphClient.Me.Messages
                .Request()
                .Select(e => new {
                    e.Body
                })
                .GetAsync();
            return messages[0];
        }

        public static async Task<string> GetEmailMeetingNumAsync()
        {
            try
            {
                // Get message from mailbox
                Message message = await GetEmailAsync();

                string accessCode;
                //string meetingStart;
                //Boolean pm = false;
                string parsedEmail = message.Body.Content;
                parsedEmail = WebUtility.HtmlDecode(parsedEmail);
                HtmlDocument htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(parsedEmail);
                //htmldoc.DocumentNode.SelectNodes("//comment()")?.Foreach(c => c.Remove());
                parsedEmail = htmldoc.DocumentNode.InnerText;
                accessCode = parsedEmail.Substring(parsedEmail.IndexOf("Meeting number (access code):"), 41);
                accessCode = accessCode.Substring(accessCode.IndexOf(':') + 2, 11);
                accessCode = accessCode.Replace(" ", "");

                return accessCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in GetEmailMeetingNumAsync: " + ex.Message);
                return "000000000";
            }
        }

        public static async Task<Subscription> AddMailSubscription()
        {
            try
            {
                _mailSubscription = new Subscription
                {
                    ChangeType = "created,updated",
                    //NotificationUrl = "https://discribefunctionapp.azurewebsites.net/api/OutlookMessageWebhookCreator1?code=oCrAsapgfgt68ChnQMGBmkTsYOdRuEGT2KB3yogU0ML4rLgdgIWMkQ==",
                    NotificationUrl = "https://discribefunctionapp.azurewebsites.net/api/subCreatorTest?code=h74tSOzgvTGtYZQ6pql0gEPxR1gnmDjL2bD67/hdqzho86y3vMa3Ww==",
                    //NotificationUrl = "http://localhost:7071/api/CreateSubscription",

                    Resource = "me/mailFolders('Inbox')/messages",
                    // This is the max expiration datetime for a mail subscription
                    ExpirationDateTime = DateTime.Now.AddMinutes(MAX_SUB_EXPIRATION_MINS),
                    ClientState = "secretClientValue"
                };

                var response = await _graphClient.Subscriptions
                    .Request()
                    .AddAsync(_mailSubscription);

                Console.WriteLine($"Returned response from add subscription: {response}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding subscription: {_mailSubscription}");
                Console.WriteLine($"Received error: {ex.Message}");
                return null;
            }
        }

        // public static async Task sendMail() {
        //     var message = new Message {
        //         Subject = "Meeting minutes",
        //         Body = new ItemBody
        //         {
        //             ContentType = BodyType.Text,
        //             Content = "This is the meeting minute for WebEx Meeting"
        //         },
        //         ToRecipients = new List<Recipient>()
        //         {
        //             new Recipient
        //             {
        //                 EmailAddress = new EmailAddress
        //                 {
        //                     Address = "seungwook.l95@gmail.com"
        //                 }
        //             }
        //         },
        //         CcRecipients = new List<Recipient>()
        //         {
        //             new Recipient
        //             {
        //                 EmailAddress = new EmailAddress
        //                 {
        //                     Address = "workplace-futurists@hotmail.com"
        //                 }
        //             }
        //         }
        //     };

        //     var saveToSentItems = true;

        //     await _graphClient.Me
        //         .SendMail(message,saveToSentItems)
        //         .Request()
        //         .PostAsync();
        // }
    }
}