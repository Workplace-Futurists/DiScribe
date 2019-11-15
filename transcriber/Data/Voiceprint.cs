using System;
using System.Collections.Generic;
using System.Text;

namespace transcriber.Data
{   
    /// <summary>
    /// Represents a Voiceprint associated with a specific user. Contains raw audio data, the time stamp
    /// for audio recording, an instance ID, and the associated user ID.
    /// </summary>
    public class Voiceprint : DataElement
    {
        public Voiceprint(byte[] audioSample, System.Guid userGUID, User associatedUser = null, DateTime timeStamp = new DateTime())
        {
            AudioSample = audioSample;
            TimeStamp = timeStamp;
          
            AssociatedUser = associatedUser;
            UserGUID = userGUID;
        }

            
        public User AssociatedUser { get; set; }


        public byte [] AudioSample { get; set; }


        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Profile ID of this user obtained from SpeakerRecognition API profile enrollment.
        /// </summary>
        public System.Guid UserGUID { get; private set; }

     
    }
}
