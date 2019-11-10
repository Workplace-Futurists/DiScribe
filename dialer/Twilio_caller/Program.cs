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
using twilio_caller.dialer;

namespace twilio_caller
{
    class Program
    {
        // main program
        static void Main(string[] args)
        {
            // Call meeting
            // create object of type dialer
            dialerManager dialer = new dialerManager();

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
