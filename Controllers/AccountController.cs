using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcApp.Data; // your context namespace
using MyMvcApp.Models; // your model namespace

namespace MyMvcApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public AccountController(AppDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        // GET: Account/SignUp
        public IActionResult SignUp()
        {
            return View();
        }

        // POST: Account/SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(User user, string passwordConfirm)
        {
            user.CreatedAt = DateTime.Now;
            user.IsActive = true;

            // Check for duplicate email
            if (_db.User.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
            }

            // Check password match
            if (user.PasswordHash != passwordConfirm)
            {
                ModelState.AddModelError("PasswordHash", "Passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // ✅ Hash and save
            user.PasswordHash = HashPassword(user.PasswordHash ?? string.Empty);
            _db.User.Add(user);
            _db.SaveChanges();

            // ✅ Save to session
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("UserName", user.UserName ?? "");
            HttpContext.Session.SetString("Email", user.Email ?? "");
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("Address", user.Address ?? "");

            TempData["Success"] = $"Account created successfully! Welcome, {user.UserName}";
            return RedirectToAction("Login");
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string password, User model)
        {
            var user = _db.User.FirstOrDefault(u => u.UserName == model.UserName);

            if (user != null)
            {
                string hashed = HashPassword(password);

                if (user.PasswordHash == hashed)
                {
                    if (user.IsActive)
                    {
                        HttpContext.Session.SetInt32("UserID", user.UserID);
                        HttpContext.Session.SetString("UserName", user.UserName ?? "");
                        HttpContext.Session.SetString("Email", user.Email ?? "");
                        HttpContext.Session.SetString("FullName", user.FullName ?? "");
                        HttpContext.Session.SetString("Address", user.Address ?? "");
                        HttpContext.Session.SetString("ProfileImage",
                            string.IsNullOrEmpty(user.ProfileImagePath)
                                ? "/images/default-avatar.png"
                                : user.ProfileImagePath);

                        TempData["Welcome"] = $"Welcome, {user.FullName}!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Error = "Your account is inactive. Please contact support.";
                        return View(model);
                    }
                }
            }

            ViewBag.Error = "Invalid username or password.";
            return View(model);
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Password Hashing
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        [HttpPost]
        public JsonResult UploadProfileImage(IFormFile file, int userId)
        {
            if (userId == 0)
                userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            if (file != null && file.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string uploadDir = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string fullPath = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                var user = _db.User.Find(userId);
                if (user != null)
                {
                    user.ProfileImagePath = "/images/" + fileName;
                    _db.SaveChanges();

                    HttpContext.Session.SetString("ProfileImage", user.ProfileImagePath);
                    return Json(new { success = true, imageUrl = user.ProfileImagePath });
                }
            }

            return Json(new { success = false });
        }


        // GET: Account/Setting
        public IActionResult Setting()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return NotFound();

            var user = _db.User.Find(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Setting(User model, IFormFile ProfileImage)
        {
            if (!ModelState.IsValid) return View(model);

            var userInDb = _db.User.Find(model.UserID);
            if (userInDb == null) return NotFound();

            userInDb.FullName = model.FullName;
            userInDb.Email = model.Email;
            userInDb.UserName = model.UserName;
            userInDb.IsActive = model.IsActive;
            userInDb.Address = model.Address;

            // Handle Profile Image Upload
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(ProfileImage.FileName);
                string uploadDir = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string fullPath = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    ProfileImage.CopyTo(stream);
                }

                userInDb.ProfileImagePath = "/images/" + fileName;
            }

            _db.Update(userInDb);
            _db.SaveChanges();

            HttpContext.Session.SetString("ProfileImage", userInDb.ProfileImagePath ?? "");

            return RedirectToAction("Index");
        }
    }
}
