# Windows Active Directory Web

A web-based interface for managing Active Directory DNS records and Users.

## Functionality

The application provides a centralized web interface to perform common Active Directory administrative tasks:

### DNS Management
- **List Records**: View all A-type (Host) records for a specific DNS zone.
- **Add Records**: Create new A-type records by providing a hostname and IP address.
- **Remove Records**: Delete existing DNS records.
- **Filtering & Sorting**: Search through records and sort them by hostname, IP address, or TTL.

### User Management
- **Create User**: Add new users to Active Directory with Login Name, First Name, Last Name, and Password.
- **Change Password**: Update the password for any existing user.
- **Remove User**: Delete a user account from Active Directory.
- **Group Membership**: Add or remove users from Active Directory groups (e.g., Domain Admins, IT Department).
- **Group Selection**: Browse and select from existing groups using a dynamic dropdown.

### Security & Architecture
- **Authentication**: Secure login using Active Directory credentials.
- **Session Management**: Secure cookie-based sessions with a 30-minute idle timeout.
- **CSRF Protection**: Built-in protection against Cross-Site Request Forgery for all state-changing API calls.
- **Mock Mode**: A full mock implementation for development and testing without a live Active Directory connection (enabled in `DEBUG` builds).

## Development & Mocking

The project includes a full mock implementation of all Active Directory and DNS services. This allows for development and testing without requiring a live connection to a Domain Controller.

- **Automatic Activation**: The mock services are automatically enabled when the application is built in `DEBUG` configuration (via conditional compilation in `Program.cs`).
- **In-Memory Storage**: All changes made in mock mode (adding DNS records, creating users, changing group memberships) are stored in-memory and will be reset when the application restarts.
- **Authentication Bypass**: In `DEBUG` mode, the `AuthController` accepts any username/password combination as valid to simplify testing.
- **Pre-populated Data**: The mock services come pre-populated with sample DNS zones (`example.com`), records, and groups (`Domain Admins`, `IT Department`) to provide an immediate working environment.

## Settings

Configuration is managed via `appsettings.json` under the `ApplicationSettings` section.

| Setting | Description | Example |
| :--- | :--- | :--- |
| `AllowedHosts` | Standard ASP.NET Core host filtering setting. Limits which hostnames can access the application. | `"*"` |
| `ServerName` | The FQDN or IP address of the DNS server (usually a Domain Controller) to manage. | `"dc01.example.com"` |
| `ActiveDirectoryDomain` | The Active Directory domain name used for authentication and service contexts. | `"example.com"` |
| `UsersAdPath` | An array of LDAP paths (OUs or Containers) where user management operations (Create/Remove) will be performed. The first entry is used as default in the UI. | `["CN=Users,DC=example,DC=com", "OU=Service Accounts,DC=example,DC=com"]` |
| `GroupsAdPath` | An array of LDAP paths (OUs or Containers) where groups will be fetched from (in addition to `UsersAdPath`). | `["CN=Users,DC=example,DC=com"]` |
| `DnsZones` | List of DNS zones available in the UI dropdown for DNS record management. | `["example.com", "corp.example.com"]` |

### Example `appsettings.json`
```json
{
  "AllowedHosts": "*",
  "ApplicationSettings": {
    "ServerName": "dc01.example.com",
    "ActiveDirectoryDomain": "example.com",
    "UsersAdPath": ["CN=Users,DC=example,DC=com", "OU=Service Accounts,DC=example,DC=com"],
    "GroupsAdPath": ["CN=Users,DC=example,DC=com"],
    "DnsZones": ["example.com"]
  }
}
```

## IIS Deployment

To publish and host the application on IIS, follow these steps. This guide assumes you are deploying to a Windows Server that has network access to your Domain Controller.

### 1. Prerequisites on the IIS Server
Before deploying, ensure the target server has the following installed:
*   **IIS (Internet Information Services)**: Enabled via "Turn Windows features on or off".
*   **.NET 10 Hosting Bundle**: Download and install the [ASP.NET Core Runtime 10.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0). This includes the .NET Runtime and the IIS Support (ASP.NET Core Module).
*   **Restart IIS**: After installing the hosting bundle, run `iisreset` in an admin command prompt to ensure the new module is recognized.

### 2. Prepare the Application (Publish)
You need to create a "Release" build of the application. Run the following command from the project root:

```powershell
dotnet publish src\Bergdahl.ActiveDirectory.Web\Bergdahl.ActiveDirectory.Web.csproj -c Release -o C:\inetpub\ad-web
```
*(Replace `C:\inetpub\ad-web` with your desired destination folder)*.

### 3. Setup IIS Site
1.  Open **IIS Manager**.
2.  Right-click **Sites** > **Add Website**.
3.  **Site name**: `AD-Web` (or your choice).
4.  **Physical path**: Point to your publish folder (e.g., `C:\inetpub\ad-web`).
5.  **Binding**: Set your desired port (e.g., `80` or `443` with SSL).
6.  Click **OK**.

### 4. Application Pool Configuration
1.  Go to **Application Pools** in IIS Manager.
2.  Find the pool for your site (e.g., `AD-Web`).
3.  Right-click > **Basic Settings**. Ensure **.NET CLR Version** is set to **No Managed Code**.
4.  Right-click > **Advanced Settings**.
    *   **Identity**: Since the app uses WMI and AD queries, it is recommended to run the Application Pool as a **Service Account** that has administrative permissions in Active Directory and on the DNS Server (WMI access). 

### 5. Permissions & Security
*   **WMI/DNS Access**: The application uses WMI to manage DNS records. Ensure the account running the app pool has permissions to the `root\MicrosoftDNS` namespace on the target DNS server.
*   **SSL/HTTPS**: It is **strongly recommended** to bind the site to HTTPS (port 443) because the application handles Active Directory credentials. 
*   **Folder Permissions**: Ensure the Application Pool Identity has **Read** permissions to the physical folder (`C:\inetpub\ad-web`).

## Project Structure
- **Bergdahl.ActiveDirectory.Core**: The core library containing models, interfaces, and the real Active Directory implementation using `System.Management` (WMI) and `System.DirectoryServices.AccountManagement`.
- **Bergdahl.ActiveDirectory.Mock**: An in-memory mock implementation of the services for development.
- **Bergdahl.ActiveDirectory.Web**: The ASP.NET Core web application (API and Static Files).