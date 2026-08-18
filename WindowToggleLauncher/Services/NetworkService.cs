using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WindowToggleLauncher.Services;

public class NetworkService
{
    public static string GetLocalIpAddress()
    {
        try
        {
            var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            // First, prefer interfaces with a default gateway (usually active Wi-Fi or Ethernet connected to LAN router)
            var preferredInterfaces = activeInterfaces
                .Where(ni => ni.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)))
                .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 2 :
                                         ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 1 : 0)
                .ToList();

            foreach (var ni in preferredInterfaces.Concat(activeInterfaces))
            {
                // Exclude virtual/container network adapters if possible (Hyper-V, WSL, VirtualBox, Docker, etc.)
                var desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                if (desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("wsl") || 
                    desc.Contains("vethernet") || desc.Contains("vmware") || desc.Contains("virtualbox") ||
                    desc.Contains("docker") || desc.Contains("tailscale") || desc.Contains("zerotier"))
                {
                    continue;
                }

                var ipProps = ni.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses
                    .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                                          !IPAddress.IsLoopback(ua.Address) &&
                                          !ua.Address.ToString().StartsWith("169.254.")); // exclude APIPA

                if (ipv4 != null)
                {
                    return ipv4.Address.ToString();
                }
            }

            // Fallback: any non-loopback IPv4 address
            var fallback = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip) && !ip.ToString().StartsWith("169.254."));

            if (fallback != null)
            {
                return fallback.ToString();
            }
        }
        catch
        {
            // Ignore and fall back to localhost
        }

        return "127.0.0.1";
    }

    public static int FindAvailablePort(int preferredPort = 8765, int maxPortAttempts = 20)
    {
        for (int port = preferredPort; port < preferredPort + maxPortAttempts; port++)
        {
            if (IsPortAvailable(port))
            {
                return port;
            }
        }
        return preferredPort;
    }

    private static bool IsPortAvailable(int port)
    {
        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            listener?.Stop();
        }
    }
}
