namespace Bergdahl.ActiveDirectory.Mock;

using Bergdahl.ActiveDirectory.Core.ActiveDirectory;
using Bergdahl.ActiveDirectory.Core.Models;

public class MockDnsWmiService : IDnsWmiService
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<DnsRecord>> _zones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["example.com"] = new List<DnsRecord>
        {
            new DnsRecord { HostName = "www.example.com", IpAddress = "192.168.1.10", RecordType = "A", TTL = 3600 },
            new DnsRecord { HostName = "api.example.com", IpAddress = "192.168.1.20", RecordType = "A", TTL = 3600 }
        }
    };

    public List<DnsRecord> GetRecords(string zone, string username, string password, string domain)
    {
        lock (_lock)
        {
            if (_zones.TryGetValue(zone, out var list))
            {
                // Return copies to avoid external mutation
                return list.Select(r => new DnsRecord
                {
                    HostName = r.HostName,
                    IpAddress = r.IpAddress,
                    RecordType = r.RecordType,
                    TTL = r.TTL
                }).ToList();
            }
            return new List<DnsRecord>();
        }
    }

    public void AddRecord(string zone, string hostname, string ip, string username, string password, string domain)
    {
        lock (_lock)
        {
            if (!_zones.TryGetValue(zone, out var list))
            {
                list = new List<DnsRecord>();
                _zones[zone] = list;
            }

            var fqdn = hostname.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase) 
                ? hostname 
                : $"{hostname}.{zone}";
            // Replace if exists
            var existing = list.FirstOrDefault(r => string.Equals(r.HostName, fqdn, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.IpAddress = ip;
                existing.RecordType = "A";
                existing.TTL = 3600;
            }
            else
            {
                list.Add(new DnsRecord { HostName = fqdn, IpAddress = ip, RecordType = "A", TTL = 3600 });
            }
        }
    }

    public void AddCnameRecord(string zone, string aliasName, string targetHost, string username, string password, string domain)
    {
        lock (_lock)
        {
            if (!_zones.TryGetValue(zone, out var list))
            {
                list = new List<DnsRecord>();
                _zones[zone] = list;
            }

            var fqdn = aliasName.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase) 
                ? aliasName 
                : $"{aliasName}.{zone}";
            // Replace if exists
            var existing = list.FirstOrDefault(r => string.Equals(r.HostName, fqdn, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.IpAddress = targetHost;
                existing.RecordType = "CNAME";
                existing.TTL = 3600;
            }
            else
            {
                list.Add(new DnsRecord { HostName = fqdn, IpAddress = targetHost, RecordType = "CNAME", TTL = 3600 });
            }
        }
    }

    public bool RemoveRecord(string zone, string hostname, string username, string password, string domain)
    {
        lock (_lock)
        {
            if (_zones.TryGetValue(zone, out var list))
            {
                var fqdn = hostname.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase) 
                    ? hostname 
                    : $"{hostname}.{zone}";
                var removedCount = list.RemoveAll(r => string.Equals(r.HostName, fqdn, StringComparison.OrdinalIgnoreCase));
                return removedCount > 0;
            }
            return false;
        }
    }
}
