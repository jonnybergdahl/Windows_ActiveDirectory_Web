using Bergdahl.ActiveDirectory.Web.Models;

namespace Bergdahl.ActiveDirectory.Web.Controllers;

using System.Security.Claims;
using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Bergdahl.ActiveDirectory.Core.Models;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationSettings _settings;

    public AuthController(IOptions<ApplicationSettings> options)
    {
        _settings = options.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
#if DEBUG
            bool valid = true; // Always valid in debug mode
#else
            var domain = _settings.ActiveDirectoryDomain;
            using var context = new PrincipalContext(ContextType.Domain, domain);
            bool valid = context.ValidateCredentials(req.Username, req.Password);
#endif

            if (!valid)
                return Unauthorized(new { message = "Invalid credentials" });

            // Create auth cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, req.Username)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // Store credentials in session for WMI calls
            HttpContext.Session.SetString("adUser", req.Username);
            HttpContext.Session.SetString("adPass", req.Password); // encrypt in production

            // Generate CSRF token
            var csrf = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("csrf", csrf);

            return Ok(new { message = "Logged in", user = req.Username, csrf });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Auth error: {ex.Message}" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}

