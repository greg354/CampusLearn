using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CampusLearnPlatform.Controllers
{
    public class ProfileController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(CampusLearnDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Profile/My
        public IActionResult My()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var student = _context.Students.FirstOrDefault(s => s.Id.ToString() == userId);
            if (student == null) return NotFound();

            var vm = new ProfileViewModel
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                ProfileInfo = student.ProfileInfo,
                ProfilePictureUrl = student.ProfilePictureUrl,
                IsMe = true
            };
            return View("ViewProfile", vm);
        }

        // GET: /Profile/Edit
        public IActionResult Edit()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var student = _context.Students.FirstOrDefault(s => s.Id.ToString() == userId);
            if (student == null) return NotFound();

            var vm = new ProfileViewModel
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                ProfileInfo = student.ProfileInfo,
                ProfilePictureUrl = student.ProfilePictureUrl,
                IsMe = true
            };
            return View("EditProfile", vm);
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == model.Id);
            if (student == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Name is required.");

            // Validate image if provided
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(ext))
                    ModelState.AddModelError(nameof(model.ProfileImage), "Unsupported file type.");

                if (model.ProfileImage.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError(nameof(model.ProfileImage), "File too large (max 2MB).");
            }

            if (!ModelState.IsValid)
                return View("EditProfile", model);

            // Update base fields
            student.Name = model.Name;
            student.ProfileInfo = model.ProfileInfo;

            // Save image if uploaded
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                var fileName = $"{student.Id:N}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsRoot, fileName);

                using (var fs = new FileStream(fullPath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fs);
                }

                // Delete previous file if exists
                if (!string.IsNullOrWhiteSpace(student.ProfilePictureUrl))
                {
                    var oldPhysical = Path.Combine(_env.WebRootPath,
                        student.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPhysical))
                        System.IO.File.Delete(oldPhysical);
                }

                student.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
            }

            _context.SaveChanges();

            // keep session display name current
            HttpContext.Session.SetString("UserName", student.Name);

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("My");
        }
    }
}
