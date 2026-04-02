using System;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using MiniToolBoxCross.Common.Enums;

namespace MiniToolBoxCross.Models.Entities;

public partial class ListenerModel : ModelBase
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _listenerNetInfo = string.Empty;

    public IPEndPoint ListenerNetIpEndPoint
    {
        get;
        set
        {
            field = value;
            ListenerNetInfo = value.ToString();
        }
    }

    public ListenerModel(Guid id, string name, IPEndPoint listenerNetIpEndPoint)
    {
        Id = id;
        ListenerNetIpEndPoint = listenerNetIpEndPoint;
        Name = name;
    }
}
