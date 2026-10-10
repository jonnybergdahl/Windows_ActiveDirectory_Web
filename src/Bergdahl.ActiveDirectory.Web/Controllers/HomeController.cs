using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bergdahl.ActiveDirectory.Web.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("/")]
public class HomeController : Controller
{
    private readonly IWebHostEnvironment _env;

    public HomeController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    [HttpGet("index.html")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("users")]
    [HttpGet("users.html")]
    public IActionResult Users()
    {
        return View();
    }
}