
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SpeakerRegistration.Data;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;

namespace SpeakerRegistration
{
    /// <summary>
    /// Provides access to profile registration functionality for DiScribe user profiles
    /// with the Azure Speaker Recognition service. Allows new profiles
    /// to be registered and checks to determine if a user is registered.
    /// </summary>
    public class RegistrationController
    {
        /// <summary>
        /// Ensures that all DiScribe User profiles have matching profiles in the
        /// the Azure Speaker Recognition service. Creates a valid RegistrationController
        /// for registering additional users.
        /// </summary>
        /// <param name="dbController"></param>
        /// <param name="userEmails"></param>
        /// <param name="speakerIDKeySub"></param>
        /// <param name="enrollmentLocale"></param>
        /// <param name="apiInterval"></param>
        public RegistrationController(DatabaseController dbController, List<User> userProfiles, SpeakerIdentificationServiceClient enrollmentClient, 
            string enrollmentLocale, int apiInterval)
        {
            /*Create REST client for enrolling users */
            EnrollmentClient = enrollmentClient;
            EnrollmentLocale = enrollmentLocale;

            DBController = dbController;

            
            UserProfiles = userProfiles;

            if (userProfiles.Count > 0)
            {
                /*Ensure that all DiScribe users have profiles enrolled with the Azure Speaker Recognition endpoint */
                EnrollVoiceProfiles().Wait();
            }
        }


        /// <summary>
        /// Builder to create a Registration controller. Creates a connection to the database
        /// and loads profiles using the collection of associated user emails.
        /// 
        /// By default, has "en-us" enrollment locale
        /// and a delay between concurrent requests specified by SPEAKER_RECOGNITION_API_INTERVAL
        /// </summary>
        /// <param name="dbConnectionString"></param>
        /// <param name="userEmails"></param>
        /// <param name="speakerIDKeySub"></param>
        public static RegistrationController BuildController(string dbConnStr, List<string> userEmails, string speakerIDKeySub,
            string enrollmentLocale = "en-us", int apiInterval = SPEAKER_RECOGNITION_API_INTERVAL)
        {
            DatabaseController dbController;
            try
            { 
                dbController = new DatabaseController(dbConnStr);
            } catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                throw new Exception("Unable to create Registration Controller due to database connection error");
            }


            
            SpeakerIdentificationServiceClient enrollmentClient = new SpeakerIdentificationServiceClient(speakerIDKeySub);
            List<User> userProfiles = new List<User>();

            try
            {
                foreach (var curEmail in userEmails)
                {
                    User curUser = dbController.LoadUser(curEmail);
                    userProfiles.Add(curUser);
                }


            }catch (Exception ex)
            {
                Console.Error.WriteLine("Loading profiles from database failed. " + ex.Message);
                throw new Exception("Unable to create Registration Controller due to database error");
            }


            return new RegistrationController(dbController, userProfiles, enrollmentClient, enrollmentLocale, apiInterval);


        }





        public const int SPEAKER_RECOGNITION_API_INTERVAL = 3000;                               //Min time between consecutive requests.

        public SpeakerIdentificationServiceClient EnrollmentClient { get; private set; }

        public List<User> UserProfiles {get; private set; }

        public DatabaseController DBController { get; private set; }

        public string EnrollmentLocale { get; private set; }


        /// <summary>
        /// Creates a new user profile for a User in the DiScribe database.
        /// 
        /// Also creates a corresponding profile with the Azure Speaker Recognition
        /// endpoint and returns the GUID for that profile on success.
        /// 
        ///
        /// </summary>
        /// <param name="client"></param>
        /// <param name="locale"></param>
        /// <returns>Created profile GUID or GUID {00000000-0000-0000-0000-000000000000} on fail</returns>
        public async Task<Guid> CreateUserProfile(UserParams userParams)
        {
            var taskComplete = new TaskCompletionSource<Guid>();
            Task<CreateProfileResponse> profileTask = null;
            Guid failGuid = new Guid();

            try
            { 
                profileTask = EnrollmentClient.CreateProfileAsync(EnrollmentLocale);
                await profileTask;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine("Error creating user profile with Azure Speaker Recognition endpoint\n" + ex.InnerException.Message);
                taskComplete.SetResult(failGuid);
                return failGuid;
            }


            
            /*Attempt to Create user profile in DB and add to list of user profiles */
            User registeredUser = DBController.CreateUser(userParams);

            if (registeredUser == null)
            {
                 taskComplete.SetResult(failGuid);
                 return failGuid;
            }

                
            UserProfiles.Add(registeredUser);                         //Add profile to list of profiles managed by this instance
            taskComplete.SetResult(profileTask.Result.ProfileId);
            return profileTask.Result.ProfileId;

        }



        /// <summary>
        /// Async check if a profile is regsitered for the email address.
        /// Returns true if so, false otherwise.
        ///  
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<User> CheckProfileExists(string email)
        {

            var taskComplete = new TaskCompletionSource<User>();
               
            /*Try to load a user with this email */
            User registeredUser = DBController.LoadUser(email);
               
            if (registeredUser == null)
            {
               return null;
            }

            taskComplete.SetResult(registeredUser);
            return registeredUser;
            
            
        }



        /// <summary>
        /// Delete a profile from the Azure Speaker Recognition endpoint and delete
        /// the matching record in the DiScribe database.
        /// </summary>
        /// <param name="email"></param>
        /// <returns>True on success, false on fail</Boolean></returns>
        public async Task<Boolean> DeleteProfile(string email)
        {

            var taskComplete = new TaskCompletionSource<Boolean>();

            var user = await CheckProfileExists(email);

            if (user == null)
            {
                taskComplete.SetResult(false);
                return false;
            }
            
            /*Delete profile Azure Spekaer Recognition endpoint */
            try
            {
                await EnrollmentClient.DeleteProfileAsync(user.ProfileGUID);
            } catch(Exception ex)
            {
                Console.Error.WriteLine($"Unable delete user from Azure Speaker Recognition endpoint. Continuing with DB profile delete {ex.Message}");
            }

            /*Delete user profile from DiScribe database */
            Boolean dbDelete = user.Delete();

            taskComplete.SetResult(dbDelete);
            return dbDelete;

        }




        /// <summary>
        /// Function which registers voice profiles with the Azure Speaker Recognition API
        /// for each DiScribe user present in UserProfiles.
        /// 
        /// In the case where users have existing profiles on the Azure endpoint, these
        /// profiles are reused. 
        /// 
        /// Voice samples for each user are enrolled with their profile. 
        /// <see cref=""/>
        /// </summary>
        /// <param name="speakerIDKey"></param>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        private async Task EnrollVoiceProfiles()
        {
           
            /*Ensure Speaker Recognition Service profiles for each User exist with the Azure endpoint. 
             * Create any profiles that do not exist */
            await ConfirmProfiles();

            /*Attempt to add a voice enrollment to each profile. If number of enrollments is exceeded,
             * enrollments will be cleared and another attempt will be made to add the enrollment */
            await EnrollVoiceSamples();

        }




        


        /// <summary>
        /// Recreates the Azure Speaker Recognition API profile for an existing user.
        /// Used when API profile experiece.
        /// 
        /// Also updates the profile GUID for the corresponding record in the DiScribe database.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        private async Task<Guid> RefreshAPIProfile(User user, string locale = "en-us")
        {

            var taskComplete = new TaskCompletionSource<Guid>();
            Task<CreateProfileResponse> profileTask = null;


            try
            {
                profileTask = EnrollmentClient.CreateProfileAsync(locale);
                await profileTask;

                user.ProfileGUID = profileTask.Result.ProfileId;
                                
            } catch (AggregateException ex)
            {
                Console.Error.WriteLine("Error creating user profile with Azure Speaker Recognition endpoint\n" + ex.InnerException.Message);
                var failGUID = new Guid();
                taskComplete.SetResult(failGUID);
                return failGUID;
            }

            /*Synchronize object with DiScribe database. Note that if this operation fails, it is not fatal.
             *The db record will not be consistent with the User object in memory, but application
             * execution will continue normally otherwise regardless. */
            try
            {
                user.Update();                                                    
            } catch(Exception ex)
            {
                Console.Error.WriteLine("Unable to update User record in database.");
            }
             
            
            taskComplete.SetResult(profileTask.Result.ProfileId);
            return user.ProfileGUID;
            

        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task ConfirmProfiles()
        {
            Profile[] existingProfiles = null;
            try
            {
                /*First check that all profiles in the voiceprint objects actually exist*/
                existingProfiles = await EnrollmentClient.GetProfilesAsync();
            }
            catch (Exception ex)
            {
                existingProfiles = null;
                Console.Error.WriteLine(">\tFetching profiles failed. Executing fallback to recreate profiles.");
            }

            
            if (existingProfiles != null)
            {
                for (int i = 0; i < UserProfiles.Count; i++)
                {
                    Boolean profileExists = false;
                    int j = 0;
                    while (!profileExists && j < existingProfiles.Length)
                    {
                        /*Check that the profile is in a usable state and that
                         * it matches with the GUID of this voiceprint */
                        if (existingProfiles[j].EnrollmentStatus != EnrollmentStatus.Unknown && 
                            UserProfiles[i].ProfileGUID == existingProfiles[j].ProfileId)
                        {
                            profileExists = true;
                        }
                        else
                            j++;
                    }
                    

                    /*Create an Azure profile if the profile doesn't actually exist. Also change the
                     * profile ID in the voiceprint object to the new ID*/
                    if (!profileExists)
                    {
                        await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                        var profileCreateTask = RefreshAPIProfile(UserProfiles[i]);
                        await profileCreateTask;
                        UserProfiles[i].ProfileGUID = profileCreateTask.Result;
                    }
                }//End-for
            }//End-if

            /*Fallback for when profiles could not be fetched from through Speaker API.*/
            else
            {
                foreach (var curVoiceprint in UserProfiles)
                {
                    await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                    var profileCreateTask = RefreshAPIProfile(curVoiceprint);
                    await profileCreateTask;
                    curVoiceprint.ProfileGUID = profileCreateTask.Result;
                }

            } //End else



        }    

        
    




        private async Task EnrollVoiceSamples()
        {
            var taskTuples = new List<Tuple<User, Task<OperationLocation>>>();
            List<Task<OperationLocation>> enrollmentTasks = new List<Task<OperationLocation>>();

            /*Start enrollment tasks for all user voiceprints. Use enrollmentTaskTuples
             * to give access to all info associated with each task later. Also add task to
               enrollmentTasks for purpose of awaiting each task.*/
            for (int i = 0; i < UserProfiles.Count; i++)
            {
                await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);                          //Do not exceed max requests per second.

                var curTask = EnrollmentClient.EnrollAsync(UserProfiles[i].AudioStream, UserProfiles[i].ProfileGUID, true);
                taskTuples.Add(new Tuple<User, Task<OperationLocation>>(UserProfiles[i], curTask));
                enrollmentTasks.Add(curTask);
            }

            await Task.WhenAll(enrollmentTasks.ToArray());                                   //Await all enrollment tasks to complete.


            /*Confirm that enrollment was successful for all the profiles
            associated with the enrollment tasks in enrollmentOps. */
            for (int i = 0; i < taskTuples.Count; i++)
            {
                Boolean done = false;
                Boolean reAttempt = true;
                /*Keep checking this enrollment for successful completion. If fail,
                 * then attempt enrollment again */
                do
                {
                    await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);

                    var enrollmentCheck = EnrollmentClient.CheckEnrollmentStatusAsync(taskTuples[i].Item2.Result);
                    await enrollmentCheck;

                    var reqStatus = enrollmentCheck.Result.Status;


                    /*If second request fails, do not try again */
                    if (reqStatus == Status.Failed && !reAttempt)
                    {
                        done = true;
                    }

                    /*If enrollment has failed, try one more time by first resetting enrollments
                     * for this profile and then performing enrollment task again. */
                    else if (reqStatus == Status.Failed)
                    {
                        reAttempt = false;                                          //Do not attempt again
                        await EnrollmentClient.ResetEnrollmentsAsync(taskTuples[i].Item1.ProfileGUID);

                        taskTuples[i].Item1.AudioStream.Position = 0;              //Ensure audio stream is at beginning.
                        var reEnrollmentTask = EnrollmentClient.EnrollAsync(taskTuples[i].Item1.AudioStream, taskTuples[i].Item1.ProfileGUID, true);

                        /*Replace curElem with an element representing the new enrollment task*/
                        taskTuples[i] = new Tuple<User, Task<OperationLocation>>(taskTuples[i].Item1, reEnrollmentTask);
                    }

                    /*Check that that result is ready and that profile is enrolled */
                    else if (!done)
                    {
                        var enrollmentResult = enrollmentCheck.Result.ProcessingResult;

                        if (enrollmentResult != null && enrollmentResult.EnrollmentStatus == EnrollmentStatus.Enrolled)
                        {
                            done = true;
                        }
                    }

                } while (!done);

            }



        }




    }
}
