using Azure.Core;
using BlogManagementApp.Data;
using BlogManagementApp.Models;
using BlogManagementApp.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BlogManagementApp.Controllers
{
    public class BlogPostsController : Controller
    {
        private readonly BlogManagementDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public BlogPostsController(BlogManagementDBContext context, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;

        }

        public async Task<IActionResult> Index(string? searchTitle, int? searchCategoryID, int? pageNumber)
        {
            try
            {
                //1. Fetch PageSize from appsettings.json, default to 10 if not  set
                int pageSize = _configuration.GetValue<int?>("Pagination:PageSize") ?? 10;

                //2. Fetch Category for the Dropdown
                var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");

                //3. Initialize query
                var postsQuery = _context.BlogPosts
                    .Include(b => b.Author) //Eager Loading
                    .Include(b => b.Category) //Eager Loading
                    .AsQueryable(); // The query is built but not executed yet

                //4. Apply Title Fileter if provided
                if (!string.IsNullOrEmpty(searchTitle))
                {
                    postsQuery = postsQuery.Where(b => b.Title.Contains(searchTitle));
                }

                //5. Apply Category Filter if provided
                if (searchCategoryID.HasValue && searchCategoryID.Value != 0)
                {
                    postsQuery = postsQuery.Where(b => b.CategoryId == searchCategoryID.Value);

                }

                //6. Order by PublishedOn descending (recent first)
                postsQuery = postsQuery.OrderByDescending(b => b.PublishedOn);

                //7. Fetch Total Count for pagination
                int totalPosts = await postsQuery.CountAsync(); //Executes the query to get the total count

                //8. Calculate Total pages
                int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
                totalPages = totalPages < 1 ? 1 : totalPages; //Ensure at least one page

                //9. Ensure pageNumber is within valid range
                pageNumber = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;
                pageNumber = pageNumber > totalPages ? totalPages : pageNumber;

                //10. Fetch posts for the current page
                var posts = await postsQuery
                    .Skip((pageNumber.Value - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(); //Execute the query  to retrive only the records which required in the current page

                //11. Prepare ViewModel for Pagination
                var viewModel = new BlogPostsIndexViewModel
                {
                    Posts = posts,
                    CurrentPage = pageNumber.Value,
                    TotalPages = totalPages,
                    SearchTitle = searchTitle,
                    SearchCategoryId = searchCategoryID ?? 0,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unable to load blog posts. Please try again later.";
                return View("Error");
            }
        }
        //Blog Post by Slug
        [HttpGet]
        [Route("/blog/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                ViewBag.ErrorMessage = "Slug not provided.";
                return View("Error");
            }
            try
            {
                var blogPost = await _context.BlogPosts
                    .Include(b => b.Author)
                    .Include(b => b.Category)
                    .Include(b => b.Comments)
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (blogPost == null)
                {
                    ViewBag.ErrorMessage = "Blog post not found.";
                    return View("Error");
                }

                //Increment Views
                blogPost.Views = blogPost.Views + 1;
                await _context.SaveChangesAsync();

                //Set SEO meta tags
                ViewBag.MetaDescription = blogPost.MetaDescription;
                ViewBag.MetaKeywords = blogPost.MetaKeywords;
                ViewBag.Title = blogPost.MetaTitle ?? blogPost.Title;

                var viewModel = new BlogPostDetailsViewModel
                {
                    BlogPost = blogPost,
                    Comment = new Comment()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while loading the blog post details.";
                return View("Error");
            }
        }
        //GET: Categories/{id}/Posts
        [HttpGet("/categories/{id}/Posts")]
        public async Task<IActionResult> PostsByCategory(int id, int? pageNumber)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                ViewBag.ErrorMessage = "Invalid Category";
                return View("Error");
            }
            int pageSize = _configuration.GetValue<int?>("Pagination:PageSize") ?? 10;

            var PostQuery = _context.BlogPosts
                .Where(b => b.CategoryId == id)
                .Include(b => b.Author)
                .Include(b => b.Category)
                .OrderByDescending(b => b.PublishedOn)
                .AsQueryable();

            int totalPosts = await PostQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
            totalPages = totalPages < 1 ? 1 : totalPages;

            pageNumber = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;
            pageNumber = pageNumber > totalPages ? totalPages : pageNumber;

            var posts = await PostQuery
                .Skip((pageNumber.Value - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CategoryPostsViewModel
            {
                Posts = posts,
                CurrentPage = pageNumber.Value,
                TotalPages = totalPages,
                CategoryName = category.Name,
                CategoryId = category.Id
            };
            return View("CategoryPosts", viewModel);

        }

        //GET: BlogPosts/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                //Fetch authors and categories from dropdown
                ViewBag.Authors = await _context.Authors.ToListAsync();
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unable to load the create blog post form. Please try again later.";
                return View("Error");

            }
        }

        //POST: BlogPost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost blogPost, IFormFile? FeaturedImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle Image Upload (using a seprate method to avoid deuplication)
                    blogPost.FeaturedImage = await UploadFeaturedImageAsync(FeaturedImage) ?? blogPost.FeaturedImage;

                    //Generate or Validate the slug
                    blogPost.Slug = string.IsNullOrEmpty(blogPost.Slug)
                        ? await GenerateSlugAsync(blogPost.Title)
                        : blogPost.Slug;

                    //Ensure teh Slug is unique
                    if (await _context.BlogPosts.AnyAsync(b => b.Slug == blogPost.Slug))
                    {
                        ModelState.AddModelError("Slug", "The Slug must be unique.");
                    }
                    else
                    {
                        _context.Add(blogPost);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Blog post added successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occured while creating the blog post.";
                    return View("Error");
                }
            }
            //if validation fails, reload authors and categories for dropdown
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Authors = await _context.Authors.ToListAsync();
            return View(blogPost);
        }
        // GET: BlogPosts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                ViewBag.ErrorMessage = "Blog post ID is missing.";
                return View("Error");
            }
            try
            {
                var blogPost = await _context.BlogPosts.FindAsync(id);
                if (blogPost == null)
                {
                    ViewBag.ErrorMessage = "Blog post not found.";
                    return View("Error");
                }
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Authors = await _context.Authors.ToListAsync();
                return View(blogPost);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while loading the edit blog post form.";
                return View("Error");
            }
        }
        // POST: BlogPosts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost blogPost, IFormFile? FeaturedImage)
        {
            if (id != blogPost.Id)
            {
                return NotFound("Blog post ID mismatch.");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var existingPost = await _context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existingPost == null)
                    {
                        return NotFound("Blog post not found.");
                    }
                    if (FeaturedImage != null && FeaturedImage.Length > 0)
                    {
                        blogPost.FeaturedImage = await UploadFeaturedImageAsync(FeaturedImage);
                    }
                    else
                    {
                        //If you to remove the Featured Image, then don't set this
                        blogPost.FeaturedImage = existingPost.FeaturedImage;
                    }
                    // Generate or validate the slug
                    blogPost.Slug = string.IsNullOrEmpty(blogPost.Slug)
                        ? await GenerateSlugAsync(blogPost.Title)
                        : blogPost.Slug;
                    if (await _context.BlogPosts.AnyAsync(b => b.Slug == blogPost.Slug && b.Id != blogPost.Id))
                    {
                        ModelState.AddModelError("Slug", "The slug must be unique.");
                    }
                    else
                    {
                        _context.Update(blogPost);
                        await _context.SaveChangesAsync();
                        // Set success message
                        TempData["SuccessMessage"] = "Blog post updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while updating the blog post.";
                    return View("Error");
                }
            }
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Authors = await _context.Authors.ToListAsync();
            return View(blogPost);
        }

        // GET: BlogPosts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                ViewBag.ErrorMessage = "Blog post ID is missing.";
                return View("Error");
            }
            try
            {
                var blogPost = await _context.BlogPosts
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (blogPost == null)
                {
                    ViewBag.ErrorMessage = "Blog post not found.";
                    return View("Error");
                }
                return View(blogPost);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while loading the blog post for deletion.";
                return View("Error");
            }
        }

        // POST: BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var blogPost = await _context.BlogPosts.FindAsync(id);
                if (blogPost != null)
                {
                    _context.BlogPosts.Remove(blogPost);
                    await _context.SaveChangesAsync();
                    // Set success message
                    TempData["SuccessMessage"] = "Blog post deleted successfully.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while deleting the blog post.";
                return View("Error");
            }
        }
        private async Task<string> UploadFeaturedImageAsync(IFormFile featuredImage)
        {
            if (featuredImage != null && featuredImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + featuredImage.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await featuredImage.CopyToAsync(fileStream);
                }
                return "/uploads/" + uniqueFileName;
            }
            return null;
        }
        private async Task<string> GenerateSlugAsync(string title)
        {
            // Slug generation with regex
            var slug = System.Text.RegularExpressions.Regex.Replace(title.ToLowerInvariant(), @"\s+", "-").Trim();
            // Ensure slug is unique by appending numbers if necessary
            var uniqueSlug = slug;
            int counter = 1;
            while (await _context.BlogPosts.AnyAsync(b => b.Slug == uniqueSlug))
            {
                uniqueSlug = $"{slug}-{counter++}";
            }
            return uniqueSlug;
        }
    }
}

