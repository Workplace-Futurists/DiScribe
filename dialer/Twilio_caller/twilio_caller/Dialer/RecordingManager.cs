using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace twilio_caller.Dialer
{
    public class RecordingManager
    {
        private static string accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        private static string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        // AC58697...
        private static string recordingBaseURL = "https://" + accountSid + ":" + authToken +
            "@api.twilio.com/2010-04-01/Accounts/" + accountSid +
            "/Recordings/";

        // TODO method to get recordings from an account
        public async Task<String> ListRecordingsAsync()
        {
            string result = "";
            return result;
        }

        // TODO given an rid, download a recording
        public async void DownloadRecordingHandler(string rid)
        {
            using (var httpClient = new HttpClient())
            {
                string recordingURL = recordingBaseURL + rid;
                Console.WriteLine(recordingURL);
                var response = httpClient.GetAsync(new Uri(recordingURL)).Result;
                Console.WriteLine(response);
            }

            //TwilioClient.Init(accountSid, authToken);

            //var recording = RecordingResource.Fetch(
            //    pathSid: rid
            //    );
            //Console.WriteLine(recording);
        }

        // TODO delete a recording given an SID
        // If successful, DELETE returns HTTP 204 (No Content) with no body
        // only event handlers should be void async methods
        public static async void DeleteRecordingAsync(string pathSid)
        {
            // instantiate login info with twilio client
            var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
            Twilio.TwilioClient.Init(accountSid, authToken);
        }
    }
}
