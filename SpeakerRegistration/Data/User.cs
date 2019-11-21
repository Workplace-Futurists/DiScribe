using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpeakerRegistration.Data
{   
    /// <summary>
    /// Represents a Voiceprint associated with a specific user. Contains raw audio data, the time stamp
    /// for audio recording, an instance ID, and the associated user ID.
    /// </summary>
    
    public class User : DataElement
    {
       /// <summary>
       /// Construct a User which uses the specified controller to interact with the DB.
       /// </summary>
       /// <param name="controller"></param>
       /// <param name="stream"></param>
       /// <param name="firstName"></param>
       /// <param name="lastName"></param>
       /// <param name="email"></param>
       /// <param name="profileGUID"></param>
       /// <param name="userID"></param>
       /// <param name="timeStamp"></param>
       /// <param name="password"></param>
       public User(DatabaseController controller,
            MemoryStream stream,
            string firstName,
            string lastName,
            string email,
            Guid profileGUID = new Guid(),
            int userID = -1,
            DateTime timeStamp = new DateTime(),
            string password = "") : base(controller)
        {
            AudioStream = stream;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            ProfileGUID = ProfileGUID;
            UserID = userID;
            TimeStamp = timeStamp;
            Password = password;
        }


        /// <summary>
        /// Overrides constructor to creates stream from the byte buffer instead
        /// of accepting a stream directly.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="audioSample"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="email"></param>
        /// <param name="profileGUID"></param>
        /// <param name="userID"></param>
        /// <param name="timestamp"></param>
        /// <param name="password"></param>
        public User(DatabaseController controller,
           byte[] audioSample,
           string firstName,
           string lastName,
           string email,
           Guid profileGUID = new Guid(),
           int userID = -1,
           DateTime timeStamp = new DateTime(),
           string password = "") : this(
               controller, 
               new MemoryStream(audioSample),
               firstName,
               lastName,
               email,
               profileGUID,
               userID,
               timeStamp)
        {    }


        /// <summary>
        /// Deletes the Voiceprint from the associated database.
        /// </summary>
        /// <returns></returns>
        override public Boolean Delete()
        {
            return false;
        }

        /// <summary>
        /// Updates the voiceprint in the database.
        /// </summary>
        /// <returns></returns>
        override public Boolean Update()
        {
            return false;
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        /// <summary>
        /// Profile ID of this user obtained from SpeakerRecognition API profile enrollment.
        /// </summary>
        public System.Guid ProfileGUID { get; set; }

               
        public int UserID { get; private set; }


        public MemoryStream AudioStream { get; set; }


        public DateTime TimeStamp { get; set; }


        public string Password { get; set; }
             

        public DatabaseController Controller { get; private set; }

      
    }
}
