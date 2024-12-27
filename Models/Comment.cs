//using System.ComponentModel.DataAnnotations;
//using BlogManagementApp.ValidationAttributes;

//namespace BlogManagementApp.Models
//{
//    public class Comment
//    {
//        public int Id { get; set; }

//        [Required, StringLength(100)]
//        public string Name { get; set; }

//        [Required, EmailAddress]
//        public string Email { get; set; }

//        [Required, StringLength(1000)]
//        [CommentTextValidator] //Custom Validator
//        public string Text { get; set; }
//        public DateTime PostedOn { get; set; }
//        public int BlogPostId { get; set; } //Foreign Key
//        public BlogPost? BlogPost { get; set; }
//    }
//}

using BlogManagementApp.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace BlogManagementApp.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(1000)]
        [CommentTextValidator] //We will create this Custom Validator
        public string Text { get; set; }

        public DateTime PostedOn { get; set; }

        // Foreign Key
        public int BlogPostId { get; set; }

        // Navigation Property
        public BlogPost? BlogPost { get; set; }
    }
}
