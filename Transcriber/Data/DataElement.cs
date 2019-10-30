using System;
using System.Collections.Generic;
using System.Text;

namespace Transcriber.Data
{
    /// <summary>
    /// Abstract class representing a generic data element. Mainly used as a container for database connection.
    /// Entity classes corresponding to database tables will inherit from this class to access the database connection.
    /// </summary>
    abstract class DataElement
    {
    }
}
