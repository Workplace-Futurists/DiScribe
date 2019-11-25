using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiScribeWebMVC.Models
{
    public class UserClass
    {
        
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Int64 UserID { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "Please enter First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Please enter Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage ="Please enter email.")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        public byte[] AudioSample { get; set; }
        //public Object AudioSample { get; set; }

        [Display(Name = "Audio Recording")]
        [Required(ErrorMessage = "Please record audio.")]
        [NotMapped]
        public string AudioSample_str { get; set; }
    }
}
