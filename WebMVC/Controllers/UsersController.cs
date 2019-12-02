using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiScribe.WebMVC.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace DiScribe.WebMVC.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationUserClass _auc;

        public UsersController(ApplicationUserClass auc)
        {
            _auc = auc;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create(string email)
        {
            if ((email != null) && (email.Length > 0))
            {
                ViewBag.curEmail = email;
            }
            else
            {
                ViewBag.curEmail = "";
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(UserClass uc, IFormFile AudioSample)
        //public IActionResult Create(UserClass uc, Object AudioSample)
        public IActionResult Create(UserClass uc, string AudioSample_str)
        {
            //byte[] converted = ObjectToByteArray(AudioSample);
            byte[] converted = Convert.FromBase64String(AudioSample_str);
            //byte[] converted = (byte[])AudioSample;
            uc.AudioSample = converted;
            _auc.Add(uc);
            _auc.SaveChanges();
            ViewBag.message = "The User " + uc.FirstName + " Is Saved Successfully!";
            return View();
        }

        private byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
    }
}