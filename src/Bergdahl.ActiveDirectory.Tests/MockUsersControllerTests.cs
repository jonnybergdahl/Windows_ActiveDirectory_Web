using Bergdahl.ActiveDirectory.Core.Models;
using Bergdahl.ActiveDirectory.Mock;
using Bergdahl.ActiveDirectory.Web.Controllers;
using Bergdahl.ActiveDirectory.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bergdahl.ActiveDirectory.Tests;

public class MockUsersControllerTests
{
    private readonly UsersController _controller;
    private readonly MockUserManagementService _mockService;
    private readonly ApplicationSettings _settings;

    public MockUsersControllerTests()
    {
        _mockService = new MockUserManagementService();
        _settings = new ApplicationSettings
        {
            ActiveDirectoryDomain = "example.com",
            UsersAdPath = new List<string> { "CN=Users,DC=example,DC=com" },
            GroupsAdPath = new List<string> { "CN=Users,DC=example,DC=com" }
        };
        var options = Options.Create(_settings);

        _controller = new UsersController(_mockService, options);

        // Mock HttpContext and Session with admin user credentials
        var mockHttpContext = new Mock<HttpContext>();
        var mockSession = new TestSession();
        mockSession.SetString("adUser", "admin");
        mockSession.SetString("adPass", "password");

        mockHttpContext.Setup(h => h.Session).Returns(mockSession);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };
    }

    [Fact]
    public void GetGroups_ReturnsSomeGroups()
    {
        var result = _controller.GetGroups();
        var ok = Assert.IsType<OkObjectResult>(result);
        var groups = Assert.IsType<List<GroupRecord>>(ok.Value);
        Assert.NotEmpty(groups);
    }

    [Fact]
    public void CreateUser_ReturnsOk()
    {
        var req = new CreateUserRequest
        {
            UserLoginName = "jdoe",
            FirstName = "John",
            LastName = "Doe",
            Password = "P@ssw0rd!"
        };
        var result = _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("created", ok.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

// Minimal in-memory ISession implementation for tests
internal class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public IEnumerable<string> Keys => _store.Keys;
    public string Id { get; } = Guid.NewGuid().ToString();
    public bool IsAvailable { get; } = true;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);

    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}
