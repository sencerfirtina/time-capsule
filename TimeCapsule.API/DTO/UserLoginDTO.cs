using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.DTO
{
    public class UserLoginDTO
    {
        [Required(ErrorMessage = "The E-mail address field cannot be left blank!")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address!")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "The Password field cannot be left blank!")]
        public required string Password { get; set; }
    }
}