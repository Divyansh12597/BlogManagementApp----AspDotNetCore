using System.ComponentModel.DataAnnotations;

namespace BlogManagementApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string EmailId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string CPassword { get; set; }
    }
}
