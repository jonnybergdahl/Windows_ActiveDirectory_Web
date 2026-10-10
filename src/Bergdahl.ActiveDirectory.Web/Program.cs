using System.Security.Cryptography;
using Bergdahl.ActiveDirectory.Core.Models;
using Bergdahl.ActiveDirectory.Core.ActiveDirectory;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
#if DEBUG
using Bergdahl.ActiveDirectory.Mock;
#endif

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllersWithViews();
    
    builder.Services.AddDistributedMemoryCache(); 
    builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login.html";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));
#if DEBUG
builder.Services.AddSingleton<IDnsWmiService, MockDnsWmiService>();
builder.Services.AddSingleton<IUserManagementService, MockUserManagementService>();
#else
builder.Services.AddSingleton<IDnsWmiService>(sp => 
{
    var settings = builder.Configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>()
                    ?? throw new InvalidOperationException("DNS Server Name is not configured.");
    return new DnsWmiService(settings.ServerName);
});
builder.Services.AddSingleton<IUserManagementService, UserManagementService>();
#endif

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// CSRF protection for cookie-authenticated API calls
app.Use(async (context, next) =>
{
    // Only protect state-changing methods
    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method))
    {
        // Only protect API routes (your static file navigation should still work)
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            // Skip CSRF for login endpoint itself (no session yet)
            if (context.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var sessionToken = context.Session.GetString("csrf");
            var headerToken = context.Request.Headers["X-XSRF-TOKEN"].ToString();

            if (string.IsNullOrWhiteSpace(sessionToken) ||
                string.IsNullOrWhiteSpace(headerToken) ||
                !CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(sessionToken),
                    System.Text.Encoding.UTF8.GetBytes(headerToken)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "CSRF validation failed." });
                return;
            }
        }
    }

    await next();
});

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}