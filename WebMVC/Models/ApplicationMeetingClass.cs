using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DiScribe.WebMVC.Models
{
    public class ApplicationMeetingClass : DbContext
    {
        public ApplicationMeetingClass(DbContextOptions<ApplicationMeetingClass> options) : base(options)
        {

        }

        public DbSet<MeetingClass> Meetings { get; set; }
    }
}
