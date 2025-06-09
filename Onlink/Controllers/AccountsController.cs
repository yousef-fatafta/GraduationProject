using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onlink.Data;
using Onlink.Models;
using System.Security.Claims;

[Authorize]
public class AccountsController : Controller
{
    private readonly DataContext _db;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(DataContext db, ILogger<AccountsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claim, out int userId))
            return userId;

        throw new UnauthorizedAccessException("User is not authenticated.");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == model.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("UserType", user.UserType)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToAction("Posts", "Dashboards");
    }

    [HttpGet]
    public IActionResult Logout() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();

        var user = await _db.Users
            .Include(u => u.Employee).ThenInclude(e => e.Certificates)
            .Include(u => u.Employee).ThenInclude(e => e.Resumes)
            .Include(u => u.Employee).ThenInclude(e => e.JobApplications)
            .Include(u => u.Employer).ThenInclude(e => e.Jobs)
            .Include(u => u.Employer).ThenInclude(e => e.Posts)
            .Include(u => u.Employer).ThenInclude(e => e.Resume)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return Forbid();

        ViewData["UserType"] = user.UserType;
        ViewData["ProfilePicturePath"] = user.ProfilePicturePath ?? "/images/default-user.png";

        return View(user);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _db.Users.AnyAsync(u => u.UserName == model.Username))
        {
            ModelState.AddModelError("Username", "Username is already taken.");
            return View(model);
        }

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email is already registered.");
            return View(model);
        }

        var user = new User
        {
            UserName = model.Username,
            Email = model.Email,
            UserType = model.UserType,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            ConfirmPasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (model.UserType == "Employee")
        {
            _db.Employee.Add(new Employee
            {
                FirstName = model.Username.Split(' ')[0],
                LastName = model.Username.Split(' ').Length > 1 ? model.Username.Split(' ')[1] : "",
                Email = model.Email,
                UserId = user.UserId
            });
        }
        else if (model.UserType == "Employer")
        {
            _db.Employer.Add(new Employer
            {
                Name = model.Username,
                Email = model.Email,
                UserId = user.UserId
            });
        }

        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("UserType", user.UserType)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Posts", "Dashboards");
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users
            .Include(u => u.Employee)
            .Include(u => u.Employer)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return NotFound();

        ViewData["UserType"] = user.UserType;
        return View(user);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
   public async Task<IActionResult> Edit(
    User model,
    IFormFile? ProfilePictureFile,
    List<IFormFile>? ResumeFiles,
    List<IFormFile>? CertificateFiles)
    {
        var userId = GetCurrentUserId();

        var user = await _db.Users
            .Include(u => u.Employee).ThenInclude(e => e.Certificates)
            .Include(u => u.Employee).ThenInclude(e => e.Resumes)
            .Include(u => u.Employer).ThenInclude(e => e.Resume)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return NotFound();

        // Update base fields
        user.UserName = model.UserName;
        user.Email = model.Email;
        user.FacebookUrl = model.FacebookUrl;
        user.TwitterUrl = model.TwitterUrl;
        user.GitHubUrl = model.GitHubUrl;

        // ===== EMPLOYEE SECTION =====
        if (user.UserType == "Employee" && model.Employee != null)
        {
            // Update employee info
            user.Employee.FirstName = model.Employee.FirstName;
            user.Employee.LastName = model.Employee.LastName;
            user.Employee.Bio = model.Employee.Bio;
            user.Employee.PhoneNumber = model.Employee.PhoneNumber;

            // Handle Resume uploads
            if (ResumeFiles != null && ResumeFiles.Count > 0)
            {
                var uploadsPath = Path.Combine("wwwroot", "uploads", "resumes");
                Directory.CreateDirectory(uploadsPath);

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                foreach (var file in ResumeFiles)
                {
                    if (file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("ResumeFiles", "Only PDF, DOC, and DOCX files are allowed for resumes.");
                            return View(user);
                        }

                        if (file.Length > 5 * 1024 * 1024) // 5MB max
                        {
                            ModelState.AddModelError("ResumeFiles", "File size cannot exceed 5MB.");
                            return View(user);
                        }

                        var uniqueName = $"resume_{user.UserId}_{Guid.NewGuid():N}{extension}";
                        var filePath = Path.Combine(uploadsPath, uniqueName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        user.Employee.Resumes.Add(new Resume
                        {
                            FullName = Path.GetFileNameWithoutExtension(file.FileName),
                            Email = user.Email,
                            LinkPath = $"/uploads/resumes/{uniqueName}",
                            UploadDate = DateTime.Now,
                            EmployeeId = user.Employee.EmployeeId
                        });
                    }
                }
            }

            // Handle Certificate uploads
            if (CertificateFiles != null && CertificateFiles.Count > 0)
            {
                var uploadsPath = Path.Combine("wwwroot", "uploads", "certificates");
                Directory.CreateDirectory(uploadsPath);

                var allowedExtensions = new[] { ".pdf", ".jpg", ".png" };

                foreach (var file in CertificateFiles)
                {
                    if (file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("CertificateFiles", "Only PDF, JPG, and PNG files are allowed for certificates.");
                            return View(user);
                        }

                        if (file.Length > 5 * 1024 * 1024) // 5MB max
                        {
                            ModelState.AddModelError("CertificateFiles", "File size cannot exceed 5MB.");
                            return View(user);
                        }

                        var uniqueName = $"certificate_{user.UserId}_{Guid.NewGuid():N}{extension}";
                        var filePath = Path.Combine(uploadsPath, uniqueName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        user.Employee.Certificates.Add(new Certificate
                        {
                            CertificateName = Path.GetFileNameWithoutExtension(file.FileName),
                            CompanyRelatedToName = "Uploaded Certificate",
                            IssuedDate = DateTime.Now,
                            Description = "Uploaded by user",
                            EmployeeId = user.Employee.EmployeeId
                        });
                    }
                }
            }
        }

        // ===== EMPLOYER SECTION =====
        else if (user.UserType == "Employer" && user.Employer != null)
        {
            user.Employer.Name = model.Employer?.Name;
            user.Employer.PhoneNumber = model.Employer?.PhoneNumber;

            if (ResumeFiles != null)
            {
                var uploadsPath = Path.Combine("wwwroot", "uploads", "employer-resumes");
                Directory.CreateDirectory(uploadsPath);
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                foreach (var file in ResumeFiles)
                {
                    if (file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("ResumeFiles", "Only PDF, DOC, and DOCX are allowed.");
                            return View(user);
                        }

                        var fileName = $"employer_resume_{user.UserId}_{Guid.NewGuid():N}{extension}";
                        var path = Path.Combine(uploadsPath, fileName);
                        using (var stream = new FileStream(path, FileMode.Create))
                            await file.CopyToAsync(stream);

                        user.Employer.Resume ??= new List<Resume>();
                        user.Employer.Resume.Add(new Resume
                        {
                            FullName = Path.GetFileNameWithoutExtension(file.FileName),
                            Email = user.Email,
                            LinkPath = $"/uploads/employer-resumes/{fileName}",
                            UploadDate = DateTime.UtcNow,
                            EmployerId = user.Employer.EmployerId
                        });
                    }
                }
            }
        }

        // ===== PROFILE PICTURE =====
        if (ProfilePictureFile != null && ProfilePictureFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(ProfilePictureFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ProfilePictureFile", "Only JPG, JPEG, and PNG are allowed.");
                return View(user);
            }

            var uploadsPath = Path.Combine("wwwroot", "uploads", "profile_pictures");
            Directory.CreateDirectory(uploadsPath);
            var fileName = $"profile_{user.UserId}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await ProfilePictureFile.CopyToAsync(stream);

            user.ProfilePicturePath = $"/uploads/profile_pictures/{fileName}";
        }

        try
        {
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile.");
            TempData["ErrorMessage"] = $"Failed to save changes: {ex.Message}";
            return View(user);
        }

        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            return View(model);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        await _db.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Password changed. Please log in again.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Error() => View();
}
