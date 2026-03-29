using System;
using MiniToolBoxCross.Common.Enums;

namespace MiniToolBoxCross.Common.Global;

public class CrossSetting
{
    public OsPlatformType OSsPlatformType { get; }

    public CrossSetting()
    {
        if (OperatingSystem.IsWindows())
            OSsPlatformType = OsPlatformType.Windows;
        else if (OperatingSystem.IsLinux())
            OSsPlatformType = OsPlatformType.Linux;
        else if (OperatingSystem.IsAndroid())
            OSsPlatformType = OsPlatformType.Android;
        else if (OperatingSystem.IsMacOS())
            OSsPlatformType = OsPlatformType.Mac;
        else
            throw new Exception("Unknown OS Platform");
    }
}
