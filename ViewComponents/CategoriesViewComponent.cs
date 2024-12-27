using BlogManagementApp.Data;
using BlogManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BlogManagementApp.ViewComponents
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly BlogManagementDBContext _context;
        public CategoriesViewComponent( BlogManagementDBContext context)
        {
            _context = context;
            
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<Category> categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            //View Location should be: Views/Shared/Components/Categories/Default.cshtml
            return View(categories);
        }
    }
}
