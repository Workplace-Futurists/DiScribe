using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using DiScribe.DatabaseManager;
using DiScribe.DatabaseManager.Data;
using System.IO;
using DiScribe.AudioHandling;

namespace DiScribe.DiScribeDebug
{
    public class RegAudioTest
    {
        public static User TestLoadUser(string userEmail)
        {
            User user = null;
            WaveFormat format = new WaveFormat(16000, 16, 1);
            using (WaveFileWriter writer = new WaveFileWriter("test.wav", format))
            {
                DatabaseController.Initialize("Server = tcp:dbcs319discribe.database.windows.net, 1433; Initial Catalog = db_cs319_discribe; Persist Security Info = False; User ID = obiermann; Password = JKm3rQ~t9sBiemann; MultipleActiveResultSets = True; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30");
                user = DatabaseManager.DatabaseController.LoadUser(userEmail);

                if (user == null)
                    return null;

                byte[] audioData = user.AudioStream.ToArray(); ;

                writer.Write(audioData, 0, audioData.Length);               //Write audio data to test.wav;.

                
            }
            return user;


        }







        /// <summary>
        /// Method for test purposes to get voice samples from a WAV file
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public  static List<UserParams> MakeTestVoiceprints(FileInfo audioFile)
        {
            /*Offsets identifying times */
            ulong user1StartOffset = 1 * 1000;
            ulong user1EndOffset = 49 * 1000;

            ulong user2StartOffset = 51 * 1000;
            ulong user2EndOffset = 100 * 1000;

            ulong user3StartOffset = 101 * 1000;
            ulong user3EndOffset = 148 * 1000;

            ulong user4StartOffset = 151 * 1000;
            ulong user4EndOffset = 198 * 1000;

            AudioFileSplitter splitter = new AudioFileSplitter(audioFile);

            var user1Audio = splitter.WriteWavToStream(user1StartOffset, user1EndOffset).ToArray();
            var user2Audio = splitter.WriteWavToStream(user2StartOffset, user2EndOffset).ToArray();
            var user3Audio = splitter.WriteWavToStream(user3StartOffset, user3EndOffset).ToArray();
            var user4Audio = splitter.WriteWavToStream(user4StartOffset, user4EndOffset).ToArray();

            var format = new WaveFormat(16000, 16, 1);

            List<UserParams> voiceprints = new List<UserParams>()
            {
                 new UserParams(user1Audio, "Brian", "Kernighan", "B.Kernighan@example.com"),
                 new UserParams(user2Audio, "Janelle", "Shane", "J.Shane@example.com"),
                 new UserParams(user3Audio, "Nick",  "Smith", "N.Smith@example.com"),
                 new UserParams(user4Audio, "Patrick", "Shyu", "P.Shyu@example.com")
            };
            return voiceprints;
        }



    }



}

