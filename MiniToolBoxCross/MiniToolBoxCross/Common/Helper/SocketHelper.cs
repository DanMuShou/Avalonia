using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MiniToolBoxCross.Common.Helper;

public static class SocketHelper
{
    private const int MinPort = 1024;
    private const int MaxPort = 65535;

    public static List<IPAddress> GetIpAddressList(AddressFamily addressFamily)
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ua => ua.Address.AddressFamily == addressFamily)
            .Select(ua => ua.Address)
            .Where(ip =>
                ip is { IsIPv6LinkLocal: false, IsIPv6SiteLocal: false }
                && !IPAddress.IsLoopback(ip)
            )
            .Reverse()
            .ToList();
    }

    public static List<IPAddress> GetIpv6AddressList()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
            .Select(ua => ua.Address)
            .Where(ip =>
                ip is { IsIPv6LinkLocal: false, IsIPv6SiteLocal: false }
                && !IPAddress.IsLoopback(ip)
            )
            .Reverse()
            .ToList();
    }

    public static List<IPAddress> GetIpv4AddressList()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(ua => ua.Address)
            .Reverse()
            .ToList();
    }

    public static int? CheckPort(int port)
    {
        var usedPorts = GetAllUsedPort();
        if (port is < MinPort or > MaxPort)
            return GetRandomAvailablePort();
        return !usedPorts.Contains(port) ? port : GetRandomAvailablePort();
    }

    public static int? GetRandomAvailablePort()
    {
        var usedPorts = GetAllUsedPort();
        var random = new Random();

        for (var i = 0; i < 100; i++)
        {
            var randomPort = random.Next(MinPort, MaxPort);
            if (!usedPorts.Contains(randomPort))
                return randomPort;
        }

        for (var port = MinPort; port < MaxPort; port++)
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
        var udpListeners = properties.GetActiveUdpListeners();
        var tcpConnections = properties.GetActiveTcpConnections();

        var usedPorts = new HashSet<int>();

        foreach (var endpoint in tcpListeners)
            usedPorts.Add(endpoint.Port);

        foreach (var conn in tcpConnections)
            usedPorts.Add(conn.LocalEndPoint.Port);

        foreach (var endpoint in udpListeners)
            usedPorts.Add(endpoint.Port);

        return [.. usedPorts];
    }
}
