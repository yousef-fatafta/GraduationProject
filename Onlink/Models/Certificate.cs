using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Onlink.Models
{
    public class Certificate
    {
        [Key]
        public int CertificateId { get; set; }

        [Required]
        [StringLength(50)]
        public string CertificateName { get; set; }

        [Required]
        [StringLength(50)]
        public string CompanyRelatedToName { get; set; }

        [Required]
        public DateTime IssuedDate { get; set; }

        [StringLength(200)]
        public string Description { get; set; } 

        public int EmployeeId { get; set; } 
       
   

        public Employee? Employee { get; set; }

      
        [StringLength(100)]

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
