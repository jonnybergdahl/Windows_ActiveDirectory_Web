namespace Bergdahl.ActiveDirectory.Web.Models;

public class CreateUserRequest
{
    public string UserLoginName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AdPath { get; set; } = string.Empty;
    public List<string> Groups { get; set; } = new();
}

public class DeleteUserRequest
{
    public string UserLoginName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string UserLoginName { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class GroupMembershipRequest
{
    public string UserLoginName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}
