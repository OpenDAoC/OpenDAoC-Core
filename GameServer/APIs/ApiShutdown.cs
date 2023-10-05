using DOL.GS.Commands;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class ApiShutdown
{
    private IMemoryCache _cache;

    public ApiShutdown()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    #region Shutdown

    public bool ShutdownServer()
    {
        ShutdownCommand.CountDown(0); // Immediately shutdown server
        return true;
    }

    #endregion
}