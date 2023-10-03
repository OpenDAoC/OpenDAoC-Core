using DOL.GS.Commands;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Shutdown
{
    private IMemoryCache _cache;

    public Shutdown()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    #region Shutdown

    public bool ShutdownServer()
    {
        ShutdownCommandHandler.CountDown(0); // Immediately shutdown server
        return true;
    }

    #endregion
}