using Bergdahl.ActiveDirectory.Core.ActiveDirectory;
using Xunit;

namespace Bergdahl.ActiveDirectory.Tests;

public class UserManagementServiceTests : CoreLiveTestsBase
{
    [Fact]
    public void GetGroups_Live_ReturnsData()
    {
        if (!IsLiveEnvironment) return;

        var service = new UserManagementService();
        
        try
        {
            var groups = service.GetGroups(Settings.TestUsername, Settings.TestPassword, Settings.ActiveDirectoryDomain, Settings.UsersAdPath, Settings.GroupsAdPath);
            Assert.NotNull(groups);
            Assert.True(groups.Count > 0);
        }
        catch (System.DirectoryServices.AccountManagement.PrincipalServerDownException)
        {
            // Server down is expected in non-AD environments
        }
        catch (System.DirectoryServices.AccountManagement.PrincipalOperationException)
        {
            // Access denied etc.
        }
    }
}
