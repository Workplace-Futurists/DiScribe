using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;


namespace DiScribe.Meeting
{   
    // this class is dependent on appsettings.json. if you wish to move this class, please remember to also move the dependent file appsettings.json
    public static class Configurations
    {
        // Reads the AppSettings.json file and adds its configurations to the returned IConfigurationRoot object
        public static IConfigurationRoot LoadAppSettings()
        {
            string basepath;
            #if DEBUG
                basepath = new DirectoryInfo(Directory
                        .GetCurrentDirectory()
                        .Replace("bin/Debug/netcoreapp3.0", "")
                        .Replace("bin/Release/netcoreapp3.0", ""))
                        .Parent.FullName;
            #else                
                basepath = Directory.GetCurrentDirectory();
            #endif

            var appConfig = new ConfigurationBuilder()
                .SetBasePath(basepath)
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            // Check for required settings
            if (string.IsNullOrEmpty(appConfig["appId"])
                || string.IsNullOrEmpty(appConfig["tenantId"])
                || string.IsNullOrEmpty(appConfig["clientSecret"])
                || string.IsNullOrEmpty(appConfig["BOT_Inbox"])
                || string.IsNullOrEmpty(appConfig["BOT_Inbox_Password"])
                || string.IsNullOrEmpty(appConfig["scopes:0"]) // Make sure there's at least one value in the scopes array
                || string.IsNullOrEmpty(appConfig["TWILIO_ACCOUNT_SID"])
                || string.IsNullOrEmpty(appConfig["TWILIO_AUTH_TOKEN"])
                || string.IsNullOrEmpty(appConfig["SENDGRID_API_KEY"])
                || string.IsNullOrEmpty(appConfig["BOT_Mail_Sender"]))
            {
                throw new Exception("Warning: one or more the required app settings are missing from appsettings.json");
            }
            return appConfig;
        }
    }
}
