using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Data;
using System.Security.Cryptography;
using System.Text;
namespace CampusLearnPlatform.Controllers
{
    public class LoginController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(CampusLearnDbContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            TempData.Clear();

            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            _logger.LogInformation("Login attempt - Email: {Email}, ModelState Valid: {IsValid}",
               model.Email, ModelState.IsValid);

            if (!ModelState.IsValid)
            {
              
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation Error: {Error}", error.ErrorMessage);
                }
                return View("~/Views/Account/Login.cshtml");
            }

            try
            {
                string email = model.Email.ToLower().Trim();
                string hashedPassword = HashPassword(model.Password);

                
                if (email.EndsWith("@student.belgiumcampus.ac.za"))
                {
                    return await AuthenticateStudent(email, hashedPassword, model);
                }
              
                else if (email.EndsWith("@tutor.belgiumcampus.ac.za"))
                {
                    return await AuthenticateTutor(email, hashedPassword, model);
                }
                else
                {
                    ModelState.AddModelError("Email", "Please use a valid Belgium Campus student or tutor email address.");
                    _logger.LogWarning("Login attempt with invalid email domain: {Email}", email);
                    return View("~/Views/Account/Login.cshtml");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View("~/Views/Account/Login.cshtml");
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userType = HttpContext.Session.GetString("UserType");

            HttpContext.Session.Clear();

            _logger.LogInformation("{UserType} logged out: {Email}", userType ?? "User", userEmail ?? "Unknown");
            TempData["SuccessMessage"] = "You have been logged out successfully.";

            return RedirectToAction("~/Views/Account/Login.cshtml");
        }


        private async Task<IActionResult> AuthenticateStudent(string email, string hashedPassword, LoginViewModel model)
        {
            var student = await _context.Students.FirstOrDefaultAsync(x => x.Email.ToLower() == email);

            if(student == null)
            {
                ModelState.AddModelError("LoginError", " Invalid email or password.");
                _logger.LogWarning("Failed login attempt - student not found: {Email}", email);
                return View("~/Views/Account/Login.cshtml");
            }

            if(student.PasswordHash != hashedPassword)
            {
                ModelState.AddModelError("LoginError", "Invalid email or password enterd.");
                _logger.LogWarning("Failed login attempt - student not found: {Email}", email);
                return View("~/Views/Account/Login.cshtml");
            }

            HttpContext.Session.SetString("UserId", student.Id.ToString());
            HttpContext.Session.SetString("UserName", student.Name);
            HttpContext.Session.SetString("UserEmail", student.Email);
            HttpContext.Session.SetString("UserType", "student");

            if (!string.IsNullOrEmpty(student.ProfileInfo))
            {
                HttpContext.Session.SetString("ProfileInfo", student.ProfileInfo);
            }

            _logger.LogInformation("Student logged in successfully: {Email}", email);
            //TempData["SuccessMessage"] = $"Welcome back, {student.Name}!";

            return RedirectToAction("Index", "Dashboard");
        }


        private async Task<IActionResult> AuthenticateTutor(string email, string hashedPassword, LoginViewModel model)
        {
            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.Email.ToLower() == email);

            if (tutor == null)
            {
                ModelState.AddModelError("LoginError", "Invalid email or password.");
                _logger.LogWarning("Failed login attempt - tutor not found: {Email}", email);
                return View("~/Views/Account/Login.cshtml");
            }

            if (tutor.PasswordHash != hashedPassword)
            {
                ModelState.AddModelError("LoginError", "Invalid email or password.");
                _logger.LogWarning("Failed login attempt - incorrect password for tutor: {Email}", email);
                return View("~/Views/Account/Login.cshtml");
            }

            
            HttpContext.Session.SetString("UserId", tutor.Id.ToString());
            HttpContext.Session.SetString("UserName", tutor.Name);
            HttpContext.Session.SetString("UserEmail", tutor.Email);
            HttpContext.Session.SetString("UserType", "tutor");

            if (!string.IsNullOrEmpty(tutor.Experience))
            {
                HttpContext.Session.SetString("Experience", tutor.Experience);
            }

            _logger.LogInformation("Tutor logged in successfully: {Email}", email);
            //TempData["SuccessMessage"] = $"Welcome back, {tutor.Name}!";

            return RedirectToAction("Index", "Dashboard");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CampusLearnSalt2024"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

