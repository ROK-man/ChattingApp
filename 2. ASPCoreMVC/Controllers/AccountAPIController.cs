using ASPCoreMVC.Models;
using ASPCoreMVC.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ASPCoreMVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountAPIController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountAPIController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SigninViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserID, model.Password, isPersistent: false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var code = Guid.NewGuid().ToString();
                    code = "hello, world!!";
                    return Ok(new { Message = "로그인 성공", Code = code });
                }
                return Unauthorized(new { Message = "아이디 또는 비밀번호가 잘못되었습니다." });
            }
            return BadRequest(ModelState);
        }
    }
}
