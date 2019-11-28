using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Timers;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using Microsoft.Identity.Client;
using Microsoft.Graph.Auth;

namespace DiScribe.MeetingManager
{
    public class GraphHelper
    {
        private static GraphServiceClient _graphClient;

        // Subscription variables
        private const int MAX_SUB_EXPIRATION_MINS = 4230;
        private static Dictionary<string, Subscription> _Subscriptions = new Dictionary<string, Subscription>();
        private static System.Timers.Timer _subscriptionTimer = null;
        private static string _userId;

        public static async Task Initialize(string appId, string tenantID, string clientSecret, string principal)
        {
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithTenantId(tenantID)
                .WithClientSecret(clientSecret)
                .Build();

            ClientCredentialProvider authenticationProvider = new ClientCredentialProvider(confidentialClientApplication);

            
            _graphClient = new GraphServiceClient(authenticationProvider);

            var user = await GetMeAsync(principal);
            
            _userId = user.Id;


        }


        /// <summary>
        /// Get the User object representing this Graph user by principal. The principal
        /// is the email address for the account.
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static async Task<Microsoft.Graph.User> GetMeAsync(string principal)
        {
            IGraphServiceUsersCollectionPage users;

                var graphUsers = await _graphClient
                .Users
                .Request()
                .Filter($"startswith(Mail,'{principal}')")
                .GetAsync();

                return graphUsers[0];
                   

        }

        

        public static async Task<IEnumerable<Event>> GetEventsAsync()
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

        

        public static async Task<Message> GetEmailAsync()
        {

            /*Get all messages for this user in inbox */
            var users = await _graphClient
            .Users
            .Request()
            .GetAsync();

            /*Get all messages for this user in inbox */
            var inbox = await _graphClient
            .Users[_userId]
            .MailFolders
            .Inbox
            .Request()
            .GetAsync();

            

            // Get messages from the inbox mail folder
            var messages = await _graphClient
                .Users[_userId] 
                .MailFolders[inbox.Id] 
                .Messages
                .Request()
                .OrderBy("ReceivedDateTime DESC")
                .Top(1)
                .GetAsync();

            if (messages.Count > 0)
                return messages[0];

            return null;

        }

      

        public static async Task<Boolean> DeleteEmailAsync(Message message)
        {
            /*Get all messages for this user in inbox */
            var users = await _graphClient
            .Users
            .Request()
            .GetAsync();

            /*Get all messages for this user in inbox */
            var inbox = await _graphClient
            .Users[_userId]
            .MailFolders
            .Inbox
            .Request()
            .GetAsync();


            // Get the latest message from the inbox mail folder
            var messages = await _graphClient
                .Users[_userId]
                .MailFolders[inbox.Id]
                .Messages
                .Request()
                .OrderBy("ReceivedDateTime DESC")
                .Top(1)
                .GetAsync();
            

            string messageId;                      
            if (messages[0] != null)
            {
                messageId = messages[0].Id;
            }

            else
                return false;


            await _graphClient.Users[_userId].Messages[messageId]
            .Request()
            .DeleteAsync();

            return true;

        }


        public static async Task<string> GetEmailMeetingNumAsync(Message message)
        {
            
            if (message.Body.Content.Contains("Meeting number (access code):"))
            {
                string accessCode;
                string startDate;
                

                string parsedEmail = message.Body.Content;
                parsedEmail = WebUtility.HtmlDecode(parsedEmail);
                HtmlDocument htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(parsedEmail);
                //htmldoc.DocumentNode.SelectNodes("//comment()")?.Foreach(c => c.Remove());
                parsedEmail = htmldoc.DocumentNode.InnerText;
                accessCode = parsedEmail.Substring(parsedEmail.IndexOf("Meeting number (access code):"), 41);
                accessCode = accessCode.Substring(accessCode.IndexOf(':') + 2, 11);
                accessCode = accessCode.Replace(" ", "");
                startDate = parsedEmail.Substring(parsedEmail.IndexOf("Meeting password: ") + 28);
                parsedEmail = parsedEmail.Substring(parsedEmail.IndexOf("  |  ", parsedEmail.IndexOf("  |  ") + 1));
                parsedEmail = parsedEmail.Substring(parsedEmail.IndexOf("  |  ") + 4);
                parsedEmail = parsedEmail.Substring(0, 8);
                parsedEmail = parsedEmail.Replace(" ", "");

               

                return accessCode;
               
            }
            return "";
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
                    _subscriptionTimer = new System.Timers.Timer(43200000);
                    _subscriptionTimer.Elapsed += CheckSubscriptions;
                    _subscriptionTimer.AutoReset = true;
                    _subscriptionTimer.Enabled = true;
                }


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
    }
}
