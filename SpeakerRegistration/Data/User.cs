using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseController.Data
{
    /// <summary>
    /// Represents a user who is a meeting participant. Contains attributes for user name, email address, and user ID.
    /// </summary>
    public class User : DataElement
    {
        public User(string name, string email, int userID)
        {
            Name = name;
            Email = email;
            UserID = userID;
        }


        override public Boolean Delete()
        {
            return false;
        }

        override public Boolean Update()
        {
            return false;
        }

        public string Name { get; set; }

        public string Email { get; set; }

        public int UserID { get; private set; }

    }
}
