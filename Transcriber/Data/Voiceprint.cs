using System;
using System.Collections.Generic;
using System.Text;

namespace FuturistTranscriber.Data
{   
    /// <summary>
    /// Represents a Voiceprint associated with a specific user. Contains raw audio data, the time stamp
    /// for audio recording, an instance ID, and the associated user ID.
    /// </summary>
    class Voiceprint : DataElement
    {
        public Voiceprint(byte[] audioSample, DateTime timeStamp = new DateTime(), int printID = 0, int userID = 0)
        {
            AudioSample = audioSample;
            TimeStamp = timeStamp;
            PrintID = printID;
            UserID = UserID;
        }

            
        public byte [] AudioSample { get; set; }

        public DateTime TimeStamp { get; set; }

        public int PrintID { get; private set; }

        public int UserID { get; private set; }
    }
}
