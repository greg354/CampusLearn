using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Data;
using System.Security.Cryptography;
using System.Text;

namespace CampusLearnPlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<AccountController> _logger;

 Profile_
        // Session keys
        private const string SK_StudentId = "CurrentStudentId";
        private const string SK_StudentName = "CurrentStudentName";
        private const string SK_StudentEmail = "CurrentStudentEmail";
=======
 main

        public AccountController(CampusLearnDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========= LOGIN =========
        [HttpGet]
        public IActionResult Login()
        {
            // If already "logged in" via session, go straight to dashboard
            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SK_StudentId)))
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var email = model.Email?.Trim().ToLower();
                var hashed = HashPassword(model.Password);

                // Check Students table (the app currently uses Students for auth)
                var student = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == email && s.PasswordHash == hashed);

                if (student == null)
                {
                    // Invalid creds
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(model); // Password field will clear; others stay for convenience
                }

                // Set session values for layout & auth simulation
                HttpContext.Session.SetString(SK_StudentId, student.Id.ToString());
                HttpContext.Session.SetString(SK_StudentName, student.Name ?? email ?? "Student");
                HttpContext.Session.SetString(SK_StudentEmail, student.Email);

                TempData["SuccessMessage"] = $"Welcome back, {student.Name ?? email}!";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                TempData["ErrorMessage"] = "Login failed due to a server error. Please try again.";
                return View(model);
            }
        }

        // ========= LOGOUT =========
        [HttpGet]
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.Clear();
                TempData["InfoMessage"] = "You have been signed out.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                TempData["ErrorMessage"] = "Could not complete logout, but your session was cleared.";
            }

            return RedirectToAction("Login");
        }

        // ========= REGISTER (unchanged EXCEPT it already redirects to Login) =========
        [HttpGet]
        public IActionResult Register()
        {
            return View("StudentRegister");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StudentRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View("StudentRegister", model);

            try
            {
                // Additional validation
                if (!await IsValidRegistrationAsync(model))
                    return View("StudentRegister", model);

                // Check if student already exists
                var email = model.Email.ToLower().Trim();
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == email);

                if (existingStudent != null)
                {
                    ModelState.AddModelError("Email", "A student with this email address already exists.");
                    return View("StudentRegister", model);
                }

                var student = new Models.Users.Student
                {
                    Id = Guid.NewGuid(),
                    Name = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
                    Email = email,
                    PasswordHash = HashPassword(model.Password),
                    ProfileInfo = $"Student in {model.Course}, {model.Year}"
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New student registered: {StudentId}, {Email}", student.Id, student.Email);

                // Important: Redirect to Login with success message
                TempData["SuccessMessage"] = "Registration successful! Please sign in with your new account.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during student registration: {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred while creating your account. Please try again.");
                return View("StudentRegister", model);
            }
        }

        // ========= HELPERS =========

        private async Task<bool> IsValidRegistrationAsync(Models.ViewModels.StudentRegisterViewModel model)
        {
            // Belgium Campus email validation
            if (!model.Email.EndsWith("@student.belgiumcampus.ac.za", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "Email must be a valid Belgium Campus student email (@student.belgiumcampus.ac.za)");
                return false;
            }

            // Password validation
            if (!IsPasswordValid(model.Password))
            {
                ModelState.AddModelError("Password", "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");
                return false;
            }

            // Terms acceptance
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "You must accept the terms and conditions");
                return false;
            }

            return await Task.FromResult(true);
        }

        private bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CampusLearnSalt2024"));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
