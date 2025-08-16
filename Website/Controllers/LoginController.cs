using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Website.ViewModels;
using Microsoft.EntityFrameworkCore;
using Website.Models;

namespace Website.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var exists = await _userManager.Users
                    .AnyAsync(u => u.UserName == model.UserID);
                if (exists)
                {
                    ModelState.AddModelError("UserID", "이미 사용 중인 ID입니다.");
                    return View(model);
                }
                exists = await _userManager.Users
                    .AnyAsync(u => u.Nickname == model.Nickname);
                if (exists)
                {
                    ModelState.AddModelError("Nickname", "이미 사용 중인 이름입니다.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.UserID,
                    Nickname = model.Nickname,
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Signout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Signin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signin(SigninViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserID, model.Password, false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "아이디 또는 비밀번호가 잘못되었습니다.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Agree()
        {
            return View();
        }
    }
}
