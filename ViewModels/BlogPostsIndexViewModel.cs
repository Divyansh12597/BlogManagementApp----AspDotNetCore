using BlogManagementApp.Models;

namespace BlogManagementApp.ViewModels
{
    public class BlogPostsIndexViewModel
    {
        public List<BlogPost>? Posts { get; set; }

        // Pagination Properties
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }

        // Search Filter Properties
        public string? SearchTitle { get; set; }
        public int? SearchCategoryId { get; set; }


    }
}
