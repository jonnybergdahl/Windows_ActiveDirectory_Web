using System.Diagnostics.CodeAnalysis;
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
public class DnsController : ControllerBase
{
    private readonly IDnsWmiService _dns;
    private readonly ApplicationSettings _settings;

    public DnsController(IDnsWmiService dns, IOptions<ApplicationSettings> options)
    {
        _dns = dns;
        _settings = options.Value;
    }

    private (string user, string pass, string domain) GetCredentials()
    {
        var user = HttpContext.Session.GetString("adUser");
        var pass = HttpContext.Session.GetString("adPass"); // decrypt in real impl
        var domain = HttpContext.Session.GetString("adDomain") ?? _settings.ActiveDirectoryDomain;
        return (user!, pass!, domain);
    }

    [HttpGet("zones")]
    public IActionResult GetZones()
    {
        var zones = _settings.DnsZones ?? new List<string>();
        return Ok(zones);
    }

    [HttpGet("{zone}")]
    public IActionResult GetRecords(string zone)
    {
        var (user, pass, domain) = GetCredentials();
        var records = _dns.GetRecords(zone, user, pass, domain);
        return Ok(records);
    }

    [HttpPost]
    public IActionResult AddRecord([FromBody] DnsRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Zone))
            return BadRequest(new { message = "Invalid zone" });
        if (!IsValidHostname(req.Hostname) || !System.Net.IPAddress.TryParse(req.IpAddress, out _))
            return BadRequest(new { message = "Invalid input" });

        try
        {
            var (user, pass, domain) = GetCredentials();
            _dns.AddRecord(req.Zone, req.Hostname, req.IpAddress, user, pass, domain);
            return Ok(new { message = $"Added {req.Hostname}.{req.Zone} -> {req.IpAddress}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cname")]
    public IActionResult AddCnameRecord([FromBody] CnameRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Zone))
            return BadRequest(new { message = "Invalid zone" });
        if (!IsValidHostname(req.AliasName) || string.IsNullOrWhiteSpace(req.TargetHost))
            return BadRequest(new { message = "Invalid input" });

        try
        {
            var (user, pass, domain) = GetCredentials();
            _dns.AddCnameRecord(req.Zone!, req.AliasName!, req.TargetHost!, user, pass, domain);
            return Ok(new { message = $"Added Alias {req.AliasName}.{req.Zone} -> {req.TargetHost}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    public IActionResult RemoveRecord([FromBody] DnsRequest req)
    {
        if (req == null)
            return BadRequest(new { message = "Invalid request" });
        if (string.IsNullOrWhiteSpace(req.Zone))
            return BadRequest(new { message = "Invalid zone" });
        if (!IsValidHostname(req.Hostname))
            return BadRequest(new { message = "Invalid hostname" });

        try
        {
            var (user, pass, domain) = GetCredentials();
            var deleted = _dns.RemoveRecord(req.Zone, req.Hostname, user, pass, domain);
            if (deleted)
            {
                return Ok(new { message = $"Deleted {req.Hostname}.{req.Zone}" });
            }
            else
            {
                return BadRequest(new { message = $"Record {req.Hostname}.{req.Zone} not found" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsValidHostname([NotNullWhen(true)] string? name) =>
        System.Text.RegularExpressions.Regex.IsMatch(name ?? "", @"^[a-zA-Z0-9\-\._]{1,255}$");
}


