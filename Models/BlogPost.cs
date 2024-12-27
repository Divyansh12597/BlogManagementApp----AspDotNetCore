using System.ComponentModel.DataAnnotations;

namespace BlogManagementApp.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }
        public string Body { get; set; }
        public string? FeaturedImage { get; set; }

        [StringLength(150)]
        public string? MetaTitle { get; set; }

        [StringLength(300)]
        public string? MetaDescription { get; set; }

        [StringLength(250)]
        public string? MetaKeywords { get; set; }

        [StringLength(200)]
        public string? Slug { get; set; }
        public int Views { get; set; } = 0;

        public DateTime PublishedOn { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedOn { get; set; } = DateTime.UtcNow;
        public int AuthorId { get; set; }//Foreign Key
        public Author? Author { get; set; }
        
        public int CategoryId { get; set; }//Foreign Key
        public Category? Category { get; set; }

        public ICollection<Comment>? Comments { get; set; }



    }
}
