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

namespace twilio_caller
{
    class Program
    {
        static void Main(string[] args)
        {

            // TODO: Adjust the code so that it prompts user to enter their phone# for the demo

            // Taken straight from the Twilio C# quickstart
            // Find your Account Sid and Auth Token at twilio.com/console
            const string accountSid = "AC5869733a59d586bbcaf5d27249d7ff2f";
            const string authToken = "312b3283121fd9bd80ca6a8fb8ea847c";
            TwilioClient.Init(accountSid, authToken);

            // From the Twilio SIP guide
            //var call = CallResource.Create(
            //    url: new Uri("http://www.example.com/sipdial.xml"),
            //    to: new PhoneNumber("sip:628353018@cs319-futurists-test01.my.webex.com"),
            //    from: new PhoneNumber("+17787444195")
            //);

            // Tried using the SIP of a webex meeting and it fails. probably because this isn't an async method
            //var to = new PhoneNumber("sip:628353018@cs319-futurists-test01.my.webex.com");

            // This will work if you call a verified phone number (currently has mine)
            var to = new PhoneNumber("+7786886112");
            // This is the twilio number linked to our account
            var from = new PhoneNumber("+17787444195");

            // makes the call resource to send
            var call = CallResource.Create(to, from,
                // I think this is a default message that plays from the url?
                url: new Uri("http://demo.twilio.com/docs/voice.xml"),
                // Records the outgoing call
                record: true);

            Console.WriteLine(call.Sid);
        }
    }
}
