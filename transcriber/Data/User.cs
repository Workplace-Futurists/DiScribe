using System;
using System.Collections.Generic;
using System.Text;

namespace transcriber.Data
{
    /// <summary>
    /// Represents a user who is a meeting participant. Contains attributes for user name, email address, and user ID.
    /// </summary>
    public class User : DataElement
    {
        public User(string name, string email, Guid userID)
        {
            Name = name;
            Email = email;
            UserID = userID;
        }

        public string Name { get; set; }

        public string Email { get; set; }

        public Guid UserID { get; private set; }

    }
}
