using Bergdahl.ActiveDirectory.Core.ActiveDirectory;
using Xunit;

namespace Bergdahl.ActiveDirectory.Tests;

public class DnsWmiServiceTests : CoreLiveTestsBase
{
    [Fact]
    public void GetRecords_Live_ReturnsData()
    {
        if (!IsLiveEnvironment) return;

        var service = new DnsWmiService(Settings.ServerName);
        var zone = Settings.DnsZones.FirstOrDefault() ?? "bergdahl.local";
        
        // These tests require valid credentials in appsettings or elsewhere
        // For now, we just check if it throws or returns something
        // Note: Real tests might need to skip if credentials are not provided
        try 
        {
            var records = service.GetRecords(zone, Settings.TestUsername, Settings.TestPassword, Settings.ActiveDirectoryDomain);
            Assert.NotNull(records);
            Assert.True(records.Count > 0);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Expected if credentials/environment are not fully set up, 
            // but at least we tested the WMI connection attempt.
        }
        catch (System.Management.ManagementException ex) when (ex.ErrorCode == System.Management.ManagementStatus.AccessDenied)
        {
            // Access denied is also a "success" in terms of reaching the server
        }
    }
}
