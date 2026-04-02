using System;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MiniToolBoxCross.Models.Entities;

public partial class LocalSocketModel : ModelBase
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _localNetInfo = string.Empty;

    [ObservableProperty]
    private string _key;

    public IPEndPoint LocalIpEndPoint
    {
        get;
        set
        {
            field = value;
            LocalNetInfo = value.ToString();
        }
    }

    public LocalSocketModel(Guid id, string name, string key, IPEndPoint localIpEndPoint)
    {
        Id = id;
        Name = name;
        Key = key;
        LocalIpEndPoint = localIpEndPoint;
    }
}
