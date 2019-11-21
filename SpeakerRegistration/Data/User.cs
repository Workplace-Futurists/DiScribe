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
        /// Overrides constructor to use a UserParams object instead of many parameters
        /// for convenience and readability.
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
        public User(DatabaseController controller, UserParams userParams) : 
            this(controller, 
                new MemoryStream(userParams.AudioSample),
                userParams.FirstName,
                userParams.LastName,
                userParams.Email,
                userParams.ProfileGUID,
                userParams.UserID,
                userParams.TimeStamp,
                userParams.Password)
        { }




        /// <summary>
        /// Deletes the Voiceprint from the associated database.
        /// </summary>
        /// <returns></returns>
        override public Boolean Delete()
        {
            return Controller.DeleteUser(this.Email);
            
        }

        /// <summary>
        /// Updates the voiceprint in the database. Note that a lookup
        /// email can be specified, if the property or properties to update 
        /// include the email for this user.
        /// </summary>
        /// <returns></returns>
        override public Boolean Update(string lookupEmail = null)
        {
            string email = lookupEmail;
            if (email == null)
            {
                email = this.Email;
            }

            return Controller.UpdateUser(this, email);
            
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

    public class UserParams
    {
        public UserParams (
           byte[] audioSample,
           string firstName,
           string lastName,
           string email,
           Guid profileGUID = new Guid(),
           int userID = -1,
           DateTime timeStamp = new DateTime(),
           string password = "") 
          {
            AudioSample = audioSample;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            ProfileGUID = ProfileGUID;
            UserID = userID;
            TimeStamp = timeStamp;
            Password = password;
        }


        public byte[] AudioSample { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public Guid ProfileGUID { get; set; }
        public int UserID { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Password { get; set; }



    }
}
