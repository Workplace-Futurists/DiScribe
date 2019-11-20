
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using transcriber.Data;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;

namespace transcriber.TranscribeAgent
{
    class SpeakerRegistration
    {
        public SpeakerRegistration(string speakerIDKeySub, List<Voiceprint> voiceprints, 
            string enrollmentLocale = "en-us", int apiInterval = SPEAKER_RECOGNITION_API_INTERVAL)
        {
            /*Create REST client for enrolling users */
            EnrollmentClient = new SpeakerIdentificationServiceClient(speakerIDKeySub);

            Voiceprints = voiceprints;
        }

        public const int SPEAKER_RECOGNITION_API_INTERVAL = 3000;                               //Min time between consecutive requests.

        public SpeakerIdentificationServiceClient EnrollmentClient { get; private set; }

        public List<Voiceprint> Voiceprints {get; private set; }





        /// <summary>
        /// Function which enrolls 2 users for testing purposes. In final system, enrollment will
        /// be done by users.
        /// </summary>
        /// <param name="speakerIDKey"></param>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public async Task EnrollUsers()
        {
           
            /*Ensure profiles associated with each voiceprint exist. Create any profiles that do not exist */
            await ConfirmProfiles();

            /*Attempt to add a voice enrollment to each profile. If number of enrollments is exceeded,
             * enrollments will be cleared and another attempt will be made to add the enrollment */
            await EnrollVoiceSamples();

        }




        /// <summary>
        /// Creates a new user profile for a User and returns the GUID for that profile.
        /// In the full system, this method should include a check to find out
        /// if the user is already registered in persistent storage (i.e. database).
        /// </summary>
        /// <param name="client"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        public async Task<Guid> CreateUserProfile(User user, string locale = "en-us")
        {
            var taskComplete = new TaskCompletionSource<Guid>();

            var profileTask = EnrollmentClient.CreateProfileAsync(locale);
            await profileTask;

            taskComplete.SetResult(profileTask.Result.ProfileId);

            return profileTask.Result.ProfileId;
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
                for (int i = 0; i < Voiceprints.Count; i++)
                {
                    Boolean profileExists = false;
                    int j = 0;
                    while (!profileExists && j < existingProfiles.Length)
                    {
                        /*Check that the profile is in a usable state and that
                         * it matches with the GUID of this voiceprint */
                        if (existingProfiles[j].EnrollmentStatus != EnrollmentStatus.Unknown && 
                            Voiceprints[i].UserGUID == existingProfiles[j].ProfileId)
                        {
                            profileExists = true;
                        }
                        else
                            j++;
                    }
                    

                    /*Create a profile if the profile doesn't actually exist. Also change the
                     * profile ID in the voiceprint object to the new ID*/
                    if (!profileExists)
                    {
                        await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                        var profileCreateTask = CreateUserProfile(Voiceprints[i].AssociatedUser);
                        await profileCreateTask;
                        Voiceprints[i].UserGUID = profileCreateTask.Result;
                    }
                }//End-for
            }//End-if

            /*Fallback for when profiles could not be fetched from through Speaker API.*/
            else
            {
                foreach (var curVoiceprint in Voiceprints)
                {
                    await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);
                    var profileCreateTask = CreateUserProfile(curVoiceprint.AssociatedUser);
                    await profileCreateTask;
                    curVoiceprint.UserGUID = profileCreateTask.Result;
                }

            } //End else



        }    



    




        private async Task EnrollVoiceSamples()
        {
            var taskTuples = new List<Tuple<Voiceprint, Task<OperationLocation>>>();
            List<Task<OperationLocation>> enrollmentTasks = new List<Task<OperationLocation>>();

            /*Start enrollment tasks for all user voiceprints. Use enrollmentTaskTuples
             * to give access to all info associated with each task later. Also add task to
               enrollmentTasks for purpose of awaiting each task.*/
            for (int i = 0; i < Voiceprints.Count; i++)
            {
                await Task.Delay(SPEAKER_RECOGNITION_API_INTERVAL);                          //Do not exceed max requests per second.

                var curTask = EnrollmentClient.EnrollAsync(Voiceprints[i].AudioStream, Voiceprints[i].UserGUID, true);
                taskTuples.Add(new Tuple<Voiceprint, Task<OperationLocation>>(Voiceprints[i], curTask));
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
                        await EnrollmentClient.ResetEnrollmentsAsync(taskTuples[i].Item1.UserGUID);

                        taskTuples[i].Item1.AudioStream.Position = 0;              //Ensure audio stream is at beginning.
                        var reEnrollmentTask = EnrollmentClient.EnrollAsync(taskTuples[i].Item1.AudioStream, taskTuples[i].Item1.UserGUID, true);

                        /*Replace curElem with an element representing the new enrollment task*/
                        taskTuples[i] = new Tuple<Voiceprint, Task<OperationLocation>>(taskTuples[i].Item1, reEnrollmentTask);
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
