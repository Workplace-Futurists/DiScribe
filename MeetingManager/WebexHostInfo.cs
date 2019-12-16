using System;
using System.Collections.Generic;
using System.Text;

namespace DiScribe
{
    public class WebexHostInfo
    {

        public WebexHostInfo()
        {

        }

        public WebexHostInfo(string email, string password, string id, string company, string timeZone = "")
        {
            Email = email;
            Password = password;
            ID = id;
            Company = company;
            TimeZone = timeZone;
        }

        public string Email {get; private set;}

        public string Password { get; private set; }

        public string ID { get; private set; }

        public string Company { get; private set; }

        public String TimeZone { get; private set; }
    }
}
