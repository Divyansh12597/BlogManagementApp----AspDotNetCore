using System.ComponentModel.DataAnnotations;

namespace BlogManagementApp.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [ EmailAddress, Required]
        public string Email { get; set; }
        public ICollection<BlogPost> BlogPosts { get; set; }
    }
}
