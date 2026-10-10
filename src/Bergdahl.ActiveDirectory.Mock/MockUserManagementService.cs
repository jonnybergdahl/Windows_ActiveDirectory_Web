using Bergdahl.ActiveDirectory.Core.ActiveDirectory;
using Bergdahl.ActiveDirectory.Core.Models;

namespace Bergdahl.ActiveDirectory.Mock;

public class MockUserManagementService : IUserManagementService
{
    private static readonly Dictionary<string, UserRecord> _users = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, GroupRecord> _groups = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Domain Admins"] = new GroupRecord { GroupName = "Domain Admins", Description = "Designated administrators of the domain" },
        ["Domain Users"] = new GroupRecord { GroupName = "Domain Users", Description = "All domain users" },
        ["IT Department"] = new GroupRecord { GroupName = "IT Department", Description = "IT Staff" }
    };
    private static readonly Dictionary<string, List<string>> _groupMembers = new(StringComparer.OrdinalIgnoreCase);

    public List<UserRecord> GetUsers(string username, string password, string domain, string adPath)
    {
        // For mock, we ignore adPath filter and return all
        return _users.Values.ToList();
    }

    public List<GroupRecord> GetGroups(string username, string password, string domain, List<string> adPath, List<string> groupsAdPaths)
        => _groups.Values.ToList();

    public void CreateUser(string loginName, string firstName, string lastName, string password, string adminUser, string adminPass, string domain, string adPath)
    {
        if (_users.ContainsKey(loginName))
            throw new InvalidOperationException($"User '{loginName}' already exists");

        _users[loginName] = new UserRecord
        {
            UserLoginName = loginName,
            FirstName = firstName,
            LastName = lastName
        };
    }

    public void RemoveUser(string loginName, string adminUser, string adminPass, string domain, string adPath)
    {
        if (!_users.Remove(loginName))
            throw new InvalidOperationException($"User '{loginName}' not found");
    }

    public void ChangePassword(string loginName, string newPassword, string adminUser, string adminPass, string domain, string adPath)
    {
        if (!_users.ContainsKey(loginName))
            throw new InvalidOperationException($"User '{loginName}' not found");
        // Password is not stored in mock. No-op.
    }

    public void AddToGroup(string loginName, string groupName, string adminUser, string adminPass, string domain, string adPath, List<string>? groupsAdPaths = null)
    {
        if (!_users.ContainsKey(loginName))
            throw new InvalidOperationException($"User '{loginName}' not found");
        if (!_groups.ContainsKey(groupName))
            throw new InvalidOperationException($"Group '{groupName}' not found");

        if (!_groupMembers.ContainsKey(groupName))
            _groupMembers[groupName] = new List<string>();

        if (!_groupMembers[groupName].Contains(loginName, StringComparer.OrdinalIgnoreCase))
            _groupMembers[groupName].Add(loginName);
    }

    public void RemoveFromGroup(string loginName, string groupName, string adminUser, string adminPass, string domain, string adPath, List<string>? groupsAdPaths = null)
    {
        if (!_users.ContainsKey(loginName))
            throw new InvalidOperationException($"User '{loginName}' not found");
        if (!_groups.ContainsKey(groupName))
            throw new InvalidOperationException($"Group '{groupName}' not found");

        if (_groupMembers.ContainsKey(groupName))
            _groupMembers[groupName].RemoveAll(u => u.Equals(loginName, StringComparison.OrdinalIgnoreCase));
    }

    public List<string> GetUserGroups(string loginName, string adminUser, string adminPass, string domain, string adPath)
    {
        var result = new List<string>();
        foreach (var group in _groupMembers)
        {
            if (group.Value.Contains(loginName, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(group.Key);
            }
        }
        return result;
    }
}
