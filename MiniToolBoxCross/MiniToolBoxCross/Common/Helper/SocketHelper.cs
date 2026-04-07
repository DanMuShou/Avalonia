using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MiniToolBoxCross.Common.Helper;

public static class SocketHelper
{
    #region IPADDRESS

    private const int MinPort = 1024;
    private const int MaxPort = 65535;

    public static IEnumerable<IPAddress> GetIpAddressList()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ua =>
                ua.Address.AddressFamily
                    is AddressFamily.InterNetwork
                        or AddressFamily.InterNetworkV6
            )
            .Select(ua => ua.Address);
    }

    public static IEnumerable<IPAddress> GetIpAddressList(AddressFamily addressFamily) =>
        GetIpAddressList().Where(ip => ip.AddressFamily == addressFamily);

    public static IEnumerable<IPAddress> GetIpAddressList(bool isLocal)
    {
        return GetIpAddressList()
            .Where(ip =>
            {
                if (isLocal)
                {
                    return ip.IsIPv6LinkLocal
                        || ip.IsIPv6SiteLocal
                        || ip.IsIPv6UniqueLocal
                        || IPAddress.IsLoopback(ip);
                }

                return ip
                        is {
                            IsIPv6LinkLocal: false,
                            IsIPv6SiteLocal: false,
                            IsIPv6UniqueLocal: false
                        }
                    && !IPAddress.IsLoopback(ip);
            });
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
        var tcpConnections = properties.GetActiveTcpConnections();

        var usedPorts = new HashSet<int>();

        foreach (var endpoint in tcpListeners)
            usedPorts.Add(endpoint.Port);

        foreach (var conn in tcpConnections)
            usedPorts.Add(conn.LocalEndPoint.Port);

        return [.. usedPorts];
    }

    public static int? CheckClientPortValidation(int port) =>
        port is >= MinPort and <= MaxPort ? port : null;

    public static int? CheckServerPortValidation(int port)
    {
        var usedPorts = GetAllUsedTcpPorts();
        usedPorts.AddRange(GetAllUsedUdpPorts());

        if (port is < MinPort or > MaxPort)
            return GetRandomAvailablePort();
        return !usedPorts.Contains(port) ? port : GetRandomServerPort();
    }

    private static int? GetRandomServerPort()
    {
        var usedPorts = GetAllUsedTcpPorts();
        usedPorts.AddRange(GetAllUsedUdpPorts());

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

    private static List<int> GetAllUsedTcpPorts()
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpListeners = properties.GetActiveTcpListeners();
        var tcpConnections = properties.GetActiveTcpConnections();

        var usedPorts = new HashSet<int>();

        foreach (var endpoint in tcpListeners)
            usedPorts.Add(endpoint.Port);

        foreach (var conn in tcpConnections)
            usedPorts.Add(conn.LocalEndPoint.Port);

        return [.. usedPorts];
    }

    private static List<int> GetAllUsedUdpPorts()
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var udpListeners = properties.GetActiveUdpListeners();

        var usedPorts = new HashSet<int>();

        foreach (var endpoint in udpListeners)
            usedPorts.Add(endpoint.Port);

        return [.. usedPorts];
    }

    #endregion

    #region SUPERSOCKET

    // public static ISocketService BuildSocketServer(
    //     ListenOptions listenOptions,
    //     IServiceProvider serviceProvider
    // )
    // {
    //     var socketHostBuilder = SuperSocketHostBuilder
    //         .Create<StringPackageInfo, CommandLinePipelineFilter>()
    //         .UseHostedService<SocketService<StringPackageInfo>>()
    //         .UseSession<SocketSession>()
    //         .ConfigureSuperSocket(options => options.AddListener(listenOptions))
    //         .UseCommand(commandOptions =>
    //         {
    //             commandOptions.AddCommand<GameCommand>();
    //         })
    //         .UseDefaultServiceProvider(
    //             (hostContext, options) =>
    //             {
    //                 options.ValidateScopes = hostContext.HostingEnvironment.IsDevelopment();
    //                 options.ValidateOnBuild = true;
    //             }
    //         )
    //         .ConfigureServices(services =>
    //         {
    //             services.AddLogging(builder =>
    //             {
    //                 builder.SetMinimumLevel(LogLevel.Debug);
    //             });

    //             services.AddSingleton(_ =>
    //                 serviceProvider.GetRequiredService<IForwardClientManager>()
    //             );
    //             services.AddSingleton(_ =>
    //                 serviceProvider.GetRequiredService<INotificationService>()
    //             );
    //         })
    //         .ConfigureLogging(
    //             (_, loggingBuilder) =>
    //             {
    //                 loggingBuilder.ClearProviders();
    //                 loggingBuilder.AddSerilog(dispose: true);
    //             }
    //         );

    //     return socketHostBuilder.BuildAsServer() as ISocketService
    //         ?? throw new InvalidOperationException("Server is not StringPackageInfo.");
    // }

    public static ReadOnlyMemory<byte> StringPackInfoMessageEncoder(
        string key,
        string target,
        string body
    )
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var targetBytes = Encoding.UTF8.GetBytes(target);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        var totalLength = keyBytes.Length + targetBytes.Length + bodyBytes.Length + 4;
        var result = new byte[totalLength];

        var offset = 0;

        keyBytes.CopyTo(result, offset);
        offset += keyBytes.Length;

        result[offset++] = (byte)' ';

        targetBytes.CopyTo(result, offset);
        offset += targetBytes.Length;

        result[offset++] = (byte)' ';

        bodyBytes.CopyTo(result, offset);
        offset += bodyBytes.Length;

        result[offset++] = (byte)'\r';
        result[offset] = (byte)'\n';

        return result;
    }

    public static ReadOnlyMemory<byte> StringPackInfoMessageEncoder(
        string key,
        string target,
        byte[] buffer,
        long offset,
        long size
    )
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var targetBytes = Encoding.UTF8.GetBytes(target);
        var sizeInt = (int)size;

        var totalLength = keyBytes.Length + targetBytes.Length + sizeInt + 4;
        var result = new byte[totalLength];

        var pos = 0;

        keyBytes.CopyTo(result, pos);
        pos += keyBytes.Length;

        result[pos++] = (byte)' ';

        targetBytes.CopyTo(result, pos);
        pos += targetBytes.Length;

        result[pos++] = (byte)' ';

        Buffer.BlockCopy(buffer, (int)offset, result, pos, sizeInt);
        pos += sizeInt;

        result[pos++] = (byte)'\r';
        result[pos] = (byte)'\n';

        return result;
    }

    public static ReadOnlyMemory<byte> StringPackInfoMessageEncoder(
        string key,
        byte[] buffer,
        long offset,
        long size
    )
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var sizeInt = (int)size;

        var totalLength = keyBytes.Length + sizeInt + 3;
        var result = new byte[totalLength];

        var pos = 0;

        keyBytes.CopyTo(result, pos);
        pos += keyBytes.Length;

        result[pos++] = (byte)' ';

        Buffer.BlockCopy(buffer, (int)offset, result, pos, sizeInt);
        pos += sizeInt;

        result[pos++] = (byte)'\r';
        result[pos] = (byte)'\n';

        return result;
    }

    #endregion
}
