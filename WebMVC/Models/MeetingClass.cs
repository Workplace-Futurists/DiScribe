using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;


namespace DiScribe.WebMVC.Models
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
        public DateTime MeetingStartDateTime { get; set; }

        [Display(Name = "Meeting End Date Time")]
        [Required(ErrorMessage = "Please enter meeting end date and time")]
        public DateTime MeetingEndDateTime { get; set; }

        [Display(Name = "Meeting File Location")]
        public string MeetingFileLocation { get; set; }

        [Display(Name = "WebExID")]
        public string WebExID { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Please enter participants emails separated by comma")]
        public string Participants { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Please enter participants names separated by comma")]
        public string ParticiNames { get; set; }

        [Display(Name = "Meeting Host Name")]
        [Required(ErrorMessage = "Please enter meeting host name")]
        [NotMapped]
        public string HostName { get; set; }

        [Display(Name = "Meeting Host Email")]
        [Required(ErrorMessage = "Please enter meeting host email")]
        [NotMapped]
        public string HostEmail { get; set; }
    }
}
