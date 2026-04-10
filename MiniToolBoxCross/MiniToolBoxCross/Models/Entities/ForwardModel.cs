using System;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using MiniToolBoxCross.Common.Enums;

namespace MiniToolBoxCross.Models.Entities;

public partial class ForwardModel : ModelBase
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ForwardTargetType _forwardTargetType;

    [ObservableProperty]
    private string _forwardNetInfo = string.Empty;

    public Guid Id { get; set; }

    public IPEndPoint IPEndPoint
    {
        get;
        set
        {
            field = value;
            ForwardNetInfo = IPEndPoint + " - " + ForwardTargetType;
        }
    }

    public ForwardModel(
        string name,
        ForwardTargetType forwardTargetType,
        IPEndPoint forwardNetIpEndPoint
    )
    {
        Name = name;
        ForwardTargetType = forwardTargetType;
        IPEndPoint = forwardNetIpEndPoint;
    }
}
