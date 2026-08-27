using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.DTO
{
    public class UserRegisterDTO
    {
        [Required(ErrorMessage = "The E-mail address field cannot be left blank!")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address!")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "The Username field cannot be left blank!")]
        [MinLength(3,ErrorMessage = "Username must be at least 3 characters long!")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "The Password field cannot be left blank!")]
        [MinLength(6,ErrorMessage = "Password must be at least 6 characters long!")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.]).{8,}$",
        ErrorMessage = "Your password must be at least 8 characters long and include 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character!!")]
        public required string Password { get; set; }
    }
}