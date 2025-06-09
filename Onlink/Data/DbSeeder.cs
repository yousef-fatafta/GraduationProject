using Onlink.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Onlink.Data
{
    public static class DbSeeder
    {
        public static void Seed(DataContext db)
        {
            if (db.Users.Any()) return;

            var random = new Random();
            var skillsPool = new[] { "C#", "Java", "SQL", "React", "Python", "Node.js", "Angular", "Docker", "Kubernetes", "ASP.NET" };
            var jobTitles = new[] { "Backend Developer", "Frontend Engineer", "Data Analyst", "DevOps Specialist", "Mobile Developer" };
            var companies = new[] { "TechNova", "CodeLab", "InnoSoft", "DevHouse", "ByteWorks" };

            var users = new List<User>();
            var employees = new List<Employee>();
            var employers = new List<Employer>();
            var resumes = new List<Resume>();
            var posts = new List<Post>();
            var jobs = new List<Job>();
            var jobApps = new List<JobApplication>();

            for (int i = 1; i <= 2000; i++)
            {
                var isEmployee = i <= 1000;
                var userType = isEmployee ? "Employee" : "Employer";
                var user = new User
                {
                    UserName = $"user{i}",
                    Email = $"user{i}@example.com",
                    PasswordHash = "123456",
                    ConfirmPasswordHash = "123456",
                    UserType = userType,
                    CreatedAt = DateTime.UtcNow
                };
                users.Add(user);

                if (isEmployee)
                {
                    var employee = new Employee
                    {
                        FirstName = $"First{i}",
                        LastName = $"Last{i}",
                        Email = user.Email,
                        PhoneNumber = $"079{i:D7}",
                        User = user,
                        Bio = "Motivated software engineer."
                    };
                    employees.Add(employee);

                    resumes.Add(new Resume
                    {
                        FullName = $"{employee.FirstName} {employee.LastName}",
                        Email = employee.Email,
                        Phone = employee.PhoneNumber,
                        Summary = "Driven and passionate developer.",
                        Education = "BSc Computer Science",
                        Experience = "2+ years in development",
                        Skills = string.Join(", ", skillsPool.OrderBy(x => random.Next()).Take(3)),
                        Employee = employee
                    });

                
                    if (i <= 500)
                    {
                        posts.Add(new Post
                        {
                            CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                            Employee = employee,
                            ActivityType = ActivityType.Post,
                            MediaUrl = string.Empty,
                            MediaType = MediaType.None,
                            Privacy = PostPrivacy.Public,
                            LikeCount = random.Next(0, 50),
                            CommentCount = random.Next(0, 20),
                            ShareCount = random.Next(0, 10)
                        });
                    }
                }
                else
                {
                    var employer = new Employer
                    {
                        Name = companies[random.Next(companies.Length)] + $" {i}",
                        Email = user.Email,
                        PhoneNumber = $"078{i:D7}",
                        User = user
                    };
                    employers.Add(employer);

                    if (i > 1500 && i <= 2000)
                    {
                        var job = new Job
                        {
                            JobName = jobTitles[random.Next(jobTitles.Length)],
                            JobDescription = $"We are looking for someone skilled in {string.Join(", ", skillsPool.OrderBy(x => random.Next()).Take(3))}.",
                            JobSalary = random.Next(500, 4000),
                            SubmitSessionDueDate = DateTime.UtcNow.AddDays(random.Next(10, 30)),
                            CreatedAt = DateTime.UtcNow,
                            Employer = employer
                        };
                        jobs.Add(job);
                        jobApps.Add(new JobApplication { Job = job });
                    }
                }
            }

            db.Users.AddRange(users);
            db.Employee.AddRange(employees);
            db.Employer.AddRange(employers);
            db.Resume.AddRange(resumes);
            db.Post.AddRange(posts);
            db.Jobs.AddRange(jobs);
            db.JobApplication.AddRange(jobApps);

            db.SaveChanges();
        }
    }
}
