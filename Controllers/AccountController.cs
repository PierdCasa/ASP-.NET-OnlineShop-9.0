using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Models;
using OnlineShop.ViewModels;

namespace OnlineShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
        {
            _users = users;
            _signIn = signIn;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel m, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = m.Email, Email = m.Email, FirstName = m.FirstName, LastName = m.LastName };
                var result = await _users.CreateAsync(user, m.Password);
                if (result.Succeeded)
                {
                    await _users.AddToRoleAsync(user, "User");
                    await _signIn.SignInAsync(user, false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
            }
            return View(m);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrEmpty(returnUrl))
                ViewBag.Message = "Pentru a continua, autentifică-te sau creează un cont.";
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel m, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var result = await _signIn.PasswordSignInAsync(m.Email, m.Password, m.RememberMe, false);
                if (result.Succeeded)
                    return LocalRedirect(returnUrl);
                ModelState.AddModelError("", "Email sau parola gresita.");
            }
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [Authorize, HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return NotFound();
            return View(new ProfileViewModel { Email = user.Email!, FirstName = user.FirstName, LastName = user.LastName });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel m)
        {
            if (!ModelState.IsValid) return View(m);
            var user = await _users.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = m.FirstName;
            user.LastName = m.LastName;
            var result = await _users.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Profil actualizat!";
                return RedirectToAction("Profile");
            }
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(m);
        }
    }
}
