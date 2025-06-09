namespace Onlink.Models
{
    public class CheckInfo
    {
        public int CheckInfoId { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
    }
}
