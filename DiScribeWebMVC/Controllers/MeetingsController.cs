using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiScribeWebMVC.Models;

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
        public IActionResult Create(MeetingClass mc, string Participants)
        {
            string[] emails = Participants.Split(',');
            string startDateTimeStr = mc.MeetingStartDate.ToString("MM/dd/yyyy HH:mm:ss");
            string endDateTimeStr = mc.MeetingEndDate.ToString("MM/dd/yyyy HH:mm:ss");
            Int64 duration = (Int64)(mc.MeetingEndDate - mc.MeetingStartDate).TotalMilliseconds;
            _amc.Add(mc);
            _amc.SaveChanges();
            ViewBag.message = "The Meeting " + mc.MeetingSubject + " Is Saved Successfully!";
            return View();
        }
    }
}