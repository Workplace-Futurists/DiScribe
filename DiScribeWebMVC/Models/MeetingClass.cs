using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiScribeWebMVC.Models
{
    public class MeetingClass
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Int64 MeetingID { get; set; }

        [Required(ErrorMessage = "Please enter meeting subject")]
        [Display(Name = "Meeting Subject")]
        public string MeetingSubject { get; set; }

        [Display(Name = "Meeting Minutes")]
        public string MeetingMinutes { get; set; }

        [Display(Name = "Meeting Start Date Time")]
        [Required(ErrorMessage = "Please enter meeting start date and time")]
        public DateTime MeetingStartDate { get; set; }

        [Display(Name = "Meeting End Date Time")]
        [Required(ErrorMessage = "Please enter meeting end date and time")]
        public DateTime MeetingEndDate { get; set; }

        [Display(Name = "Meeting Start Time")]
        public DateTime MeetingStartTime { get; set; }

        [Display(Name = "Meeting End Time")]
        public DateTime MeetingEndTime { get; set; }

        [Display(Name = "Meeting File Location")]
        public string MeetingFileLocation { get; set; }

        [Display(Name = "WebExID")]
        public string WebExID { get; set; }
        
        [NotMapped]
        [Required(ErrorMessage = "Please enter participants emails separated by comma")]
        public string Participants { get; set; }
    }
}
