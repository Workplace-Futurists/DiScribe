using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Timers;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace MeetingControllers
{
    class GraphHelper
    {
        private static GraphServiceClient _graphClient;

        // Subscription variables
        private const int MAX_SUB_EXPIRATION_MINS = 4230;
        private static Dictionary<string, Subscription> _Subscriptions = new Dictionary<string, Subscription>();
        private static System.Timers.Timer _subscriptionTimer = null;
        private static ArrayList meetings = new ArrayList();

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

        public static async Task<Boolean> ifNewEmail()
        {
            // TODO
            return false;
        }

        public static async Task<Message> GetEmailAsync()
        {
            var messages = await _graphClient.Me.Messages
                .Request()
                .Select(e => new
                {
                    e.Body
                })
                .GetAsync();
            return messages[0];
        }

        public static async void CreateMeetingInstance(string accessCode, DateTime date, TimeSpan length)
        {

            if ((date - DateTime.Now).TotalMilliseconds > 0)
            {
                //dialerManager dialer = new dialerManager("AC5869733a59d586bbcaf5d27249d7ff2f", "312b3283121fd9bd80ca6a8fb8ea847c");
                //Console.WriteLine($"The following meeting was added to the queue:{accessCode}");
                //Console.WriteLine($"Now waiting for your meeting to begin before joining.");
                //await Task.Delay((int)(date - DateTime.Now).TotalMilliseconds);
                //Console.WriteLine($"The recording process has begun for the following meeting: {accessCode}");
                //dialer.CallMeeting(accessCode);
            }
            else if (date.TimeOfDay + length > DateTime.Now.TimeOfDay)
            {
                //dialerManager dialer = new dialerManager("AC5869733a59d586bbcaf5d27249d7ff2f", "312b3283121fd9bd80ca6a8fb8ea847c");
                //Console.WriteLine($"The recording process has begun for the following meeting: {accessCode}");
                //dialer.CallMeeting(accessCode);
            }

        }

        public static async Task DeleteEmailAsync(Message message)
        {

            await _graphClient.Me.Messages[message.Id]
                .Request()
                .DeleteAsync();
        }

        public static async Task<string> GetEmailMeetingNumAsync()
        {
            Message message = await GetEmailAsync();

            if (message.Body.Content.Contains("Meeting number (access code):"))
            {
                string accessCode;
                string startDate;
                TimeSpan meetingLength;
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

                if (parsedEmail.Contains("hr") && !parsedEmail.Contains("min"))
                {
                    parsedEmail = parsedEmail.Substring(0, parsedEmail.IndexOf("h"));
                    meetingLength = TimeSpan.Parse(parsedEmail + ":00:00");
                }
                else if (parsedEmail.Contains("min") && !parsedEmail.Contains("hr"))
                {
                    parsedEmail = parsedEmail.Substring(0, parsedEmail.IndexOf("m"));
                    meetingLength = TimeSpan.Parse("00:" + parsedEmail + ":00");
                }
                else
                {
                    meetingLength = TimeSpan.Parse(parsedEmail.Substring(0, parsedEmail.IndexOf("h")) + ":" + parsedEmail.Substring(parsedEmail.IndexOf("r") + 1, parsedEmail.IndexOf("m") - 4) + ":00");
                }

                startDate = startDate.Substring(0, startDate.IndexOf("2019") + 21);
                DateTime date = DateTime.Parse(startDate);

                return accessCode;
                //if (!meetings.Contains(accessCode))
                //{
                //    meetings.Add(accessCode);
                //    Thread thread = new Thread(() => CreateMeetingInstance(accessCode, date, meetingLength));
                //    thread.Start();
                //}
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
