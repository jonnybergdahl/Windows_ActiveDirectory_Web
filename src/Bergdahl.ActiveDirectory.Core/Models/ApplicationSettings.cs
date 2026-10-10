namespace Bergdahl.ActiveDirectory.Core.Models;

public class ApplicationSettings
{
    public string ServerName { get; set; } = string.Empty;
    public string ActiveDirectoryDomain { get; set; } = string.Empty;
    public List<string> UsersAdPath { get; set; } = new();
    public List<string> GroupsAdPath { get; set; } = new();
    public List<string> DnsZones { get; set; } = new();
    public string TestUsername { get; set; } = string.Empty;
    public string TestPassword { get; set; } = string.Empty;
}
