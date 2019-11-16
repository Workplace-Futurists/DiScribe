using System;
using System.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;

namespace twilio_caller.dialer
{
    public class dialerManager
    {
        string _accountSid;
        string _authToken;

        public dialerManager(string sid, string authTok)
        {
            _accountSid = sid;
            _authToken = authTok;
        }
        // a function to add pauses ('w' characters) between meeting call in numbers and extensions
        // ex. 628079791
        private string formatDigits(string meetingNum)
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

        public string CallMeeting(string mNum)
        {
            TwilioClient.Init(_accountSid, _authToken);

            // call in number and call from number
            const string vancouverTollNum = "+12268289662";
            const string twilioAccNum = "+17787444195";
            //string meetingNum = "628079791";

            // this is the webex call vancouver toll number
            var to = new PhoneNumber(vancouverTollNum);
            // This will work if you call a verified phone number (currently has mine)
            //var to = new PhoneNumber("+17786886112");

            // This is the twilio number linked to our account
            var from = new PhoneNumber(twilioAccNum);

            // makes the call resource to send
            var call = CallResource.Create(to, from,
                //method: Twilio.Http.HttpMethod.Get,
                sendDigits: formatDigits(mNum),
                // Records the outgoing call
                record: true,
                // I think this is a default message that plays from the url?
                //url: new Uri("http://lonelycompany.ca/test.xml")
                // default demo uri
                url: new Uri("http://demo.twilio.com/docs/voice.xml")
             );

            return call.Sid;
        }
    }
}
