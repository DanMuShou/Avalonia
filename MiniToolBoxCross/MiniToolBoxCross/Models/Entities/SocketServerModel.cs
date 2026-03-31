using System;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using MiniToolBoxCross.Common.Enums;

namespace MiniToolBoxCross.Models.Entities;

public partial class SocketServerModel : ModelBase
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _serverNetInfo = string.Empty;

    [ObservableProperty]
    private string _localNetInfo = string.Empty;

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRunning;

    public SocketConfigureType ConfigureType { get; set; } = SocketConfigureType.Communication;

    public IPEndPoint ServerIpEndPoint
    {
        get;
        set
        {
            field = value;
            ServerNetInfo = value.ToString();
        }
    }

    public IPEndPoint LocalIpEndPoint
    {
        get;
        set
        {
            field = value;
            LocalNetInfo = value.ToString();
        }
    }

    public SocketServerModel(
        Guid id,
        string name,
        IPEndPoint serverIpEndPoint,
        IPEndPoint localIpEndPoint
    )
    {
        Id = id;
        ServerIpEndPoint = serverIpEndPoint;
        LocalIpEndPoint = localIpEndPoint;
        Name = name;
    }
}
