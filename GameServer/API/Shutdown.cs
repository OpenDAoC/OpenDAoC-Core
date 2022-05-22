using DOL.GS.Commands;
using DOL.GS.ServerProperties;
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

    public bool ShutdownServer(string password)
    {
        var apiPassword = Properties.API_PASSWORD;
        if (apiPassword is (null or "")) return false;
        if (password is (null or "")) return false;
        if (password != apiPassword) return false;
        ShutdownCommandHandler.CountDown(0); // Immediately shutdown server
        return true;
    }

    #endregion
}