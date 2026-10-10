using Bergdahl.ActiveDirectory.Core.ActiveDirectory;
using Bergdahl.ActiveDirectory.Core.Models;
using Bergdahl.ActiveDirectory.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bergdahl.ActiveDirectory.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _users;
    private readonly ApplicationSettings _settings;

    public UsersController(IUserManagementService users, IOptions<ApplicationSettings> options)
    {
        _users = users;
        _settings = options.Value;
    }

    private (string user, string pass, string domain, List<string> adPaths, List<string> groupsAdPaths) GetCredentials()
    {
        var user = HttpContext.Session.GetString("adUser");
        var pass = HttpContext.Session.GetString("adPass");
        var domain = HttpContext.Session.GetString("adDomain") ?? _settings.ActiveDirectoryDomain;
        var adPaths = _settings.UsersAdPath;
        var groupsAdPaths = _settings.GroupsAdPath;
        return (user!, pass!, domain, adPaths, groupsAdPaths);
    }

    [HttpGet("paths")]
    public IActionResult GetPaths()
    {
        return Ok(_settings.UsersAdPath);
    }

    [HttpGet]
    public IActionResult GetUsers([FromQuery] string adPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(adPath))
                return BadRequest(new { message = "adPath is required" });

            var (admin, adminPass, domain, _, _) = GetCredentials();
            var users = _users.GetUsers(admin, adminPass, domain, adPath);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error fetching users: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateUserRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.UserLoginName) ||
                string.IsNullOrWhiteSpace(req.FirstName) ||
                string.IsNullOrWhiteSpace(req.LastName) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "All fields are required" });

            var (admin, adminPass, domain, adPaths, groupsAdPaths) = GetCredentials();
            var adPath = string.IsNullOrWhiteSpace(req.AdPath) ? (adPaths.FirstOrDefault() ?? string.Empty) : req.AdPath;
            _users.CreateUser(req.UserLoginName, req.FirstName, req.LastName, req.Password, admin, adminPass, domain, adPath);

            if (req.Groups != null && req.Groups.Count > 0)
            {
                foreach (var groupName in req.Groups)
                {
                    try
                    {
                        _users.AddToGroup(req.UserLoginName, groupName, admin, adminPass, domain, adPath, groupsAdPaths);
                    }
                    catch (Exception ex)
                    {
                        // Log or ignore if some groups fail, but return info in response?
                        // For now, let's just log it to debug
                        System.Diagnostics.Debug.WriteLine($"Failed to add user to group {groupName}: {ex.Message}");
                    }
                }
            }

            return Ok(new { message = $"User '{req.UserLoginName}' created" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    public IActionResult Delete([FromBody] DeleteUserRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.UserLoginName))
                return BadRequest(new { message = "User login name is required" });

            var (admin, adminPass, domain, adPaths, _) = GetCredentials();
            var adPath = adPaths.FirstOrDefault() ?? string.Empty;
            _users.RemoveUser(req.UserLoginName, admin, adminPass, domain, adPath);
            return Ok(new { message = $"User '{req.UserLoginName}' deleted" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.UserLoginName) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { message = "User login name and new password are required" });

            var (admin, adminPass, domain, adPaths, _) = GetCredentials();
            var adPath = adPaths.FirstOrDefault() ?? string.Empty;
            _users.ChangePassword(req.UserLoginName, req.NewPassword, admin, adminPass, domain, adPath);
            return Ok(new { message = $"Password changed for '{req.UserLoginName}'" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("groups")]
    public IActionResult GetGroups()
    {
        try
        {
            var (admin, adminPass, domain, adPaths, groupsAdPaths) = GetCredentials();
            var groups = _users.GetGroups(admin, adminPass, domain, adPaths, groupsAdPaths);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error fetching groups: {ex.Message}" });
        }
    }

    [HttpPost("add-to-group")]
    public IActionResult AddToGroup([FromBody] GroupMembershipRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.UserLoginName) || string.IsNullOrWhiteSpace(req.GroupName))
                return BadRequest(new { message = "User login name and group name are required" });

            var (admin, adminPass, domain, adPaths, groupsAdPaths) = GetCredentials();
            var adPath = adPaths.FirstOrDefault() ?? string.Empty;
            _users.AddToGroup(req.UserLoginName, req.GroupName, admin, adminPass, domain, adPath, groupsAdPaths);
            return Ok(new { message = $"User '{req.UserLoginName}' added to group '{req.GroupName}'" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("remove-from-group")]
    public IActionResult RemoveFromGroup([FromBody] GroupMembershipRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.UserLoginName) || string.IsNullOrWhiteSpace(req.GroupName))
                return BadRequest(new { message = "User login name and group name are required" });

            var (admin, adminPass, domain, adPaths, groupsAdPaths) = GetCredentials();
            var adPath = adPaths.FirstOrDefault() ?? string.Empty;
            _users.RemoveFromGroup(req.UserLoginName, req.GroupName, admin, adminPass, domain, adPath, groupsAdPaths);
            return Ok(new { message = $"User '{req.UserLoginName}' deleted from group '{req.GroupName}'" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("groups/{loginName}")]
    public IActionResult GetUserGroups(string loginName)
    {
        try
        {
            var (admin, adminPass, domain, adPaths, _) = GetCredentials();
            var adPath = adPaths.FirstOrDefault() ?? string.Empty;
            var groups = _users.GetUserGroups(loginName, admin, adminPass, domain, adPath);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error fetching user groups: {ex.Message}" });
        }
    }
}
