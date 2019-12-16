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
using SendGrid.Helpers.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System.Linq;


namespace DiScribe.Email
{
    public static class EmailListener
    {
        private static GraphServiceClient _graphClient;

        // Subscription variables
        private const int MAX_SUB_EXPIRATION_MINS = 4230;
        private static Dictionary<string, Subscription> _Subscriptions = new Dictionary<string, Subscription>();
        private static Timer _subscriptionTimer;
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

            Console.WriteLine(">\tInitialized Email Listener on Email : <" + principal + ">");
        }

        /// <summary>
        /// Get the User object representing this Graph user by principal. The principal
        /// is the email address for the account.
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static async Task<Microsoft.Graph.User> GetMeAsync(string principal)
        {

            var graphUsers = await _graphClient
                .Users
                .Request()
                .Filter($"startswith(Mail,'{principal}')")
                .GetAsync();

            if (graphUsers.Count > 0)
                return graphUsers[0];

            throw new Exception("Graph Users seem to be Empty");
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

        /// <summary>
        /// Get the most recent event for the bot Outlook account.
        /// </summary>
        /// <returns></returns>
        public static async Task<Event> GetEventAsync()
        {
            /*Get all messages for this user in inbox */
            var users = await _graphClient
                .Users
                .Request()
                .GetAsync();

            /*Get the most recent event for this user  */
            var events = await _graphClient
                .Users[_userId]
                .Events
                .Request()
                .OrderBy("CreatedDateTime DESC")
                .Top(1)
                .GetAsync();

            if (events is null)
                throw new Exception("Events retrieved were <NULL>");

            if (events.Count == 0)
                throw new Exception("No meetings scheduled.");

            return events.ToArray()[0];
        }

        /// <summary>
        /// Deletes the specified event for the bot Outlook account.
        /// Return true if success, and false if no such event was found
        /// </summary>
        /// <param name="inviteEvent"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteEventAsync(Event inviteEvent)
        {
            /*Get all messages for this user in inbox */
            var users = await _graphClient
                .Users
                .Request()
                .GetAsync();

            /*Get all messages for this user in inbox */
            var events = await _graphClient
                .Users[_userId]
                .Events
                .Request()
                .GetAsync();

            string eventId = "";

            foreach (var curEvent in events)
            {
                if (curEvent.Id == inviteEvent.Id)
                {
                    eventId = curEvent.Id;
                    break;
                }
            }

            /*No such event */
            if (eventId == "")
                return false;

            /*Otherwise, delete the specified event */
            Task f = _graphClient
                .Users[_userId]
                .Events[eventId]
                .Request()
                .DeleteAsync();

            f.Wait();

            return true;
        }

        /// <summary>
        /// Check if this is a valid Webex-generated invite message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>

        public static bool IsValidWebexInvitation(Event inviteEvent)
        {
            if (inviteEvent is null)
                throw new Exception("IsValidWebExInvitation: Email Message Received was <NULL>");

            return IsValidWebexInvitation(inviteEvent.Body.Content);
        }




        public static bool IsValidWebexInvitation(string body)
        {
            bool webExExist = body.Contains("Webex", StringComparison.OrdinalIgnoreCase);
            bool inviteExist = body.Contains("invites", StringComparison.OrdinalIgnoreCase);
            bool accessExist = body.Contains("Meeting number (access code):", StringComparison.OrdinalIgnoreCase);

            if (webExExist && inviteExist && accessExist)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Get the latest email for this user
        /// </summary>
        /// <returns></returns>
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

            if (inbox.TotalItemCount == 0)
                throw new Exception("Email Inbox Empty...");

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

            return messages[0];
        }



        //[ObsoleteAttribute("This method is deprecated and does not work in all cases.")]
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

        /// <summary>
        /// Check if this is a valid Outlook template invitate message to the Webex bot.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [ObsoleteAttribute("This method is deprecated and does not work in all cases.")]
        public static Boolean IsValidOutlookInvitation(Message message)
        {
            if (message is null)
                throw new Exception("ValidOutlookInvitation: Email Message Received was <NULL>");

            var content = message.Body.Content;
            return content.Contains("Subject", StringComparison.OrdinalIgnoreCase)
                 && content.Contains("Participants", StringComparison.OrdinalIgnoreCase)
                 && content.Contains("Start Date Time: ", StringComparison.OrdinalIgnoreCase)
                 && content.Contains("End Date Time: ", StringComparison.OrdinalIgnoreCase);
        }



        [ObsoleteAttribute("This method is obsolete and is no longer supported.")]
        public static Meeting.MeetingInfo GetMeetingInfoFromOutlookInvite(Message message)
        {
            var body = message.Body.Content;

            string subject = "";
            var participants = new List<SendGrid.Helpers.Mail.EmailAddress>();
            DateTime startTime = new DateTime();
            DateTime endTime = new DateTime();


            /*Get the meeting subject and remove the property name for next property from this string*/
            var subjectRegex = new Regex("Subject:.+Participants:");
            subject = subjectRegex.Match(body).Value.Replace("Participants:", "");

            /*Get the list of participant emails as a string and remove the property name of next property */
            var emailsRegex = new Regex("Participants:.+Start Date Time:");
            var emailsString = emailsRegex.Match(body).Value;
            emailsString = emailsString.Replace("Participants:", "").Replace("Start Date Time:", "");

            /*Convert email string to a list of SendGrid.Helpers.Mail.EmailAddress object */
            participants = ParseOutlookEmailsString(emailsString);

            /*Attempt to get start time */
            var startTimeRegex = new Regex("Start Date Time:+.End Date Time:");
            var startTimeStr = startTimeRegex.Match(body).Value;
            startTimeStr = startTimeStr.Replace("Start Date Time:", "").Replace("End Date Time:", "");
            Boolean startTimeParsed = DateTime.TryParse(startTimeStr, out startTime);

            /*Attempt to get end time */
            var endTimeRegex = new Regex("End Date Time:.+");
            var endTimeStr = endTimeRegex.Match(body).Value.Replace("End Date Time:", "");
            Boolean endTimeParsed = DateTime.TryParse(endTimeStr, out startTime);

            var meeting = new DatabaseManager.Data.Meeting(0, subject, "",
                startTime, endTime);




            return new Meeting.MeetingInfo(meeting, participants);
        }

        /// <summary>
        /// Converts a string list of email address to a list of SendGrid.Helpers.Mail.EmailAddress objects.
        /// </summary>
        /// <param name="emailList"></param>
        /// <returns></returns>
        public static List<SendGrid.Helpers.Mail.EmailAddress> ParseOutlookEmailsString(string emailListStr)
        {
            var emails = emailListStr.Trim().Split(",");
            var sendGridEmails = new List<SendGrid.Helpers.Mail.EmailAddress>();

            foreach (var emailStr in emails)
            {
                sendGridEmails.Add(new SendGrid.Helpers.Mail.EmailAddress(emailStr));
            }

            return sendGridEmails;
        }

        /// <summary>
        /// Converts a list of strings into a list of SendGrid.Helpers.Mail.EmailAddress representing
        /// emails.
        /// </summary>
        /// <param name="emailList"></param>
        /// <returns></returns>
        public static List<SendGrid.Helpers.Mail.EmailAddress> ParseEmailList(List<string> emailList)
        {
            var sendGridEmails = new List<SendGrid.Helpers.Mail.EmailAddress>();

            foreach (var emailStr in emailList)
            {
                sendGridEmails.Add(new SendGrid.Helpers.Mail.EmailAddress(emailStr));
            }

            return sendGridEmails;
        }


        /// <summary>
        /// Get a list of attendee email addresses as strings for the specified Event
        /// </summary>
        /// <param name="inviteEvent"></param>
        /// <returns></returns>
        public static List<string> GetAttendeeEmails(Microsoft.Graph.Event inviteEvent)
        {
            var emails = new List<string>();

            foreach (var curAttendee in inviteEvent.Attendees)
            {
                emails.Add(curAttendee.EmailAddress.Address);

            }

            return emails;
        }

        public static List<string> GetAttendeeNames(Microsoft.Graph.Event inviteEvent)
        {
            var names = new List<string>();

            foreach (var curAttendee in inviteEvent.Attendees)
            {

                names.Add(curAttendee.EmailAddress.Name);

            }

            return names;
        }



        /// <summary>
        /// Parses an html body email element to create a MeetingInfo object.
        /// </summary>
        /// <param name="inviteBody"></param>
        /// <param name="subject"></param>
        /// <param name="organizerAddress"></param>
        /// <param name="hostInfo"></param>
        /// <returns></returns>
        public static Meeting.MeetingInfo GetMeetingInfoFromWebexInvite(string inviteBody, string subject, WebexHostInfo hostInfo)
        {
            if (inviteBody is null)
                throw new Exception("Email body was <NULL>");

            if (!IsValidWebexInvitation(inviteBody))
                throw new Exception("Not a Webex Meeting Invitation Email");

            var meetingInfo = GetMeetingInfoFromWebexHTML(inviteBody, subject);

            meetingInfo.HostInfo = hostInfo;

            /*Add all other meeting attendees to meetingInfo*/
            meetingInfo.AttendeesEmails = Meeting.MeetingController.GetAttendeeEmails(meetingInfo);

            /* Add the host  as well */
            meetingInfo.AttendeesEmails.Add(new SendGrid.Helpers.Mail.EmailAddress(hostInfo.Email));
            meetingInfo.AttendeesEmails = meetingInfo.AttendeesEmails.Distinct().ToList();

            foreach (var attendee in meetingInfo.AttendeesEmails)
            {
                Console.WriteLine("\t-\t" + attendee.Email);
            }

            return meetingInfo;
        }



        private static Meeting.MeetingInfo GetMeetingInfoFromWebexHTML(string htmlBody, string subject)
        {
            var meetingInfo = new Meeting.MeetingInfo();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlBody);

            var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//tbody/tr/td");

            if (htmlNodes is null)
                throw new Exception("Email is not in proper format");

            string meeting_Sbj = subject;
            if (!String.IsNullOrEmpty(meeting_Sbj))
            {
                meeting_Sbj = meeting_Sbj.Replace("Webex meeting invitation:", "");
            }
            else
            {
                meeting_Sbj = "";
            }
            meetingInfo.Subject = meeting_Sbj;

            for (int i = 0; i < htmlNodes.Count; i++)
            {
                var node = htmlNodes[i];
                string text = node.InnerText.Trim();

                if (text.Contains("Meeting number (access code):")
                    && text.Length < 50)
                    meetingInfo.AccessCode = text.Replace(
                        "Meeting number (access code):", "")
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

                    text = htmlNodes[i + 1].InnerText
                        .Trim()
                        .Replace(time, "", StringComparison.OrdinalIgnoreCase)
                        .Replace("&nbsp;", "")
                        .Replace("|", "")
                        .Replace(" ", "");

                    var timezone = text.Substring(0,
                        text.IndexOf(")",
                        StringComparison.Ordinal))
                        .Replace("(", "")
                        .Replace("UTC", "");

                    var sum = (date + " " + time + " " + timezone).Trim();

                    if (DateTime.TryParse(sum, out DateTime date_time))
                        meetingInfo.StartTime = date_time.ToLocalTime();
                    else
                        continue;
                    break;
                }
            }
            if (meetingInfo.MissingAccessInfo())
                throw new Exception("Important fields missing for MeetingInfo class");

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

                    NotificationUrl = "https://discribefunctionapp.azurewebsites.net/api/subCreatorTest?code=h74tSOzgvTGtYZQ6pql0gEPxR1gnmDjL2bD67/hdqzho86y3vMa3Ww==",


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
