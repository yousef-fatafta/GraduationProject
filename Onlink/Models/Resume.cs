using System.ComponentModel.DataAnnotations;

namespace Onlink.Models
{
    public class Resume
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name should be between 3 and 50 characters.")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public string? Summary { get; set; }
        public string? Education { get; set; }
        public string? Experience { get; set; }
        public string? Skills { get; set; }

        [Url]
        public string? LinkPath { get; set; }

        public int? EmployeeId { get; set; }
        public int? EmployerId { get; set; }

        public Employee? Employee { get; set; }
        public Employer? Employer { get; set; }
        public DateTime UploadDate { get; internal set; }
    }
}
