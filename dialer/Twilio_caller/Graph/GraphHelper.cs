using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Timers;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

namespace twilio_caller.Graph
{
    public class GraphHelper
    {
        private static GraphServiceClient _graphClient;

        // Subscription variables
        private const int MAX_SUB_EXPIRATION_MINS = 4230;
        private static Dictionary<string, Subscription> _Subscriptions = new Dictionary<string, Subscription>();
        private static Timer _subscriptionTimer = null;

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

        [HttpGet]
        public static async Task<Subscription> AddMailSubscription()
        {
            try
            {
                Subscription mailSubscription = new Subscription
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

                // make http request to graph api
                var response = await _graphClient.Subscriptions
                    .Request()
                    .AddAsync(mailSubscription);

                // add this subscription to class variable
                _Subscriptions[response.Id] = response;

                // set timer to renew subscription
                if (_subscriptionTimer == null)
                {
                    // calls to check subscriptions every 12 hours = 43200000 milliseconds
                    _subscriptionTimer = new Timer(43200000);
                    _subscriptionTimer.Elapsed += CheckSubscriptions;
                    _subscriptionTimer.AutoReset = true;
                    _subscriptionTimer.Enabled = true;
                }

                Console.WriteLine($"Returned response from add subscription: {response}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding mail subscription");
                Console.WriteLine($"Received error: {ex.Message}");
                return null;
            }
        }

        private static void CheckSubscriptions(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine($"Checking subscriptions {DateTime.Now.ToString("h:mm:ss.fff")}");
            Console.WriteLine($"Current subscription count {_Subscriptions.Count}");

            foreach (var subscription in _Subscriptions)
            {
                // if the subscription expires in the next day, renew it
                if (subscription.Value.ExpirationDateTime < DateTime.UtcNow.AddDays(1))
                {
                    RenewSubscription(subscription.Value);
                }
            }
        }

        private static void RenewSubscription(Subscription subscription)
        {
            Console.WriteLine($"Current subscription: {subscription.Id}, Expiration: {subscription.ExpirationDateTime}");

            subscription.ExpirationDateTime = DateTime.UtcNow.AddMinutes(MAX_SUB_EXPIRATION_MINS);

            var foo = _graphClient
              .Subscriptions[subscription.Id]
              .Request()
              .UpdateAsync(subscription).Result;

            Console.WriteLine($"Renewed subscription: {subscription.Id}, New Expiration: {subscription.ExpirationDateTime}");
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