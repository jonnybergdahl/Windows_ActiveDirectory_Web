namespace Bergdahl.ActiveDirectory.Core.ActiveDirectory;

using Bergdahl.ActiveDirectory.Core.Models;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;

public class UserManagementService : IUserManagementService
{
    public List<UserRecord> GetUsers(string username, string password, string domain, string adPath)
    {
        using var ctx = CreateContext(domain, adPath, username, password);
        using var searcher = new PrincipalSearcher(new UserPrincipal(ctx));
        return searcher.FindAll()
            .OfType<UserPrincipal>()
            .Select(u => new UserRecord
            {
                UserLoginName = u.SamAccountName,
                FirstName = u.GivenName,
                LastName = u.Surname
            })
            .ToList();
    }

    public List<GroupRecord> GetGroups(string username, string password, string domain, List<string> adPaths, List<string> groupsAdPaths)
    {
        var groups = new List<GroupRecord>();
        var pathsToFetch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (adPaths != null)
        {
            foreach (var path in adPaths)
            {
                if (!string.IsNullOrWhiteSpace(path)) pathsToFetch.Add(path);
            }
        }
        
        if (groupsAdPaths != null)
        {
            foreach (var path in groupsAdPaths)
            {
                if (!string.IsNullOrWhiteSpace(path)) pathsToFetch.Add(path);
            }
        }

        foreach (var path in pathsToFetch)
        {
            var pathGroups = FetchGroupsFromPath(domain, path, username, password);
            foreach (var group in pathGroups)
            {
                if (!groups.Any(g => string.Equals(g.GroupName, group.GroupName, StringComparison.OrdinalIgnoreCase)))
                {
                    groups.Add(group);
                }
            }
        }
        
        return groups;
    }

    private List<GroupRecord> FetchGroupsFromPath(string domain, string adPath, string username, string password)
    {
        using var ctx = CreateContext(domain, adPath, username, password);
        using var searcher = new PrincipalSearcher(new GroupPrincipal(ctx));
        return searcher.FindAll()
            .OfType<GroupPrincipal>()
            .Select(g => new GroupRecord
            {
                GroupName = g.SamAccountName,
                Description = g.Description
            })
            .ToList();
    }

    private PrincipalContext CreateContext(string domain, string adPath, string adminUser, string adminPass)
    {
        return new PrincipalContext(ContextType.Domain, domain, adPath, adminUser, adminPass);
    }

    public void CreateUser(string loginName, string firstName, string lastName, string password, string adminUser, string adminPass, string domain, string adPath)
    {
        using var ctx = CreateContext(domain, adPath, adminUser, adminPass);

        // Check if user already exists
        var existing = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, loginName);
        if (existing != null)
        {
            throw new InvalidOperationException($"User '{loginName}' already exists");
        }

        var user = new UserPrincipal(ctx)
        {
            SamAccountName = loginName,
            GivenName = firstName,
            Surname = lastName
        };

        user.SetPassword(password);
        user.Enabled = true;
        user.Save();
    }

    public void RemoveUser(string loginName, string adminUser, string adminPass, string domain, string adPath)
    {
        using var ctx = CreateContext(domain, adPath, adminUser, adminPass);
        var user = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, loginName)
                   ?? throw new InvalidOperationException($"User '{loginName}' not found");
        user.Delete();
    }

    public void ChangePassword(string loginName, string newPassword, string adminUser, string adminPass, string domain, string adPath)
    {
        using var ctx = CreateContext(domain, adPath, adminUser, adminPass);
        var user = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, loginName)
                   ?? throw new InvalidOperationException($"User '{loginName}' not found");
        user.SetPassword(newPassword);
        user.Save();
    }

    public void AddToGroup(string loginName, string groupName, string adminUser, string adminPass, string domain, string adPath, List<string>? groupsAdPaths = null)
    {
        using var ctx = CreateContext(domain, adPath, adminUser, adminPass);
        var group = FindGroup(ctx, groupName, domain, adminUser, adminPass, groupsAdPaths);
        if (group == null)
            throw new InvalidOperationException($"Group '{groupName}' not found");

        var user = UserPrincipal.FindByIdentity(ctx, loginName)
                   ?? throw new InvalidOperationException($"User '{loginName}' not found");

        if (!group.Members.Contains(user))
        {
            group.Members.Add(user);
            group.Save();
        }
    }

    public void RemoveFromGroup(string loginName, string groupName, string adminUser, string adminPass, string domain, string adPath, List<string>? groupsAdPaths = null)
    {
        using var ctx = CreateContext(domain, adPath, adminUser, adminPass);
        var group = FindGroup(ctx, groupName, domain, adminUser, adminPass, groupsAdPaths);
        if (group == null)
            throw new InvalidOperationException($"Group '{groupName}' not found");

        var user = UserPrincipal.FindByIdentity(ctx, loginName)
                   ?? throw new InvalidOperationException($"User '{loginName}' not found");

        if (group.Members.Contains(user))
        {
            group.Members.Remove(user);
            group.Save();
        }
    }

    private GroupPrincipal? FindGroup(PrincipalContext userCtx, string groupName, string domain, string adminUser, string adminPass, List<string>? groupsAdPaths)
    {
        // Try user context first
        var group = GroupPrincipal.FindByIdentity(userCtx, groupName);
        if (group != null) return group;

        // Try other paths if provided
        if (groupsAdPaths != null)
        {
            foreach (var path in groupsAdPaths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                try
                {
                    var ctx = CreateContext(domain, path, adminUser, adminPass);
                    group = GroupPrincipal.FindByIdentity(ctx, groupName);
                    if (group != null) return group;
                }
                catch
                {
                    // Ignore search errors in specific OUs
                }
            }
        }

        return null;
    }

    public List<string> GetUserGroups(string loginName, string adminUser, string adminPass, string domain, string adPath)
    {
        using var ctx = CreateContext(domain, adPath, adminUser, adminPass);
        var user = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, loginName)
                   ?? throw new InvalidOperationException($"User '{loginName}' not found");

        return user.GetGroups()
            .Select(g => g.SamAccountName)
            .ToList();
    }
}
