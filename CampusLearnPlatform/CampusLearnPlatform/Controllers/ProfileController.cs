using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Users;
using CampusLearnPlatform.Models.ViewModels;

namespace CampusLearnPlatform.Controllers
{
    public class ProfileController : Controller
    {
        private readonly CampusLearnDbContext _db;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(CampusLearnDbContext db, ILogger<ProfileController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // /Profile/My
        // Shows the profile for the currently logged-in student (via Session)
        public async Task<IActionResult> My()
        {
            var idStr = HttpContext.Session.GetString("CurrentStudentId");
            if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var studentId))
            {
                TempData["Error"] = "Please sign in first.";
                return RedirectToAction("Login", "Account");
            }

            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null)
            {
                TempData["Error"] = "Your account could not be found.";
                return RedirectToAction("Login", "Account");
            }

            var vm = new ProfileViewModel
            {
                Id = student.Id,
                Name = student.Name ?? "",
                Email = student.Email ?? "",
                ProfileInfo = student.ProfileInfo ?? "",
                // If you later store picture path in DB, bind it here:
                ProfilePictureUrl = "/images/default-profile.png",
                IsMe = true
            };

            return View("ViewProfile", vm);
        }

        // /Profile/View/{id}
        // View another student's profile (public view)
        [HttpGet("/Profile/View/{id:guid}")]
        public async Task<IActionResult> View(Guid id)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return NotFound();

            var myIdStr = HttpContext.Session.GetString("CurrentStudentId");
            bool isMe = (Guid.TryParse(myIdStr, out var me) && me == id);

            var vm = new ProfileViewModel
            {
                Id = student.Id,
                Name = student.Name ?? "",
                Email = student.Email ?? "",
                ProfileInfo = student.ProfileInfo ?? "",
                ProfilePictureUrl = "/images/default-profile.png",
                IsMe = isMe
            };

            return View("ViewProfile", vm);
        }

        // GET: /Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var idStr = HttpContext.Session.GetString("CurrentStudentId");
            if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var studentId))
            {
                TempData["Error"] = "Please sign in first.";
                return RedirectToAction("Login", "Account");
            }

            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null) return NotFound();

            var vm = new ProfileViewModel
            {
                Id = student.Id,
                Name = student.Name ?? "",
                Email = student.Email ?? "",
                ProfileInfo = student.ProfileInfo ?? "",
                ProfilePictureUrl = "/images/default-profile.png",
                IsMe = true
            };

            return View("EditProfile", vm);
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            var idStr = HttpContext.Session.GetString("CurrentStudentId");
            if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var studentId))
            {
                TempData["Error"] = "Please sign in first.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid) return View("EditProfile", model);

            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null) return NotFound();

            // Update editable fields
            student.Name = model.Name?.Trim();
            student.ProfileInfo = model.ProfileInfo?.Trim();

            await _db.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(My));   // back to your own profile
        }
    }
}
