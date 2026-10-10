using Bergdahl.ActiveDirectory.Core.Models;
using Bergdahl.ActiveDirectory.Mock;
using Bergdahl.ActiveDirectory.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bergdahl.ActiveDirectory.Tests;

public class MockDnsControllerTests
{
    private readonly DnsController _controller;
    private readonly MockDnsWmiService _mockService;
    private readonly ApplicationSettings _settings;

    public MockDnsControllerTests()
    {
        _mockService = new MockDnsWmiService();
        _settings = new ApplicationSettings
        {
            ActiveDirectoryDomain = "example.com",
            DnsZones = new List<string> { "example.com" }
        };
        var options = Options.Create(_settings);
        
        _controller = new DnsController(_mockService, options);
        
        // Mock HttpContext and Session
        var mockHttpContext = new Mock<HttpContext>();
        var mockSession = new Mock<ISession>();
        
        byte[] userBytes = System.Text.Encoding.UTF8.GetBytes("testuser");
        byte[] passBytes = System.Text.Encoding.UTF8.GetBytes("testpass");
        
        mockSession.Setup(s => s.TryGetValue("adUser", out userBytes)).Returns(true);
        mockSession.Setup(s => s.TryGetValue("adPass", out passBytes)).Returns(true);
        
        mockHttpContext.Setup(h => h.Session).Returns(mockSession.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };
    }

    [Fact]
    public void GetZones_ReturnsConfiguredZones()
    {
        // Act
        var result = _controller.GetZones();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var zones = Assert.IsType<List<string>>(okResult.Value);
        Assert.Single(zones);
        Assert.Equal("example.com", zones[0]);
    }

    [Fact]
    public void GetRecords_ReturnsRecordsForZone()
    {
        // Act
        var result = _controller.GetRecords("example.com");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var records = Assert.IsType<List<DnsRecord>>(okResult.Value);
        // MockDnsWmiService pre-populates example.com with 2 records
        Assert.NotEmpty(records);
    }

    [Fact]
    public void RemoveRecord_NoIpAddress_ReturnsOk()
    {
        // Act
        var req = new Bergdahl.ActiveDirectory.Web.Models.DnsRequest 
        { 
            Zone = "example.com", 
            Hostname = "www" // www.example.com exists in MockDnsWmiService
        };
        var result = _controller.RemoveRecord(req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = Newtonsoft.Json.Linq.JObject.FromObject(okResult.Value);
        Assert.Contains("Deleted", json["message"].ToString());
    }

    [Fact]
    public void RemoveRecord_NonExistentRecord_ReturnsBadRequest()
    {
        // Act
        var req = new Bergdahl.ActiveDirectory.Web.Models.DnsRequest 
        { 
            Zone = "example.com", 
            Hostname = "nonexistent" 
        };
        var result = _controller.RemoveRecord(req);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = Newtonsoft.Json.Linq.JObject.FromObject(badResult.Value);
        Assert.Contains("not found", json["message"].ToString());
    }

    [Fact]
    public void RemoveRecord_InvalidHostname_ReturnsBadRequest()
    {
        // Act
        var req = new Bergdahl.ActiveDirectory.Web.Models.DnsRequest 
        { 
            Zone = "example.com", 
            Hostname = "invalid!hostname" // ! not allowed
        };
        var result = _controller.RemoveRecord(req);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = Newtonsoft.Json.Linq.JObject.FromObject(badResult.Value);
        Assert.Equal("Invalid hostname", json["message"].ToString());
    }

    [Fact]
    public void RemoveRecord_Fqdn_ReturnsOk()
    {
        // Act
        var req = new Bergdahl.ActiveDirectory.Web.Models.DnsRequest 
        { 
            Zone = "example.com", 
            Hostname = "www.example.com" 
        };
        var result = _controller.RemoveRecord(req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = Newtonsoft.Json.Linq.JObject.FromObject(okResult.Value);
        Assert.Contains("Deleted", json["message"].ToString());
    }
}
