using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RewardsAndRecognitionRepository.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]//
    public class AccountController : ControllerBase
    {

        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Login via email/password.
        /// Called by Blazor: POST api/account/login
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Equivalent to your MVC LoginModel.OnPostAsync
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                var user = await _userManager.FindByEmailAsync(model.Email);
                // var token = GenerateJwtToken(user);
                // return Ok(new { Token = token });
                if (user == null)
                    return Ok(); // fallback, should not happen

                var roles = await _userManager.GetRolesAsync(user); // could be multiple
                var role = roles.FirstOrDefault(); // pick first (or decide rule)

                return Ok(new
                {
                    userId = user.Id,
                    role = role
                });
            }

            if (result.RequiresTwoFactor)
            {
                // For now just indicate with a specific status/message.
                // You can handle this in the Blazor client later if needed.
                _logger.LogInformation("User requires two-factor authentication.");
                return StatusCode(428, "RequiresTwoFactor"); // 428 Precondition Required
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                // 423 Locked matches your Blazor status check using HttpStatusCode.Locked
                return StatusCode(423, "User account is locked out.");
            }

            // Invalid credentials
            _logger.LogWarning("Invalid login attempt for email {Email}.", model.Email);
            return Unauthorized("Invalid login attempt."); // 401
        }

        /// <summary>
        /// Logout current user (if using cookie auth).
        /// Blazor can call: POST api/account/logout
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return Ok();
        }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
    }
}
