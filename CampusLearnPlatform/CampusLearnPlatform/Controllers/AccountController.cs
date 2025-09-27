using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Users;
using System.Security.Cryptography;
using System.Text;

namespace CampusLearnPlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(CampusLearnDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

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
        public async Task<IActionResult> Register(StudentRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("StudentRegister", model);
            }

            try
            {
                // Additional validation
                if (!await IsValidRegistrationAsync(model))
                {
                    return View("StudentRegister", model);
                }

                // Check if student already exists
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == model.Email.ToLower());

                if (existingStudent != null)
                {
                    ModelState.AddModelError("Email", "A student with this email address already exists.");
                    return View("StudentRegister", model);
                }

                // Create new student
                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    Name = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
                    Email = model.Email.ToLower().Trim(),
                    PasswordHash = HashPassword(model.Password),
                    ProfileInfo = $"Student in {model.Course}, {model.Year}"
                };

                // Save to database
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New student registered: {StudentId}, {Email}", student.Id, student.Email);

                TempData["SuccessMessage"] = "Registration successful! Please log in with your new account.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during student registration: {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred while creating your account. Please try again.");
                return View("StudentRegister", model);
            }
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }

//Tutor page to login if no problems
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
            
            
//Validation on student register (backend and DB?)
        private async Task<bool> IsValidRegistrationAsync(StudentRegisterViewModel model)
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

            return true;
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

        private int ParseAcademicYear(string yearString)
        {
            return yearString switch
            {
                "First Year" => 1,
                "Second Year" => 2,
                "Third Year" => 3,
                "Fourth Year" => 4,
                _ => 1
            };
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