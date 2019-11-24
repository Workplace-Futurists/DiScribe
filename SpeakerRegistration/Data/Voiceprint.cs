using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DatabaseController.Data
{   
    /// <summary>
    /// Represents a Voiceprint associated with a specific user. Contains raw audio data, the time stamp
    /// for audio recording, an instance ID, and the associated user ID.
    /// </summary>
    public class Voiceprint : DataElement
    {
        public Voiceprint(byte[] audioSample, User associatedUser, Guid userGUID = new Guid(), DateTime timeStamp = new DateTime())
        {
            TimeStamp = timeStamp;
          
            AssociatedUser = associatedUser;
            UserGUID = userGUID;

            AudioStream = new MemoryStream(audioSample);
        }

        public Voiceprint(MemoryStream stream, User associatedUser, Guid userGUID = new Guid(), DateTime timeStamp = new DateTime())
        {
            AudioStream = stream;
            AssociatedUser = associatedUser;
            UserGUID = userGUID;

            TimeStamp = timeStamp;
        }


        override public Boolean Delete()
        {
            return false;
        }

        override public Boolean Update()
        {
            return false;
        }


        public User AssociatedUser { get; set; }


        public MemoryStream AudioStream { get; set; }


        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Profile ID of this user obtained from SpeakerRecognition API profile enrollment.
        /// </summary>
        public System.Guid UserGUID { get; set; }

     
    }
}
