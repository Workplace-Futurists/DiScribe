using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using twilio_caller.Dialer;

namespace twilio_caller
{
    class Program
    {
        //// a function to add pauses ('w' characters) between meeting call in numbers and extensions
        //// ex. 628079791
        //static string formatDigits(string meetingNum)
        //{
        //    // TODO: assert length of meeting number 

        //    // add necessary digits and pauses ('w') for send digits
        //    var result = "wwwwwwwwww1ww#wwww" +
        //        meetingNum[0] + "w" +
        //        meetingNum[1] + "w" +
        //        meetingNum[2] + "w" +
        //        meetingNum[3] + "w" +
        //        meetingNum[4] + "w" +
        //        meetingNum[5] + "w" +
        //        meetingNum[6] + "w" +
        //        meetingNum[7] + "w" +
        //        meetingNum[8] + "w#wwwww#";

        //    // return the result after concat
        //    return result;
        //}
        //// main program
        //static void Main(string[] args)
        //{

        //    // TODO: Adjust the code so that it prompts user to enter their phone# for the demo

        //    // Taken straight from the Twilio C# quickstart
        //    // Find your Account Sid and Auth Token at twilio.com/console
        //    // see https://www.twilio.com/docs/usage/secure-credentials to set up your env variables
        //    var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        //    var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        //    TwilioClient.Init(accountSid, authToken);

        //    // From the Twilio SIP guide
        //    //var call = CallResource.Create(
        //    //    url: new Uri("http://www.example.com/sipdial.xml"),
        //    //    to: new PhoneNumber("sip:628353018@cs319-futurists-test01.my.webex.com"),
        //    //    from: new PhoneNumber("+17787444195")
        //    //);

        //    // Tried using the SIP of a webex meeting and it fails. probably because this isn't an async method
        //    //var to = new PhoneNumber("sip:628353018@cs319-futurists-test01.my.webex.com");

        //    const string vancouverTollNum = "+12268289662";
        //    const string twilioAccNum = "+17787444195";
        //    string meetingNum = "628079791";

        //    // this is the webex call vancouver toll number
        //    var to = new PhoneNumber(vancouverTollNum);
        //    // This will work if you call a verified phone number (currently has mine)
        //    //var to = new PhoneNumber("+17786886112");

        //    // This is the twilio number linked to our account
        //    var from = new PhoneNumber(twilioAccNum);

        //    // makes the call resource to send
        //    var call = CallResource.Create(to, from,
        //        //method: Twilio.Http.HttpMethod.Get,
        //        //sendDigits: "ww1#ww628079791##",
        //        sendDigits: formatDigits(meetingNum),
        //        // Records the outgoing call
        //        record: true,
        //        // I think this is a default message that plays from the url?
        //        url: new Uri("http://lonelycompany.ca/test.xml")
        //     );

        //    Console.WriteLine(call.Sid);
        // main program
        static void Main(string[] args)
        {
            // Call meeting
            // create object of type dialer
            DialerManager dialer = new DialerManager();

            // sets variable for meeting number
            string mnum = "123456789";
            // executes call to webex meeting
            string callSid = dialer.CallMeeting(mnum);

            Console.WriteLine(callSid);

            // TODO send Sid to another method to download the recording
            // Download recording
            RecordingManager recManager = new RecordingManager();
            // set recording id
            string rid = "RE4250a08aac66a6a25f7147a3226e6376";
            // call helper to download recording
            recManager.DownloadRecordingHandler(rid);
        }

    }
}
