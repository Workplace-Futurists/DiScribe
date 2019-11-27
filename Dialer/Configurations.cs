using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Dialer
{
    public static class Configurations
    {
        // TODO make this a proper class with get functions if we have time
        public static IConfigurationRoot LoadAppSettings()
        {
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            // Check for required settings
            if (string.IsNullOrEmpty(appConfig["appId"]) ||
            string.IsNullOrEmpty(appConfig["mailUser"]) ||
            string.IsNullOrEmpty(appConfig["mailPass"]) ||
            // Make sure there's at least one value in the scopes array
            string.IsNullOrEmpty(appConfig["scopes:0"]) ||
            string.IsNullOrEmpty(appConfig["TWILIO_ACCOUNT_SID"]) ||
            string.IsNullOrEmpty(appConfig["TWILIO_AUTH_TOKEN"]) ||
            string.IsNullOrEmpty(appConfig["SENDGRID_API_KEY"]))
            {
                return null;
            }
            return appConfig;
        }
    }
}
