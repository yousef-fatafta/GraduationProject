using System.ComponentModel.DataAnnotations;

namespace Onlink.Models
{
    public class Employer
    {
        public int EmployerId { get; set; }

        [Required(ErrorMessage = "First Name Must Be Added")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Name At least 2 Chars")]
        public string Name { get; set; }

        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Email Is Required")]
        public string Email { get; set; }

        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone Not Valid")]
        [Phone]
        public string? PhoneNumber { get; set; }

        public IEnumerable<Job>? Jobs { get; set; }
        public IEnumerable<Post>? Posts { get; set; }
        public ICollection<Resume> Resume { get; set; } = new List<Resume>();


        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
