namespace Bergdahl.ActiveDirectory.Core.Models;

public class DnsRecord
{
    public string HostName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string RecordType { get; set; } = "A";
    public int TTL { get; set; }
}