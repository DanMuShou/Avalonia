using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaStudy.Models;

namespace AvaloniaStudy.Services;

/// <summary>
/// 音频服务接口，定义了音频相关的服务方法
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// 获取通道配置列表
    /// </summary>
    /// <returns>返回通道配置项列表的任务</returns>
    Task<List<ChannelConfigItem>> GetChannelConfigsList();
}
