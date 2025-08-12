using Microsoft.AspNetCore.Identity;

namespace ASPCoreMVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Nickname {  get; set; }
        public string? UserID {  get; set; }
    }
}
