using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DiScribe.Scheduler.Windows;
using Microsoft.EntityFrameworkCore;
using DiScribe.WebMVC.Models;
using DiScribe.Meeting;
using EmailAddress = SendGrid.Helpers.Mail.EmailAddress;
using SendGrid.Helpers.Mail;
using System.Diagnostics;

namespace DiScribeWebMVC.Controllers
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
            List<string> emails = new List<string>(Participants.Split(','));
            List<string> names = new List<string>(ParticiNames.Split(','));
            string startDateTimeStr = mc.MeetingStartDate.ToString("MM/dd/yyyy HH:mm:ss");
            string endDateTimeStr = mc.MeetingEndDate.ToString("MM/dd/yyyy HH:mm:ss");
            Int64 duration = (Int64)(mc.MeetingEndDate - mc.MeetingStartDate).TotalMinutes;
            var access_code = MeetingController.CreateWebExMeeting(mc.MeetingSubject, names, emails, startDateTimeStr, duration.ToString());
            try
            {
                var attendees = MeetingController.GetAttendeeEmails(access_code);
                MeetingController.SendEmailsToAnyUnregisteredUsers(attendees);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex);
                Trace.WriteLine(ex);
            }

            try
            {
                // TODO warning this part is hardcoded
                SchedulerController.ScheduleTask(access_code, mc.MeetingStartDate, "Main.exe", @"C:\cs319_main");
            }
            catch (Exception ex)
            {

            }

            //MeetingControllers.EmailController.Initialize();
            //EmailController.SendEmailForVoiceRegistration(emailAddresses);
            _amc.Add(mc);
            _amc.SaveChanges();
            ViewBag.message = "The Meeting " + mc.MeetingSubject + " Is Saved Successfully!";
            return View();
        }
    }
}