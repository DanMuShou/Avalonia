using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MiniToolBoxCross.Common.Helper;

public static class SocketHelper
{
    public static List<IPAddress> GetIpAddressList(AddressFamily addressFamily)
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ua => ua.Address.AddressFamily == addressFamily)
            .Select(ua => ua.Address)
            .Where(ip => !IPAddress.IsLoopback(ip))
            .Reverse()
            .ToList();
    }

    public static int? CheckPort(int port)
    {
        var usedPorts = GetAllUsedPort();
        return !usedPorts.Contains(port) ? port : GetRandomAvailablePort();
    }

    public static int? GetRandomAvailablePort()
    {
        var usedPorts = GetAllUsedPort();
        var random = new Random();

        for (var i = 0; i < 100; i++)
        {
            var randomPort = random.Next(1024, 65535);
            if (!usedPorts.Contains(randomPort))
                return randomPort;
        }

        for (var port = 1024; port < 65535; port++)
        {
            if (!usedPorts.Contains(port))
                return port;
        }

        return null;
    }

    private static List<int> GetAllUsedPort()
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();

        var tcpListeners = properties.GetActiveTcpListeners();
        var tcpConnections = properties.GetActiveTcpConnections();

        var udpListeners = properties.GetActiveUdpListeners();

        const int minPort = 1024;
        const int maxPort = 65535;

        var usedPorts = new HashSet<int>();

        foreach (var endpoint in tcpListeners.Where(q => q.Port is >= minPort and <= maxPort))
        {
            usedPorts.Add(endpoint.Port);
        }

        foreach (
            var conn in tcpConnections.Where(q => q.LocalEndPoint.Port is >= minPort and <= maxPort)
        )
        {
            usedPorts.Add(conn.LocalEndPoint.Port);
        }

        foreach (var endpoint in udpListeners.Where(q => q.Port is >= minPort and <= maxPort))
        {
            usedPorts.Add(endpoint.Port);
        }

        return [.. usedPorts];
    }
}
