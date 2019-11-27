using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using DiScribe.DatabaseManager;
using DiScribe.DatabaseManager.Data;

namespace DiScribe.DiScribeDebug
{
    public class RegAudioTest
    {
        public static Boolean TestRegAudio(string userEmail)
        {
            WaveFormat format = new WaveFormat(16000, 16, 1);
            using (WaveFileWriter writer = new WaveFileWriter("test.wav", format))
            {

                User user = DatabaseManager.DatabaseController.LoadUser(userEmail);
                byte[] audioData = user.AudioStream.ToArray(); ;

                writer.Write(audioData, 0, audioData.Length);               //Write audio data to test.wav;.

            }

            return true;
        }

    }

}

