using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Diagnostics;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace dialer_01.dialer
{
    class RecordingManager
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
        
        // given a string rid, download a recording to the Recordings File
        public async Task DownloadRecordingHandlerAsync(string rid)
        {
            using (var httpClient = new HttpClient())
            {
                // set url    
                string recordingURL = recordingBaseURL + rid;
                // make new uri with previous url
                Uri recordingURI = new Uri(recordingURL);
                Console.WriteLine("The following rid will be downloaded " + rid);

                // get response content from api
                var response = await httpClient.GetAsync(recordingURI, HttpCompletionOption.ResponseHeadersRead);

                // make sure request worked once headers are read
                response.EnsureSuccessStatusCode();

                // Save file to disk
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    // Define buffer and buffer size
                    int bufferSize = 1024;
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead = 0;

                    string filePath = ("..\\..\\..\\Recordings\\" + rid + ".wav");

                    // Read from response and write to file
                    using (FileStream fileStream = File.Create(filePath))
                    {
                        while ((bytesRead = stream.Read(buffer, 0, bufferSize)) != 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                        } // end while
                    }
                }
            }

            //TwilioClient.Init(accountSid, authToken);

            //var recording = RecordingResource.Fetch(
            //    pathSid: rid
            //    );
            //Console.WriteLine(recording);
        }

        // delete a recording given an SID
        // If successful, DELETE returns HTTP 204 (No Content) with no body
        public async Task DeleteRecordingAsync(string rid)
        {
            // Start initiate a twilio client
            TwilioClient.Init(accountSid, authToken);
            Console.WriteLine("The following rid will be deleted " + rid);
            // Deletes the recording resource with specified rid from Twilio cloud
            var response = await RecordingResource.DeleteAsync(pathSid: rid);
            // response should be true if recording was deleted
            Debug.Assert(response == true);
            Console.WriteLine(rid + " has been deleted.");
        }
    }
}
