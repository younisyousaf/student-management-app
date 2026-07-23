using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.WebApi.DTOs;

namespace StudentManagementApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        // POST: api/auth/register
        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto request)
        {
            try
            {
                // Use the domain constructor to enforce DDD encapsulation rules!
                var user = new User(
                    request.Username,
                    request.Email,
                    request.Role ?? "Admin"
                );

                _userService.RegisterUser(user, request.Password);
                return Ok(new { Message = "User registered successfully!" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }

        // POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto request)
        {
            var token = _userService.Login(request.Username, request.Password);

            if (token == null)
            {
                return Unauthorized(new { Message = "Invalid username or password." });
            }
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,           // requires HTTPS
                //SameSite = SameSiteMode.None, //Cross-Site Compatibility (https, http)
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            return Ok(new
            {
                Token = token,
                Message = "Login successful!"
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return Ok(new { Message = "Logged out." });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            // Confirms the cookie is valid — Angular calls this on app init to restore session
            return Ok(new { Username = User.Identity?.Name });
        }
    }

}