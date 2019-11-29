using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DiScribe.Dialer
{
    public static class Configurations
    {
        // TODO make this a proper class with get functions if we have time
        public static IConfigurationRoot LoadAppSettings()
        {
            DirectoryInfo dir = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory().Replace("bin/Debug/netcoreapp3.0", ""));
            string basepath;
            if (dir.Parent.Name == "cs319-2019w1-hsbc")
                basepath = dir.Parent.FullName;
            else
                basepath = Directory.GetCurrentDirectory();
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(basepath)
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
