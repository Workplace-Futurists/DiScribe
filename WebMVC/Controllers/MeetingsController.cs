using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiScribe.WebMVC.Models;
using DiScribe.Meeting;
using EmailAddress = SendGrid.Helpers.Mail.EmailAddress;
using SendGrid.Helpers.Mail;
using System.Diagnostics;
using DiScribe.Email;

namespace DiScribe.WebMVC.Controllers
{
    public class MeetingsController : Controller
    {
        private readonly ApplicationMeetingClass _amc;

        public MeetingsController(ApplicationMeetingClass amc)
        {
            _amc = amc;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MeetingClass mc, string Participants, string ParticiNames)
        {
            WebexHostInfo meetingHost = new WebexHostInfo("kengqiangmk@gmail.com", "Cs319_APP", "kengqiangmk", "companykm.my");

            List<string> emails = new List<string>(Participants.Split(','));
            List<string> names = new List<string>(ParticiNames.Split(','));

            var sendGridEmails = EmailListener.parseEmailList(emails);


            Int64 duration = (Int64)(mc.MeetingEndDate - mc.MeetingStartDate).TotalMinutes;
            var access_code = MeetingController.CreateWebexMeeting(mc.MeetingSubject, names, emails, mc.MeetingStartDate, duration.ToString(), meetingHost);
            /*try
            {
                var attendees = MeetingController.GetAttendeeEmails(access_code, meetingHost);
                //MeetingController.SendEmailsToAnyUnregisteredUsers(attendees);
                //EmailSender.SendEmailForStartURL(attendees, access_code, mc.MeetingSubject);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex);
                Trace.WriteLine(ex);
            }

            try
            {
                // TODO warning this part is hardcoded
                //SchedulerController.ScheduleTask(access_code, mc.MeetingStartDate, "Main.exe", @"C:\cs319_main");
            }
            catch (Exception ex)
            {

            }*/

            //MeetingControllers.EmailController.Initialize();
            //EmailController.SendEmailForVoiceRegistration(emailAddresses);
            _amc.Add(mc);
            _amc.SaveChanges();
            ViewBag.message = "The Meeting " + mc.MeetingSubject + " Is Saved Successfully!";
            return View();
        }
    }
}