using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onlink.Data;
using Onlink.Models;
using Onlink.Services;
using System.Security.Claims;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace Onlink.Controllers
{
    public class CVController : Controller
    {
        private readonly DataContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly OpenAiService _openAi;

        public CVController(DataContext db, IWebHostEnvironment env, OpenAiService openAi)
        {
            _db = db;
            _env = env;
            _openAi = openAi;
        }


       
        [HttpGet]
        public async Task<IActionResult> SuggestedJobs()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userType = User.FindFirst("UserType")?.Value;

            if (userIdClaim == null || userType != "Employee")
                return RedirectToAction("AccessDenied", "Accounts");

            int userId = int.Parse(userIdClaim.Value);

            var employee = await _db.Employee.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
                return RedirectToAction("AccessDenied", "Accounts");

            var resume = await _db.Resume.FirstOrDefaultAsync(r => r.EmployeeId == employee.EmployeeId);
            if (resume == null)
            {
                ViewBag.Message = "You need to upload a resume first.";
                return View("SuggestedJobs", new List<(Job, string)>());
            }

            string resumeText = $"{resume.Summary} {resume.Experience} {resume.Education} {resume.Skills}";

            var jobs = await _db.Jobs.ToListAsync();
            var results = new List<(Job job, string decision)>();

            foreach (var job in jobs.Take(50)) // اختبر فقط أول 3 وظائف مؤقتاً
            {
                string jobText = $"{job.JobName} {job.JobDescription} {job.JobSalary} {job.SubmitSessionDueDate}";
                string prompt = $"Is the following resume suitable for the job description?\n\nResume:\n{resumeText}\n\nJob Description:\n{jobText}\n\nPlease respond with only one of the following: Suitable, Not Suitable, or Partially Suitable.";

                try
                {
                    string aiResponse = await _openAi.AskGPT(prompt);

                    if (!string.IsNullOrWhiteSpace(aiResponse) &&
                        (aiResponse.Contains("Suitable") || aiResponse.Contains("Partialy")))
                    {
                        results.Add((job, aiResponse));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error from OpenAI for job {job.JobName}: {ex.Message}");
                }
            }

            return View("SuggestedJobs", results);
        }


      
    }
}
