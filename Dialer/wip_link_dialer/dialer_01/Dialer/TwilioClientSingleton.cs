using System;
using System.Collections.Generic;
using System.Text;
using Twilio;

namespace dialer_01.dialer
{
    public class TwilioClientSingleton
    {
        // the instance variable of this client
        private static TwilioClientSingleton instance;
        private string accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        private string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");

        private TwilioClientSingleton() { }

        public static TwilioClientSingleton Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TwilioClientSingleton();
                }
                return instance;
            }
        }

    }
}
