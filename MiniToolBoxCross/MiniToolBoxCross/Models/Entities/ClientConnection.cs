using System;

namespace MiniToolBoxCross.Models.Entities;

public class ClientConnection
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? IpAddress { get; set; }
    public int Port { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.Now;
    public bool IsConnected { get; set; }
}
