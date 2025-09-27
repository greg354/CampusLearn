using Microsoft.AspNetCore.Mvc;
using CampusLearnPlatform.Models.ViewModels;

namespace CampusLearnPlatform.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Redirect to dashboard (frontend only)
                return RedirectToAction("Index", "Dashboard");
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View("StudentRegister");
        }

        [HttpPost]
        public IActionResult Register(StudentRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["SuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }
            return View("StudentRegister", model);
        }


        public IActionResult ForgotPassword()
        {
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }

        public IActionResult TutorRegister()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TutorRegister(TutorRegisterViewModel model)
        {
            // For now, just redirects to login with success message (frontend only)
            // Later we can add database saving like the student registration

            if (ModelState.IsValid)
            {
                TempData["SuccessMessage"] = "Tutor registration successful! Please log in with your new account.";
                return RedirectToAction("Login");
            }

            // If validation fails, stay on the page and show errors
            return View(model);
        }
    }
}