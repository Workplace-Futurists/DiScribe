using System;
using System.Collections.Generic;
using System.Text;

namespace SpeakerRegistration.Data
{
    /// <summary>
    /// Abstract class representing a generic data element. Mainly used as a container for database controller
    /// used by derived classes.
    /// Entity classes corresponding to database tables will derive from this class to access the database connection.
    /// </summary>
    public abstract class DataElement
    {
        public DataElement (DatabaseController controller)
        {
            Controller = controller;
        }

        protected DatabaseController Controller;


        public abstract Boolean Delete();
      
        
        public abstract Boolean Update();
     

        


    }
}
