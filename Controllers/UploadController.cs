using Microsoft.AspNetCore.Mvc;

namespace BlogManagementApp.Controllers
{
    [Route("file/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
        {
            _environment = environment;
        }

        //POST file/upload
        [HttpPost]
        [IgnoreAntiforgeryToken] // Disable CSRF for this endpoint
        public async Task<IActionResult> FileUpload(IFormFile upload)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                {
                    return BadRequest(new { error = new { message = "No file uploaded." } });
                }

                // Validate file type (allow only images)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = new { message = "Invalid file type." } });
                }

                // Generate unique file name
                var uniqueFileName = Guid.NewGuid().ToString() + extension;
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

                // Ensure the uploads folder exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }

                var response = new
                {
                    uploaded = true,
                    url = Url.Content($"~/uploads/{uniqueFileName}")
                };

                // Return the expected response format for CKEditor
                return Ok(response);

                // return Ok(new { uploaded = true, url = Url.Content($"~/uploads/{uniqueFileName}") });
            }
            catch (IOException ioEx)
            {
                return StatusCode(500, new { error = new { message = "An error occurred while saving the file. Please try again." } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { message = "An unexpected error occurred. Please try again later." } });
            }
        }
    }
}