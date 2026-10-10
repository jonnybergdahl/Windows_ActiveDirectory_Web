namespace Bergdahl.ActiveDirectory.Core.ActiveDirectory;

using System.Management;
using Bergdahl.ActiveDirectory.Core.Models;

public interface IDnsWmiService
{
    List<DnsRecord> GetRecords(string zone, string username, string password, string domain);
    void AddRecord(string zone, string hostname, string ip, string username, string password, string domain);
    void AddCnameRecord(string zone, string aliasName, string targetHost, string username, string password, string domain);
    bool RemoveRecord(string zone, string hostname, string username, string password, string domain);
}

public class DnsWmiService : IDnsWmiService
{
    private readonly string _dnsServer;

    public DnsWmiService(string dnsServer)
    {
        _dnsServer = dnsServer;
    }
    private ManagementScope Connect(string username, string password, string domain)
    {
        var scope = new ManagementScope($@"\\{_dnsServer}\root\MicrosoftDNS");

        scope.Options = new ConnectionOptions
        {
            Username = $"{domain}\\{username}",
            Password = password,
            Impersonation = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.PacketPrivacy
        };

        scope.Connect();
        return scope;
    }

    public List<DnsRecord> GetRecords(string zone, string username, string password, string domain)
    {
        var scope = Connect(username, password, domain);
        var records = new List<DnsRecord>();

        // Fetch A records
        var aQuery = new ObjectQuery($"SELECT * FROM MicrosoftDNS_AType WHERE ContainerName='{zone}'");
        using var aSearcher = new ManagementObjectSearcher(scope, aQuery);
        foreach (ManagementObject obj in aSearcher.Get())
        {
            records.Add(new DnsRecord
            {
                HostName = obj["OwnerName"]?.ToString() ?? string.Empty,
                IpAddress = obj["IPAddress"]?.ToString() ?? string.Empty,
                RecordType = "A",
                TTL = Convert.ToInt32(obj["TTL"])
            });
        }

        // Fetch CNAME records
        var cnameQuery = new ObjectQuery($"SELECT * FROM MicrosoftDNS_CNAMEType WHERE ContainerName='{zone}'");
        using var cnameSearcher = new ManagementObjectSearcher(scope, cnameQuery);
        foreach (ManagementObject obj in cnameSearcher.Get())
        {
            records.Add(new DnsRecord
            {
                HostName = obj["OwnerName"]?.ToString() ?? string.Empty,
                IpAddress = obj["PrimaryName"]?.ToString() ?? string.Empty, // Use target host in place of IP address for UI
                RecordType = "CNAME",
                TTL = Convert.ToInt32(obj["TTL"])
            });
        }

        return records;
    }

    public void AddRecord(string zone, string hostname, string ip,
                          string username, string password, string domain)
    {
        var scope = Connect(username, password, domain);
        var dnsClass = new ManagementClass(scope, 
            new ManagementPath("MicrosoftDNS_AType"), null);

        var inParams = dnsClass.GetMethodParameters("CreateInstanceFromPropertyData");
        inParams["DnsServerName"] = _dnsServer;
        inParams["ContainerName"] = zone;
        var fqdn = hostname.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase) 
            ? hostname 
            : $"{hostname}.{zone}";
        inParams["OwnerName"] = fqdn;
        inParams["IPAddress"] = ip;

        dnsClass.InvokeMethod("CreateInstanceFromPropertyData", inParams, null);
    }

    public void AddCnameRecord(string zone, string aliasName, string targetHost,
                               string username, string password, string domain)
    {
        var scope = Connect(username, password, domain);
        var dnsClass = new ManagementClass(scope, 
            new ManagementPath("MicrosoftDNS_CNAMEType"), null);

        var inParams = dnsClass.GetMethodParameters("CreateInstanceFromPropertyData");
        inParams["DnsServerName"] = _dnsServer;
        inParams["ContainerName"] = zone;
        var fqdn = aliasName.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase) 
            ? aliasName 
            : $"{aliasName}.{zone}";
        inParams["OwnerName"] = fqdn;
        inParams["PrimaryName"] = targetHost;

        dnsClass.InvokeMethod("CreateInstanceFromPropertyData", inParams, null);
    }

    public bool RemoveRecord(string zone, string hostname,
                             string username, string password, string domain)
    {
        var scope = Connect(username, password, domain);
        var fqdn = hostname.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase) 
            ? hostname 
            : $"{hostname}.{zone}";
        
        var deleted = false;
        
        // Try deleting from A records
        var aQuery = new ObjectQuery($"SELECT * FROM MicrosoftDNS_AType WHERE ContainerName='{zone}' AND OwnerName='{fqdn}'");
        using (var aSearcher = new ManagementObjectSearcher(scope, aQuery))
        {
            foreach (ManagementObject obj in aSearcher.Get())
            {
                obj.Delete();
                deleted = true;
            }
        }
        
        // Try deleting from CNAME records
        var cnameQuery = new ObjectQuery($"SELECT * FROM MicrosoftDNS_CNAMEType WHERE ContainerName='{zone}' AND OwnerName='{fqdn}'");
        using (var cnameSearcher = new ManagementObjectSearcher(scope, cnameQuery))
        {
            foreach (ManagementObject obj in cnameSearcher.Get())
            {
                obj.Delete();
                deleted = true;
            }
        }

        return deleted;
    }

}