using System.ComponentModel.DataAnnotations;

namespace Onlink.Models
{
    public class Job
    {

        public int JobId { get; set; }

        [Required(ErrorMessage = "Name Must Be Added")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Name At least 2 Chars")]

        public string JobName { get; set; }

        public string? JobDescription { get; set; }

        [Range(290, 5000)]
        public double JobSalary { get; set; }

        public DateTime SubmitSessionDueDate { get; set; }

        public int EmployerId { get; set; }

        public JobApplication? JobApplication { get; set; }
        public Employer? Employer { get; set; }
        public DateTime CreatedAt { get; internal set; }
    }
}
