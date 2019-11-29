using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.Graph.Auth;
using System.Globalization;


namespace DiScribe.Email
{
    public static class EmailListener
    {
        private static GraphServiceClient _graphClient;

        // Subscription variables
        private const int MAX_SUB_EXPIRATION_MINS = 4230;
        private static Dictionary<string, Subscription> _Subscriptions = new Dictionary<string, Subscription>();
        private static System.Timers.Timer _subscriptionTimer;
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

            if (messages is null)
                throw new Exception("Messages Retrieved were <NULL>");

            if (messages.Count > 0)
                return messages[0];

            throw new Exception("Email Inbox Empty...");
        }

        public static async Task<bool> DeleteEmailAsync(Message message)
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

            string messageId = "";
            foreach (var _message in messages)
            {
                if (_message.Id == message.Id)
                    messageId = _message.Id;
            }

            if (messageId == "")
                return false;

            await _graphClient
                .Users[_userId]
                .Messages[messageId]
                .Request()
                .DeleteAsync();

            return true;
        }

        public static bool IsValidWebexInvitation(Message message)
        {
            if (message is null)
                throw new Exception("Email Message Received was <NULL>");

            return message.Body.Content.Contains("Webex");
        }

        public static Meeting.MeetingInfo GetMeetingInfo(Message message)
        {
            if (message is null)
                throw new Exception("Email Message Received was <NULL>");

            var meetingInfo = new Meeting.MeetingInfo();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(message.Body.Content);
            var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//tbody/tr/td");

            if (htmlNodes is null)
                throw new Exception("Email is not in proper format");            

            for (int i = 0; i < htmlNodes.Count; i++)
            {
                var node = htmlNodes[i];
                string text = node.InnerText.Trim();

                if (text.Contains("Meeting number (access code): ")
                    && text.Length < 50)
                    meetingInfo.AccessCode = text.Replace(
                        "Meeting number (access code): ", "")
                        .Trim().Replace(" ", "");

                else if (text.Contains("Meeting password: ")
                    && text.Length < 50)
                    meetingInfo.Password = text.Replace(
                        "Meeting password: ", "")
                        .Trim().Replace(" ", "");

                else if (text.Contains(",")
                    && text.Length < 50)
                {
                    string date;
                    if (DateTime.TryParse(text, out DateTime _date))
                        date = _date.ToString().Substring(0, 10);
                    else
                        continue;

                    text = htmlNodes[i + 1].InnerText.Trim();

                    var time = text.Replace("&nbsp;", "");
                    time = time.Substring(0,
                        time.IndexOf("|",
                        StringComparison.Ordinal))
                        .ToUpper();

                    if (DateTime.TryParse(date + " " + time,
                        new CultureInfo("en-US"),
                        DateTimeStyles.AssumeLocal,
                        out DateTime date_time))
                        meetingInfo.StartTime = date_time;
                    else
                        continue;
                    break;

                    // TODO timezone differentiation
                }
            }
            return meetingInfo;
        }

        [HttpGet]
        private static async Task<Subscription> AddMailSubscription()
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
