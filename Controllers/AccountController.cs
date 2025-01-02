using BlogManagementApp.Data;
using BlogManagementApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BlogManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly BlogManagementDBContext _DbContext;
        public AccountController(BlogManagementDBContext DbContext)
        {
            _DbContext = DbContext;
        }
        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if the username already exits
                if (_DbContext.Users.Any(u => u.UserName == user.UserName))
                {
                    //ModelState.AddModelError("", "Username already exists");
                    TempData["Uservalidation"] = "Username already exists";
                    return View();
                }

                // Check if Password and Confirm password are same
                if (user.Password != user.CPassword)
                {
                    TempData["Passwordvalidation"] = "Confirm password is not same";
                    return View();
                }

                _DbContext.Users.Add(user);
                _DbContext.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            return View();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _DbContext.Users.SingleOrDefault(u => u.UserName == username && u.Password == password);
            if (user != null)
            {
                HttpContext.Session.SetString("UserSession", username);
              
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["LoginValidation"] = "Invalid username or password";

            }


            return View();
        }

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            if(HttpContext.Session.GetString("UserSession") != null)
            {
                HttpContext.Session.Remove("UserSession");
                return RedirectToAction("Login", "Account");

            }
            return View("Login", "Application");           
        }
    }
}
