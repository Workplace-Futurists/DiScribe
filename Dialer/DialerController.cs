using System;
using System.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;

namespace DiScribe.Dialer
{
    public class DialerController
    {
        private static string _accountSid;
        private static string _authToken;

        public DialerController(IConfigurationRoot appConfig)
        {

            // add twilio authentication values
            _accountSid = appConfig["TWILIO_ACCOUNT_SID"];
            _authToken = appConfig["TWILIO_AUTH_TOKEN"];
        }
        // a function to add pauses ('w' characters) between meeting call in numbers and extensions
        // ex. 628079791
        private string FormatDigits(string meetingNum)
        {
            // TODO: assert length of meeting number 
            // add necessary digits and pauses ('w') for send digits
            var result = "wwwwwwwwww1ww#wwww" +
                meetingNum[0] + "w" +
                meetingNum[1] + "w" +
                meetingNum[2] + "w" +
                meetingNum[3] + "w" +
                meetingNum[4] + "w" +
                meetingNum[5] + "w" +
                meetingNum[6] + "w" +
                meetingNum[7] + "w" +
                meetingNum[8] + "w#wwwww#";

            // return the result after concat
            return result;
        }

        public async Task<string> CallMeetingAsync(string mNum)
        {
            Console.WriteLine("Dialing into webex meeting with access code " + mNum);
            TwilioClient.Init(_accountSid, _authToken);

            // call in number and call from number
            const string vancouverTollNum = "+14084189388";
            const string twilioAccNum = "+15046366992";
            //string meetingNum = "628079791";

            // this is the webex call vancouver toll number
            var to = new PhoneNumber(vancouverTollNum);
            // This will work if you call a verified phone number (currently has mine)
            //var to = new PhoneNumber("+17786886112");

            // This is the twilio number linked to our account
            var from = new PhoneNumber(twilioAccNum);

            Console.WriteLine(">\tCalling In...");
            // makes the call resource to send
            var call = CallResource.Create(to, from,
                //method: Twilio.Http.HttpMethod.Get,
                sendDigits: FormatDigits(mNum) + "wwww#",
                // Records the outgoing call
                record: true,
                // I think this is a default message that plays from the url?
                url: new Uri("http://lonelycompany.ca/test.xml")
             // default demo uri
             //url: new Uri("http://demo.twilio.com/docs/voice.xml")
             );

            var callSid = call.Sid;

            // set status to default value
            CallResource.StatusEnum status = CallResource.StatusEnum.Queued;

            // for checking what Sid was used in the loop
            CallResource finishedCall = null;

            // wait for meeting to finish
            while (status != CallResource.StatusEnum.Completed)
            {
                // return all the call resources for account user
                var calls = await CallResource.ReadAsync();

                // find element in list
                finishedCall = calls.First(record => record.Sid == callSid);
                var pendingStatus = finishedCall.Status;

                // check if the record is 
                if (pendingStatus == CallResource.StatusEnum.Completed)
                {
                    Console.WriteLine(">\tCall Completed");
                    // if the call has been completed return the completed status
                    status = pendingStatus;
                }
                else if (status == CallResource.StatusEnum.Canceled ||
                  status == CallResource.StatusEnum.Failed ||
                  status == CallResource.StatusEnum.NoAnswer)
                {
                    Console.Error.WriteLine(">\tThe call was not completed.");
                    break;
                }
            }

            //var subresourceUri = finishedCall.SubresourceUris;

            //Console.WriteLine("\nThe meeting has ended. The Call Resource status was: " + status + "\n");
            //Console.WriteLine("The call resource is: " + finishedCall);
            //Console.WriteLine("The call sid is: " + callSid);

            // retrieve 10 most recent recordings
            var recordings = RecordingResource.Read(limit: 10);
            var resultRecording = recordings.First(recording => recording.CallSid == callSid);

            //Console.WriteLine("The recording call sid is: " + resultRecording.CallSid);
            //Console.WriteLine("The recording sid is: " + resultRecording.Sid);

            return resultRecording.Sid;
        }
    }
}
