using System.ComponentModel.DataAnnotations;

namespace Onlink.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "First Name Must Be Added")]
        [StringLength(24, MinimumLength = 3, ErrorMessage = "Name At least 3 Chars")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name Must Be Added")]
        [StringLength(24, MinimumLength = 3, ErrorMessage = "Name At least 3 Chars")]
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Email Is Required")]
        public string Email { get; set; } = string.Empty;

        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone Not Valid")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }

        // Navigation properties
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public CheckInfo? CheckInfo { get; set; }
        public ICollection<EmployeeJob> JobApplications { get; set; } = new List<EmployeeJob>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();

        // User relationship
        public int UserId { get; set; }
        public User? User { get; set; }
        
      
    }
}
