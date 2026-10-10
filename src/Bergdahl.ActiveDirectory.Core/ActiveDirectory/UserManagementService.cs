namespace Bergdahl.ActiveDirectory.Core.ActiveDirectory;

using Bergdahl.ActiveDirectory.Core.Models;
using System.Collections.Generic;

public interface IUserManagementService
{
    List<UserRecord> GetUsers(string username, string password, string domain, string adPath);
    List<GroupRecord> GetGroups(string username, string password, string domain, List<string> adPaths, List<string> groupsAdPaths);
    void CreateUser(string loginName, string firstName, string lastName, string password, string adminUser, string adminPass, string domain, string adPath);
    void RemoveUser(string loginName, string adminUser, string adminPass, string domain, string adPath);
    void ChangePassword(string loginName, string newPassword, string adminUser, string adminPass, string domain, string adPath);
    void AddToGroup(string loginName, string groupName, string adminUser, string adminPass, string domain, string adPath, List<string>? groupsAdPaths = null);
    void RemoveFromGroup(string loginName, string groupName, string adminUser, string adminPass, string domain, string adPath, List<string>? groupsAdPaths = null);
    List<string> GetUserGroups(string loginName, string adminUser, string adminPass, string domain, string adPath);
}
