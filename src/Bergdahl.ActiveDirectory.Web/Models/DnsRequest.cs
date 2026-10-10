namespace Bergdahl.ActiveDirectory.Web.Models;

public class DnsRequest
{
    public string? Zone { get; set; }
    public string? Hostname { get; set; }
    public string? IpAddress { get; set; }
}

public class CnameRequest
{
    public string? Zone { get; set; }
    public string? AliasName { get; set; }
    public string? TargetHost { get; set; }
}