using System.ComponentModel.DataAnnotations;

namespace BlogManagementApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public ICollection<BlogPost> BlogPosts { get; set; }
    }
}
