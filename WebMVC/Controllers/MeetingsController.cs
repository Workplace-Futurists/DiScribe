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
using System.Globalization;

namespace DiScribe.WebMVC.Controllers
{
    public class MeetingsController : Controller
    {
        private readonly ApplicationMeetingClass _amc;

        public MeetingsController(ApplicationMeetingClass amc)
        {
            _amc = amc;
        }
        //public IActionResult Index()
        //{
        //    return View();
        //}

        public IActionResult Index(DateTime earliestDate, DateTime latestDate, string subject = "")
        {
            if (earliestDate == DateTime.MinValue)
            {
                earliestDate = DateTime.Today.AddDays(-10);
            }
            if (latestDate == DateTime.MinValue)
            {
                latestDate = DateTime.Today.AddDays(10);
            }
            if ((!String.IsNullOrEmpty(subject)) && (!String.IsNullOrWhiteSpace(subject)))
            {
                ViewBag.Subject = subject;
                ViewBag.EarliestDate = earliestDate.ToString("yyyy-MM-dd");
                ViewBag.LatestDate = latestDate.ToString("yyyy-MM-dd");
                return View(_amc.Meetings.Where(x => x.MeetingStartDateTime.Date >= earliestDate.Date && x.MeetingStartDateTime.Date <= latestDate.Date && x.MeetingSubject.Contains(subject)).ToList());
            }
            else
            {
                ViewBag.Subject = "";
                ViewBag.EarliestDate = earliestDate.ToString("yyyy-MM-dd");
                ViewBag.LatestDate = latestDate.ToString("yyyy-MM-dd");
                return View(_amc.Meetings.Where(x => x.MeetingStartDateTime.Date >= earliestDate.Date && x.MeetingStartDateTime.Date <= latestDate.Date).ToList());
                //return View(_amc.Meetings.ToList());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MeetingClass mc, string Participants, string ParticiNames, string HostName, string HostEmail)
        {
            WebexHostInfo meetingHost = new WebexHostInfo("kengqiangmk@gmail.com", "Cs319_APP", "kengqiangmk", "companykm.my");

            List<string> emails = new List<string>(Participants.Split(','));
            List<string> names = new List<string>(ParticiNames.Split(','));
            string startDateTimeStr = mc.MeetingStartDateTime.ToString("MM/dd/yyyy HH:mm:ss");
            string endDateTimeStr = mc.MeetingEndDateTime.ToString("MM/dd/yyyy HH:mm:ss");
            Int64 duration = (Int64)(mc.MeetingEndDateTime - mc.MeetingStartDateTime).TotalMinutes;
            emails.Add(HostEmail);
            names.Add(HostName);
            Microsoft.Graph.EmailAddress delegateEmailAddress = new Microsoft.Graph.EmailAddress();
            delegateEmailAddress.Name = HostName;
            delegateEmailAddress.Address = HostEmail;
            var meetingInfo = MeetingController.CreateWebexMeeting(mc.MeetingSubject, names, emails, mc.MeetingStartDateTime, duration.ToString(), meetingHost, delegateEmailAddress);
            mc.WebExID = meetingInfo.AccessCode;
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

            //_amc.Add(mc);
            //_amc.SaveChanges();
            ViewBag.message = "The Meeting " + mc.MeetingSubject + " Is Saved Successfully!";
            return View();
        }
    }
}