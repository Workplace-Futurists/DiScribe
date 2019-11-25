using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;
using Transcriber.Audio;
using System.Collections.Generic;
using DatabaseController.Data;
using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;
using NAudio.Wave;
using DatabaseController;

namespace Main.Test
{
    public class RegistrationTest
    {
        private static readonly FileInfo TestRecording = new FileInfo(@"../../../../Record/test_meeting.wav");

        /// <summary>
        /// Method for test purposes to get voice samples from a WAV file
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public static List<User> TestRegistration()
        {
            Console.WriteLine(">\tGenerating Test Voice prints...");

            /*Set result with List<Voiceprint> containing both voiceprint objects */
            /*Create registration controller where thare are no profiles loaded initially */
            RegistrationController regController =
                RegistrationController.BuildController(new List<string>());

            /*Try to register some profiles */


            /*Offsets identifying times */
            ulong user1StartOffset = 1 * 1000;
            ulong user1EndOffset = 49 * 1000;

            ulong user2StartOffset = 51 * 1000;
            ulong user2EndOffset = 100 * 1000;

            ulong user3StartOffset = 101 * 1000;
            ulong user3EndOffset = 148 * 1000;

            ulong user4StartOffset = 151 * 1000;
            ulong user4EndOffset = 198 * 1000;

            AudioFileSplitter splitter = new AudioFileSplitter(TestRecording);

            var user1Audio = splitter.WriteWavToStream(user1StartOffset, user1EndOffset);
            var user2Audio = splitter.WriteWavToStream(user2StartOffset, user2EndOffset);
            var user3Audio = splitter.WriteWavToStream(user3StartOffset, user3EndOffset);
            var user4Audio = splitter.WriteWavToStream(user4StartOffset, user4EndOffset);

            var userParams = new List<UserParams>{
                 new UserParams(user1Audio.GetBuffer(), "Brian", "Kernighan", "B.Kernighan@Example.com"),
                 new UserParams(user2Audio.GetBuffer(), "Janelle", "Shane", "J.Shane@Example.com"),
                 new UserParams(user3Audio.GetBuffer(), "Nick", "Smith", "N.Smith@Example.com"),
                 new UserParams(user4Audio.GetBuffer(), "Patrick", "Shyu", "P.Shyu@Example.com")
            };

            /*- Check if any user voice profiles already exist (should be false)
             *- Register the profiles
             *- Check exist again (should be true)
             */
            Console.WriteLine(">\tChecking voice profile enrollment...");
            foreach (var curParam in userParams)
            {
                /*Check if this profile exists */
                Task<User> profileCheck = regController.CheckProfileExists(curParam.Email);
                profileCheck.Wait();
                User curUser = profileCheck.Result;

                Console.WriteLine($">\tProfile for {curParam.Email} " + (curUser == null ? "does not exist" : "exists"));

                if (curUser == null)
                {
                    Console.WriteLine($">\tTesting CreateProfile() for {curParam.Email}");
                    Task<Guid> profileCreateTask = regController.CreateUserProfile(curParam);
                    profileCreateTask.Wait();

                    Guid outcome = profileCreateTask.Result;

                    /*If default guid of all 0's is returned, then profile creation failed */
                    if (outcome.ToString() == new Guid().ToString())
                    {
                        Console.WriteLine($">\tProfile creation failed for {curParam.Email}");
                    }
                    /*Else profile created successfuly */
                    else
                    {
                        Console.WriteLine($">\tProfile creation successful for {curParam.Email} with Azure profile Guid {outcome}");
                    }
                }

                /*Test DeleteProfile() method */
                Console.WriteLine($">\tTesting DeleteProfile() for {curParam.Email}");
                Task<Boolean> profileDelete = regController.DeleteProfile(curParam.Email);
                profileDelete.Wait();

                /*Check if this profile exists after delete*/
                Task<User> verifyDelete = regController.CheckProfileExists(curParam.Email);
                profileCheck.Wait();
                User deletedUser = profileCheck.Result;

                /*User was deleted, as profile check returns a null result */
                if (deletedUser == null)
                {
                    Console.WriteLine($">\tProfile for {curParam.Email} successfully deleted.");
                    Console.WriteLine($"Recreating profile for { curParam.Email} after delete");
                    regController.CreateUserProfile(curParam).Wait();
                }
                else
                {
                    Console.WriteLine($">\tDelete failed for {curParam.Email}.");
                }
            }
            return regController.UserProfiles;                             //Return the created User profiles created by the RegistrationController
        }
    }
}
