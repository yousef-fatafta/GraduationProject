using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.ComponentModel.DataAnnotations;

namespace Onlink.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        [Compare("PasswordHash")]
        public string ConfirmPasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string UserType { get; set; } = string.Empty; // "Employee", "Employer", "Company"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Employee? Employee { get; set; }
        public Employer? Employer { get; set; }
        public string? ProfilePicturePath { get; set; }
        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? GitHubUrl { get; set; }

    }
}
