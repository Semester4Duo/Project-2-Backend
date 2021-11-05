using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class CallHub : Hub
{
    public CallHub(CallCache cache)
    {
        Cache = cache;
    }

    public CallCache Cache { get; }

    public override async Task OnConnectedAsync()
    {
        foreach (var pair in Cache.Calls.Where(pair => pair.Value == false))
        {
            await Clients.Caller.SendAsync("HostConference", pair.Key);
        }
        await base.OnConnectedAsync();
    }

    public async Task<bool> HostCall(string conferenceId)
    {
        if (!Cache.Calls.TryAdd(conferenceId, true)) return false;
        await Clients.Others.SendAsync("HostConference", conferenceId);
        return true;
    }

    public Task<bool> JoinCall(string conferenceId)
    {
        return Task.FromResult(Cache.Calls.TryUpdate(conferenceId, true, false));
    }
    
    public async Task EndCall(string conferenceId)
    {
        if (Cache.Calls.TryRemove(conferenceId, out var hasJoined))
        {
            if (!hasJoined)
            {
                await Clients.All.SendAsync("EndConference", conferenceId);
            }
        }
    }
}

public class CallCache
{
    public ConcurrentDictionary<string, bool> Calls { get; } = new();
}