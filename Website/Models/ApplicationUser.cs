using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Website.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Nickname { get; set; }
    }
}
