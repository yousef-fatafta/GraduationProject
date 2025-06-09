namespace Onlink.Models
{
    public class EmployeeJob
    {
        public int EmployeeJobId { get; set; }

        // Foreign keys
        public int EmployeeId { get; set; }
        public int EmployerId { get; set; }
        public int JobApplicationId { get; set; }

        // Additional join table properties
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        public string? CoverLetter { get; set; }

        // Navigation properties
        public Employee? Employee { get; set; } 
        public Employer? Employer{ get; set; } 
        public JobApplication? JobApplication { get; set; }
    }
}
