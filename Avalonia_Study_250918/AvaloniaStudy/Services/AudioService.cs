using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaStudy.Models;

namespace AvaloniaStudy.Services;

public class AudioService : IAudioService
{
    public Task<List<ChannelConfigItem>> GetChannelConfigsList() =>
        Task.FromResult(
            new List<ChannelConfigItem>()
            {
                new("Mono Stereo Configuration", "Mono", "Mono"),
                new("Mono Stereo Configuration", "Stereo", "Stereo"),
                new("5.1 Surround", "5.1 DTS - (L, R, Ls, Rs, C, LFE)", "5.1 DTS"),
                new("5.1 Surround", "5.1 DTS - (L, R, C, LFE, Ls, Rs)", "5.1 ITU"),
                new("5.1 Surround", "5.1 DTS - (L, C, R, Ls, Rs, LFE)", "5.1 FILM"),
            }
        );
}
