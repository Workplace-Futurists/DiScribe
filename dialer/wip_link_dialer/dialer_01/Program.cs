using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using dialer_01.dialer;

namespace twilio_caller
{
    class Program
    {

        
        // main program
        static void Main(string[] args)
        {
            // Welcome message and FAQ
            Console.WriteLine("Welcome to the DiScribe Meeting transcriber.\n");
            Console.WriteLine("Make sure your Twilio Login has been saved as the correct environment variables to run this application.");
            Console.WriteLine("There is no system in place to guard against meeting recording length for testing.");
            Console.WriteLine("Please do not run system on long meetings, or storage fees will be charged.\n\n");
            Console.WriteLine("After making your WebEx, please start the meeting and input your meeting number.");

            // sets variable for meeting number
            //string mnum = "123456789";
            string mnum = Console.ReadLine();
            
            // Call meeting
            // create object of type dialer
            dialer dialer = new dialer();

            // executes call to webex meeting
            string callSid = dialer.CallMeeting(mnum);
            // display CallSid to console
            Console.WriteLine("The call had the following Sid: " + callSid);

            // prompt user for recording resource sid
            Console.WriteLine("Below is the start of code to download a recording.");
            Console.WriteLine("Currently recordings are not saved.");
            Console.WriteLine("To test fetching a recording resource, feel free to use the following sid: RE4250a08aac66a6a25f7147a3226e6376");
            Console.WriteLine("Enter the Recording Sid for the desired Twilio Recording Resource to fetch: ");

            // TODO send Sid to another method to download the recording
            // Download recording
            RecordingManager recManager = new RecordingManager();
            // set recording id
            //string rid = "RE4250a08aac66a6a25f7147a3226e6376";
            string rid = Console.ReadLine();
            // call helper to download recording
            recManager.DownloadRecordingHandler(rid);
        }
    }
}
