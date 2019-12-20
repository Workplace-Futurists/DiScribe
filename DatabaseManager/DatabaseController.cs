using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text;
using DiScribe.DatabaseManager.Data;

namespace DiScribe.DatabaseManager
{
    /// <summary>
    /// Provides access to the stored procedures provided by the MS SQL registration database.
    /// This supports the standard CRUD operations on User objects for registration/lookup/update/deregistraiton.
    /// </summary>
    public static class DatabaseController
    {
        
        private static SqlConnection DBConnection = null;
        private static Boolean initialized = false;

        public static void Initialize(string connectionStr = "!!ADD DB CONNECTION STRING!!")
        {
            
            var connection = new SqlConnection(connectionStr);
            connection.Open();
            DBConnection = connection;
            

            initialized = true;
        }



        /// <summary>
        /// Stores a user with params matching userParams in the database.
        /// </summary>
        /// <param name="userParams"></param>
        /// <returns></returns>
        public static User CreateUser(UserParams userParams)
        {
            return CreateUser(userParams.AudioSample,
                userParams.FirstName,
                userParams.LastName,
                userParams.Email,
                userParams.ProfileGUID,
                userParams.TimeStamp,
                userParams.Password);
        }



        /// <summary>
        /// Stores a user record in the database and returns an object representing that User.
        /// </summary>
        /// <param name="audioSample"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="email"></param>
        /// <param name="profileGUID"></param>
        /// <param name="timeStamp"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static User CreateUser(
           byte[] audioSample,
           string firstName,
           string lastName,
           string email,
           Guid profileGUID = new Guid(),
           DateTime timeStamp = new DateTime(),
           string password = "")
        {
            if (!initialized)
                  Initialize();

            SqlTransaction transaction = DBConnection.BeginTransaction();
            string execStr = "dbo.stpCreateUser";

            using (SqlCommand command = new SqlCommand(execStr, DBConnection, transaction))
            {
                try
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    //@FirstName nvarchar(50),
                    //@LastName nvarchar(50),
                    //@Email nvarchar(254),
                    //@ProfileGUID nvarchar(128),
                    //@AudioSample varbinary(max),
                    //@TimeStamp datetime = null,
                    //@Password nvarchar(100) = null,
                    //@RowID INT OUTPUT

                    var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@FirstName", firstName),
                        new SqlParameter("@LastName", lastName),
                        new SqlParameter("@Email", email),
                        new SqlParameter("@ProfileGUID", profileGUID),
                        new SqlParameter("@AudioSample", audioSample),
                        new SqlParameter("@TimeStamp", timeStamp),
                        new SqlParameter("@Password", password)
                    };

                    SqlParameter result = new SqlParameter("@RowID", System.Data.SqlDbType.Int);

                    command.Parameters.AddRange(parameters.ToArray());
                    command.Parameters.Add(result).Direction = System.Data.ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    transaction.Commit();

                    int rowID = Convert.ToInt32(command.Parameters["@RowID"].Value);

                    /*Return User object representing this user */
                    return new User(new UserParams(audioSample, firstName, lastName, email, profileGUID, rowID, timeStamp, password));
                }
                catch (Exception ex)
                {
                    Console.Error.Write($"Error creating user profile in database. {ex.Message}");
                    transaction.Rollback();
                }
            }
            return null;
        }

        public static Boolean CheckUser(string email)
        {

            if (!initialized)
                Initialize();


            using (SqlTransaction transaction = DBConnection.BeginTransaction())
            {
                string execStr = "dbo.stpLoadUser";

                using (SqlCommand command = new SqlCommand(execStr, DBConnection, transaction))
                {
                    try
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        SqlParameter emailParam = new SqlParameter("@email", email);
                        command.Parameters.Add(emailParam);


                        using (var reader = command.ExecuteReader())
                        {
                            Boolean canRead = reader.Read();
                            return canRead;
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.Error.Write($"Error checking profile in database  {ex.Message}");
                        transaction.Rollback();
                        return false;
                    }
                }

            }
        }

        /// <summary>
        /// Attempts to load a user with a matching email address from the DiScribe DB.
        /// </summary>
        /// <param name="email"></param>
        /// <returns>Matching User object for the specified email, or null if no such user exists.</returns>
        public static User LoadUser(string email)
        {

            if (!initialized)
                Initialize();


            using (SqlTransaction transaction = DBConnection.BeginTransaction())
            {
                string execStr = "dbo.stpLoadUser";

                using (SqlCommand command = new SqlCommand(execStr, DBConnection, transaction))
                {
                    try
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        SqlParameter emailParam = new SqlParameter("@email", email);
                        command.Parameters.Add(emailParam);

                        User result = null;

                        using (var reader = command.ExecuteReader())
                        {
                            Boolean canRead = false;
                            if (canRead = reader.Read())
                            {
                                string firstName = (reader["FirstName"] is DBNull ? "" : Convert.ToString(reader["FirstName"]));
                                string lastName = (reader["LastName"] is DBNull ? "" : Convert.ToString(reader["LastName"]));
                                string password = (reader["Password"] is DBNull ? "" : Convert.ToString(reader["Password"]));

                                Guid profileGuid = (reader["ProfileGUID"] is DBNull ? new Guid() :  new Guid(Convert.ToString(reader["ProfileGUID"])));
                                int userID = ((reader["UserID"]) is DBNull ? -1 : Convert.ToInt32(reader["UserID"]));

                                byte[] audioSample = (reader["AudioSample"] is DBNull ? null : (byte[])(reader["AudioSample"]));
                                DateTime timestamp = (reader["TimeStamp"]) is DBNull ? new DateTime() : Convert.ToDateTime(reader["TimeStamp"]);

                                result = new User(new UserParams(audioSample, firstName, lastName, email, profileGuid, userID, timestamp, password));
                            }
                        }
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.Write($"Error loading user profile from database. {ex.Message}");
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public static Boolean UpdateUser(User user, string lookupEmail)
        {

            if (!initialized)
                Initialize();

            SqlTransaction transaction = DBConnection.BeginTransaction();
            string execStr = "dbo.stpUpdateUserByEmail";

            using (SqlCommand command = new SqlCommand(execStr, DBConnection, transaction))
            {
                try
                {
                    //@LookupEmail nvarchar(254),
                    //@FirstName nvarchar(50),
                    //@LastName nvarchar(50),
                    //@Email nvarchar(254),
                    //@ProfileGUID nvarchar(128),
                    //@AudioSample varbinary(max),
                    //@Password nvarchar(100) = null,
                    //@TimeStamp datetime = null,
                    //@RowID INT OUTPUT
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                  

                    var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@LookupEmail", lookupEmail),
                        new SqlParameter("@FirstName", user.FirstName),
                        new SqlParameter("@LastName", user.LastName),
                        new SqlParameter("@Email", user.Email),
                        new SqlParameter("@ProfileGUID", user.ProfileGUID.ToString()),
                        new SqlParameter("@AudioSample", user.AudioStream.ToArray()),
                        new SqlParameter("@TimeStamp", user.TimeStamp),
                        new SqlParameter("@Password", user.Password)
                    };

                    SqlParameter result = new SqlParameter("@Outcome", System.Data.SqlDbType.Int);
                    result.Direction = System.Data.ParameterDirection.Output;

                    command.Parameters.AddRange(parameters.ToArray());
                    command.Parameters.Add(result);
                                        

                    command.ExecuteNonQuery();

                    transaction.Commit();


                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.Write($"Error updating user profile in database. {ex.Message}");
                    transaction.Rollback();
                }
            }
            return false;
        }




        public static Boolean DeleteUser(string email)
        {
            if (!initialized)
                Initialize();


            SqlTransaction transaction = DBConnection.BeginTransaction();
            string execStr = "dbo.stpDeleteUser";

            using (SqlCommand command = new SqlCommand(execStr, DBConnection, transaction))
            {
                try
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;                             //Specify that this is a stp.

                    SqlParameter deleteParam = new SqlParameter("@email", System.Data.SqlDbType.NVarChar);
                    deleteParam.Value = email;

                    /*Expecting a bit (boolean) return value to indicate success or failure */
                    SqlParameter resultParam = new SqlParameter("@result", System.Data.SqlDbType.Bit);

                    command.Parameters.Add(deleteParam);
                    command.Parameters.Add(resultParam).Direction = System.Data.ParameterDirection.Output;      //Mark this param as output.

                    command.ExecuteNonQuery();

                    transaction.Commit();

                    return Convert.ToBoolean(command.Parameters["@result"].Value);

                }
                catch (Exception ex)
                {
                    Console.Error.Write($"Error deleting user profile from database. {ex.Message}");
                    transaction.Rollback();
                }
            }
            return false;
        }

        public static List<string> GetUnregisteredUsersFrom(List<string> emails)
        {
            var unregistered = new List<string>();
            foreach (string email in emails)
            {
                if (!CheckUser(email))
                    unregistered.Add(email);
            }
            return unregistered;
        }



        /// <summary>
        /// Creates a Meeting record with fields corresponding to the specified meeting params.
        /// </summary>
        /// <returns></returns>
        public static Meeting CreateMeeting(List<String> userEmails, 
            DateTime meetingStartDateTime,
            DateTime meetingEndDateTime,
            string webExID,
            string meetingSubject = "",
            string meetingMinutes = "",
            string meetingFileLocation = "" )
        {

            if (!initialized)
                Initialize();



            var userIDs = GetUserIDsByEmail(userEmails);                                //Load the ids for the specified emails

            string userIDString = CreateUserIdString(userIDs);                         //Convert user IDs to a comma-delimited string.

        
            SqlTransaction transaction = DBConnection.BeginTransaction();
            string execStr = "dbo.stpCreateMeeting";

            using (SqlCommand command = new SqlCommand(execStr, DBConnection, transaction))
            {
                try
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    
                    /* 
                        @AttendeesStr nvarchar(MAX),
	                    @MeetingStartDateTime datetime = null,
                        @MeetingEndDateTime datetime = null,
	                    @WebExID nvarchar(100) = null,
                        @MeetingSubject nvarchar(500) = null,
	                    @MeetingMinutes nvarchar(MAX) = null,
	                    @MeetingFileLocation nvarchar(500) = null
                    */

                    var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@AttendeesStr", userIDString),
                        new SqlParameter("@MeetingStartDateTime", meetingStartDateTime),
                        new SqlParameter("@MeetingEndDateTime", meetingEndDateTime),
                        new SqlParameter("@WebExID", webExID),
                        new SqlParameter("@MeetingSubject", meetingSubject),
                        new SqlParameter("@MeetingMinutes", meetingMinutes),
                        new SqlParameter("@MeetingFileLocation", meetingFileLocation)
                    };

                    SqlParameter result = new SqlParameter("@RowID", System.Data.SqlDbType.Int);

                    command.Parameters.AddRange(parameters.ToArray());
                    command.Parameters.Add(result).Direction = System.Data.ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    transaction.Commit();

                    int rowID = Convert.ToInt32(command.Parameters["@RowID"].Value);

                    /*Return User object representing this user */
                    return new Meeting(rowID,
                        meetingSubject,
                        meetingMinutes,
                        meetingStartDateTime,
                        meetingEndDateTime,
                        meetingFileLocation,
                        webExID);
                }
                catch (Exception ex)
                {
                    Console.Error.Write($"Error creating user profile in database. {ex.Message}");
                    transaction.Rollback();
                }
            }



            return null;
        }





        /// <summary>
        /// Updates the Meeting record with the properties of the specified Meeting object.
        /// </summary>
        /// <param name="meeting"></param>
        /// <returns>True if update is successful, false otherwise</returns>
        public static Boolean UpdateMeeting(Meeting meeting)
        {
            if (!initialized)
                Initialize();

            SqlTransaction transaction = DBConnection.BeginTransaction();
            string execStr = "dbo.stpUpdateMeeting";

            using (SqlCommand command = new SqlCommand(execStr, DBConnection, transaction))
            {
                try
                {
                    /* stpUpdateMeeting params:
                        @LookupID bigint,
                        @MeetingStartDateTime datetime = null,
	                    @MeetingEndDateTime datetime = null,
                        @WebExID nvarchar(100) = null,
                        @MeetingSubject nvarchar(500) = null,
	                    @MeetingMinutes nvarchar(MAX) = null,
	                    @MeetingFileLocation nvarchar(500) = null,
	                    @Outcome int OUT
                     */
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    
                    var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@LookupID", meeting.MeetingId),
                        new SqlParameter("@MeetingStartDateTime", meeting.MeetingStartDateTime == default ? SqlDateTime.MinValue : meeting.MeetingStartDateTime),
                        new SqlParameter("@MeetingEndDateTime", meeting.MeetingEndDateTime == default ? SqlDateTime.MinValue : meeting.MeetingEndDateTime),
                        new SqlParameter("@WebExID", meeting.WebExID),
                        new SqlParameter("@MeetingSubject", meeting.MeetingSubject),
                        new SqlParameter("@MeetingMinutes", meeting.MeetingMinutes),
                        new SqlParameter("@MeetingFileLocation", meeting.MeetingFileLocation),
                    };

                    SqlParameter result = new SqlParameter("@Outcome", System.Data.SqlDbType.Int);
                    result.Direction = System.Data.ParameterDirection.Output;

                    command.Parameters.AddRange(parameters.ToArray());
                    command.Parameters.Add(result);


                    command.ExecuteNonQuery();

                    transaction.Commit();


                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.Write($">\tError updating meeting record in database. {ex.Message}");
                    transaction.Rollback();
                }
            }
            return false;
 
        }



        /// <summary>
        /// Get a list of user IDs for the user records with the specified emails
        /// </summary>
        /// <param name="emails"></param>
        /// <returns></returns>
        public static List<string> GetUserIDsByEmail(List<string> emails)
        {

            var userIDList = new List<string>();

            try
            {
                if (!initialized)
                    Initialize();

                

                foreach (var email in emails)
                {
                    var curUser = LoadUser(email);
                    if (curUser is null)
                        continue;

                    userIDList.Add(curUser.UserID.ToString());
                }

                
            } catch (Exception ex)
            {
                Console.Error.WriteLine($">\tError loading user ids by email. Reason: {ex.Message}");
            }

            return userIDList;


        }


        /// <summary>
        /// Create a string containing all user ids in users delimited by commas.
        /// </summary>
        /// <param name="users"></param>
        private static string CreateUserIdString(List<string> userIDs)
        {


            if (userIDs.Count == 0)
                return "";

            StringBuilder builder = new StringBuilder();
            
            /*Append user ID's to the stringbuilder delimited by commas. Do not add a comma after the last id*/
            for(int i = 0; i < userIDs.Count; i++)
            {
                builder.Append((i < userIDs.Count - 1 ? $"{userIDs[i]}," : userIDs[i]));
            }


            return builder.ToString();

        }





    }
}
