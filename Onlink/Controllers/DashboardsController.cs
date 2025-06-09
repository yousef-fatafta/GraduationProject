using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Onlink.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Onlink.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Onlink.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;

        public DashboardsController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> CreateJob()
        {
            ViewBag.EmployerId = new SelectList(await _context.Employer.ToListAsync(), "EmployerId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(Job job)
        {
            if (!ModelState.IsValid)
                return View(job);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized("User not logged in.");

            var employer = await _context.Employer.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employer == null)
                return BadRequest("Employer not found.");

            job.EmployerId = employer.EmployerId;
            job.CreatedAt = DateTime.UtcNow;

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            return RedirectToAction("Posts", "Dashboards");
        }



        // GET: Show the form to create a post
        [HttpGet]
        public IActionResult CreatePost()
        {
            return View(new Post());
        }

        // POST: Save the post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(Post model, IFormFile? MediaFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Text))
            {
                ModelState.AddModelError("Text", "Post text is required.");
                return View(model);
            }

            if (user.UserType == "Employee")
            {
                var employee = await _context.Employee.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null) return BadRequest();

                model.EmployeeId = employee.EmployeeId;
                model.EmployerId = null;
            }
            else if (user.UserType == "Employer")
            {
                var employer = await _context.Employer.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employer == null) return BadRequest();

                model.EmployerId = employer.EmployerId;
                model.EmployeeId = null;
            }

            // handle media
            if (MediaFile != null && MediaFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "posts");
                Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid() + Path.GetExtension(MediaFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await MediaFile.CopyToAsync(stream);
                }

                model.MediaUrl = "/uploads/posts/" + fileName;
                model.MediaType = MediaFile.ContentType.StartsWith("video") ? MediaType.Video : MediaType.Image;
            }
            else
            {
                model.MediaUrl = string.Empty;
                model.MediaType = MediaType.None;
            }

            model.CreatedAt = DateTime.UtcNow;
            TempData["PostMessage"] = "A New Post Created.";
            _context.Post.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Posts));
        }

        // GET: Display all posts
        [HttpGet]
        public async Task<IActionResult> Posts()
        {
            var posts = await _context.Post
                .Include(p => p.Employee)
                .Include(p => p.Employer)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        [HttpPost]
        public async Task<IActionResult> LikePost(int id)
        {
            // Get logged-in user ID
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            // Check if user already liked the post
            bool alreadyLiked = await _context.PostLikes
                .AnyAsync(pl => pl.PostId == id && pl.UserId == userId);

            if (alreadyLiked)
            {
                return Json(new { success = false, message = "You already liked this post." });
            }

            // Add like record
            _context.PostLikes.Add(new PostLike
            {
                PostId = id,
                UserId = userId
            });

            // Increase post's like count
            var post = await _context.Post.FindAsync(id);
            if (post == null) return NotFound();

            post.LikeCount++;

            await _context.SaveChangesAsync();

            return Json(new { success = true, likeCount = post.LikeCount });
        }

        [HttpGet]
        public async Task<IActionResult> Jobs(string sort = "")
        {
            var jobsQuery = _context.Jobs.Include(j => j.Employer).AsQueryable();

            if (sort == "salary_desc")
                jobsQuery = jobsQuery.OrderByDescending(j => j.JobSalary);
            else if (sort == "salary_asc")
                jobsQuery = jobsQuery.OrderBy(j => j.JobSalary);

            var jobs = await jobsQuery.ToListAsync();

            // Get logged-in user type
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                ViewBag.UserType = user?.UserType;
            }

            ViewBag.Sort = sort;
            return View(jobs);
        }

        [HttpGet]
        public async Task<IActionResult> MyPosts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.Include(u => u.Employee).Include(u => u.Employer).FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound();

            List<Post> posts;

            if (user.UserType == "Employee")
            {
                posts = await _context.Post
                    .Where(p => p.EmployeeId == user.Employee.EmployeeId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                posts = await _context.Post
                    .Where(p => p.EmployerId == user.Employer.EmployerId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }

            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> EditPost(int id)
        {
            var post = await _context.Post.FindAsync(id);
            if (post == null) return NotFound();

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Post post, IFormFile? MediaFile)
        {
            if (!ModelState.IsValid)
                return View(post);

            var existing = await _context.Post.FindAsync(post.PostId);
            if (existing == null) return NotFound();

            existing.ActivityType = post.ActivityType;
            existing.Privacy = post.Privacy;
            existing.Text = post.Text;

            // If a new media file is uploaded
            if (MediaFile != null && MediaFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "posts");
                Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(MediaFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await MediaFile.CopyToAsync(stream);
                }

                existing.MediaUrl = "/uploads/posts/" + fileName;
                existing.MediaType = MediaFile.ContentType.StartsWith("video") ? MediaType.Video : MediaType.Image;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyPosts));
        }

        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            // ✅ 1. Get current logged-in user ID
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // ✅ 2. Get the logged-in employer
            var employer = await _context.Employer.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employer == null)
                return Forbid(); // Not an employer

            // ✅ 3. Find the job
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
                return NotFound();

            // ✅ 4. Verify ownership
            if (job.EmployerId != employer.EmployerId)
                return Forbid(); // Trying to access someone else's job

            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(Job job)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var employer = await _context.Employer.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employer == null)
                return Forbid();

            var existing = await _context.Jobs.FindAsync(job.JobId);
            if (existing == null)
                return NotFound();

            if (existing.EmployerId != employer.EmployerId)
                return Forbid();

            // ✅ Update allowed fields
            existing.JobName = job.JobName;
            existing.JobDescription = job.JobDescription;
            existing.JobSalary = job.JobSalary;
            existing.SubmitSessionDueDate = job.SubmitSessionDueDate;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyJobs));
        }


        [HttpGet]
        public async Task<IActionResult> MyJobs()
        {
            // Get current logged-in user ID (replace with your auth logic)
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var employer = await _context.Employer.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employer == null)
            {
                return RedirectToAction("AccessDenied", "Accounts"); // Or show "Not authorized"
            }

            var jobs = await _context.Jobs
                .Where(j => j.EmployerId == employer.EmployerId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyJob(int jobId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var employee = _context.Employee.FirstOrDefault(e => e.UserId == userId);
            if (employee == null)
            {
                return Json(new { success = false, message = "Only employees can apply for jobs." });
            }

            var job = _context.Jobs.FirstOrDefault(j => j.JobId == jobId);
            if (job == null)
            {
                return Json(new { success = false, message = "Job not found." });
            }

            var existing = _context.EmployeeJob
                .Any(ej => ej.EmployeeId == employee.EmployeeId && ej.JobApplication.JobId == jobId);

            if (existing)
            {
                return Json(new { success = false, message = "You have already applied for this job." });
            }

            var jobApp = new JobApplication
            {
                JobId = jobId,
                EmployeeId = employee.EmployeeId,
                AppliedAt = DateTime.UtcNow
            };

            _context.JobApplication.Add(jobApp);
            _context.SaveChanges();

            _context.EmployeeJob.Add(new EmployeeJob
            {
                EmployeeId = employee.EmployeeId,
                EmployerId = job.EmployerId,
                JobApplicationId = jobApp.JobApplicationId,
                AppliedDate = DateTime.UtcNow
            });

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult Notifications(string message, string type = "info")
        {
            return PartialView("~/Views/Dashboards/Notifications.cshtml", (message, type));
        }

    }
}