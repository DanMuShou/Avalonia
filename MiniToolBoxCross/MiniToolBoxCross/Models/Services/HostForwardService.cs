using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniToolBoxCross.Common.Enums;
using MiniToolBoxCross.Common.Helper;
using MiniToolBoxCross.Models.Repositories;
using MiniToolBoxCross.Services;
using MiniToolBoxCross.Services.Commands;
using Serilog;
using SuperSocket.Command;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Host;

namespace MiniToolBoxCross.Models.Services;

public class HostForwardService(
    ILogger<HostForwardService> logger,
    IServiceProvider serviceProvider
) : IHostForwardService
{
    private ISocketService _socketService = null!;

    private readonly ConcurrentDictionary<Guid, EchoClient> _forwardClients = [];
    private readonly ConcurrentDictionary<string, HashSet<EchoClient>> _forwardTargets = [];
    private readonly Lock _targetsLock = new();

    public bool IsConfigured { get; private set; }
    public bool IsStarted { get; private set; }

    public async Task<bool> ConfigAsync(IPEndPoint endPoint)
    {
        if (IsConfigured)
        {
            logger.LogWarning("主机已经配置过");
            return true;
        }

        if (!SocketHelper.GetIpAddressList(AddressFamily.InterNetworkV6).Contains(endPoint.Address))
        {
            logger.LogError("服务器地址未找到: {Address}", endPoint.Address);
            return false;
        }

        var validPort = SocketHelper.CheckServerPortValidation(endPoint.Port);
        if (!validPort.HasValue)
        {
            logger.LogError("没有找到服务器可用的网络端口: {Port}", endPoint.Port);
            return false;
        }

        var listenOptions = new ListenOptions
        {
            Ip = endPoint.Address.ToString(),
            Port = validPort.Value,
        };

        try
        {
            if (_socketService is not null)
                await _socketService.DisposeAsync();

            var socketHostBuilder = SuperSocketHostBuilder
                .Create<StringPackageInfo, CommandLinePipelineFilter>()
                .UseHostedService<SocketService<StringPackageInfo>>()
                .UseSession<SocketSession>()
                .ConfigureSuperSocket(options => options.AddListener(listenOptions))
                .UseCommand(commandOptions =>
                {
                    commandOptions.AddCommand<GameCommand>();
                })
                .UseDefaultServiceProvider(
                    (hostContext, options) =>
                    {
                        options.ValidateScopes = hostContext.HostingEnvironment.IsDevelopment();
                        options.ValidateOnBuild = true;
                    }
                )
                .ConfigureServices(services =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Debug);
                    });

                    services.AddSingleton(_ =>
                        serviceProvider.GetRequiredService<IHostForwardService>()
                    );
                })
                .ConfigureLogging(
                    (_, loggingBuilder) =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddSerilog(dispose: true);
                    }
                );

            _socketService =
                socketHostBuilder.BuildAsServer() as ISocketService
                ?? throw new InvalidOperationException("Server is not ISocketService.");

            IsConfigured = true;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "主机配置失败: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> StartAsync()
    {
        if (!IsConfigured)
        {
            logger.LogError("主机未配置，无法启动");
            return false;
        }

        if (IsStarted)
        {
            logger.LogWarning("主机已经在运行中");
            return true;
        }

        try
        {
            foreach (var client in _forwardClients.Values)
                client.Connect();
            await _socketService.StartAsync();
            IsStarted = true;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "主机启动失败: {Message}", ex.Message);
            IsStarted = false;
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        if (!IsConfigured)
        {
            logger.LogError("主机未配置，无法停止");
            return false;
        }

        if (!IsStarted)
        {
            logger.LogWarning("主机未在运行");
            return true;
        }

        try
        {
            foreach (var client in _forwardClients.Values)
                client.Disconnect();
            await _socketService.StopAsync();
            IsStarted = false;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "主机停止失败: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> RestartAsync()
    {
        if (!IsConfigured)
        {
            logger.LogError("主机未配置，无法重启");
            return false;
        }

        if (!IsStarted)
        {
            logger.LogWarning("主机未在运行，无法重启");
            return true;
        }

        try
        {
            await StopAsync();
            await StartAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "主机重启失败: {Message}", ex.Message);
            return false;
        }
    }

    public EchoClient? AddForwardClientAsync(
        IPEndPoint endPoint,
        ForwardTargetType forwardTargetType
    )
    {
        if (IsStarted)
        {
            logger.LogError("主机正在运行，无法添加转发客户端");
            return null;
        }

        if (_forwardClients.Values.Any(client => client.Endpoint.Equals(endPoint)))
        {
            logger.LogWarning("转发客户端已存在: {EndPoint}", endPoint);
            return null;
        }

        var validPort = SocketHelper.CheckClientPortValidation(endPoint.Port);
        if (!validPort.HasValue)
        {
            logger.LogError("没有找到转发客户端可用的网络端口: {Port}", endPoint.Port);
            return null;
        }
        var validIpEndPoint = new IPEndPoint(endPoint.Address, validPort.Value);

        try
        {
            var client = new EchoClient(validIpEndPoint, forwardTargetType);
            _forwardClients.TryAdd(client.Id, client);

            lock (_targetsLock)
            {
                if (!_forwardTargets.TryGetValue(client.ForwardTargetType, out var clients))
                {
                    clients = [];
                    _forwardTargets[client.ForwardTargetType] = clients;
                }
                clients.Add(client);
            }

            client.ErrorOccurred += async error =>
            {
                logger.LogError("转发客户端发生错误: {Error}", error);
            };

            client.DataReceived += (buffer, offset, size) =>
            {
                _socketService.BroadcastAsync(
                    client.ForwardTargetType,
                    buffer,
                    (int)offset,
                    (int)size
                );
            };

            return client;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加转发客户端失败: {Message}", ex.Message);
            return null;
        }
    }

    public bool RemoveForwardClientAsync(Guid id)
    {
        if (IsStarted)
        {
            logger.LogError("主机正在运行，无法移除转发客户端");
            return false;
        }

        if (!_forwardClients.TryRemove(id, out var client))
        {
            logger.LogWarning("转发客户端不存在: {id}", id);
            return false;
        }

        try
        {
            if (_forwardTargets.TryGetValue(client.ForwardTargetType, out var clients))
            {
                lock (_targetsLock)
                {
                    clients.Remove(client);
                    if (clients.Count == 0)
                        _forwardTargets.TryRemove(client.ForwardTargetType, out _);
                }
            }
            client.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "移除转发客户端失败: {Id}, {Message}", id, ex.Message);
            return false;
        }
    }

    public void Send(string forwardTargetType, string message)
    {
        HashSet<EchoClient>? clients;
        lock (_targetsLock)
        {
            _forwardTargets.TryGetValue(forwardTargetType, out clients);
        }

        if (clients == null || clients.Count == 0)
            return;

        EchoClient[] snapshot;
        lock (_targetsLock)
        {
            snapshot = [.. clients];
        }

        foreach (var client in snapshot)
        {
            try
            {
                client.SendAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "转发客户端 {ClientId} 发送消息失败", client.Id);
            }
        }
    }

    public async Task<bool> DisposeAsync()
    {
        if (!IsConfigured)
        {
            logger.LogWarning("主机未配置，无需释放");
            return true;
        }

        try
        {
            foreach (var client in _forwardClients.Values)
                client.Dispose();
            _forwardClients.Clear();
            _forwardTargets.Clear();

            await _socketService.DisposeAsync();
            IsConfigured = false;
            IsStarted = false;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "主机资源释放失败: {Message}", ex.Message);
            return false;
        }
    }
}
