using System;
using System.Collections.Generic;
using System.Text;

namespace dialer_01.Dialer
{
    public class Recording
    {
        // instantiated variables
        private string _rid = string.Empty;
        private DateTime _rCreationDate;

        // constructor
        public Recording()
        {
            // TODO: add recording id and datetime it was recorded
        }

        public string rid
        {
            get { return _rid; }
            set { _rid = value; }
        }
        public DateTime rCreationDate
        {
            get { return _rCreationDate; }
        }
    }
}
