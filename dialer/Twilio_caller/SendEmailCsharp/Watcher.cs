using System;
using System.IO;
using System.Security.Permissions;

namespace twilio_caller.SendEmailCsharp
{
    public class Watcher
    {
        private static string sendGridAPIKey;
        private static bool sendGridUsed = false;
        /*
        public static void Main()
        {
            Run();
        }
        */

        private static void useSendGrid(string sendGridAPI)
        {
            SendEmailCsharp.Initialize(sendGridAPIKey);
            SendEmailCsharp.sendEmail().Wait();
            sendGridUsed = true;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void Run(string sendGridAPI)
        {
            /*
            string[] args = Environment.GetCommandLineArgs();

            // If a directory is not specified, exit program.
            if (args.Length != 2)
            {
                // Display the proper way to call the program.
                Console.WriteLine("Usage: Watcher.exe (directory)");
                return;
            }
            */

            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                sendGridAPIKey = sendGridAPI;
                // watcher.Path = args[1];
                // var bytes = File.ReadAllBytes("../../../cs319-2019w1-hsbc/transcriber/transcript");
                // var path = Convert.ToBase64String(bytes);
                watcher.Path = "../../../cs319-2019w1-hsbc/transcriber/transcript";

                // Watch for changes in LastWrite times, 
                // and the renaming of files
                watcher.NotifyFilter = NotifyFilters.LastWrite
                                    | NotifyFilters.FileName;

                // Only watch text files.
                watcher.Filter = "*.txt";

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                // Console.WriteLine("Press 'q' to quit and send email.");
                // while (Console.Read() != 'q');
                while (!sendGridUsed);
            }
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e) => 
            // Specify what is done when a file is changed, created, or deleted.
            // Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            useSendGrid(sendGridAPIKey);

        private static void OnRenamed(object source, RenamedEventArgs e) =>
            // Specify what is done when a file is renamed.
            // Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
            useSendGrid(sendGridAPIKey);
    }
}
