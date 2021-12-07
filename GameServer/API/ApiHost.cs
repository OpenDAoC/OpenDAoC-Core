using System;
using System.Collections.Generic;
using DOL.GS;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Xml;

using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API
{
    internal class ApiHost
    {
        private IMemoryCache _cache;
        private const string _playerCountCacheKey = "api_player_count";

        public ApiHost()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());

            var builder = WebApplication.CreateBuilder();

            var app = builder.Build();

            app.MapGet("/hello", () => "Hello World");

            app.MapGet("/players", async c =>
                await c.Response.WriteAsync(GetPlayers()));
            
            app.Run();
        }
        
        public class PlayerCount
        {
            public int Albion {get; set;}
            public int Midgard {get; set;}
            public int Hibernia {get; set;}
            public int Total {get; set;}
        }

        
        public string GetPlayers()
        {
            if (!_cache.TryGetValue(_playerCountCacheKey, out PlayerCount playerCount))
            {
                IList<GameClient> clients = WorldMgr.GetAllClients();
                int Albion = 0, Midgard = 0, Hibernia = 0, Total = 0;

                foreach (GameClient c in clients)
                {
                    if (c == null)
                        continue;

                    #region realm specific counting

                    switch (c.Player.Realm)
                    {
                        case eRealm.Albion:
                            Albion++;
                            Total++;
                            break;
                        case eRealm.Midgard:
                            Midgard++;
                            Total++;
                            break;
                        case eRealm.Hibernia:
                            Hibernia++;
                            Total++;
                            break;
                        default:
                            Total++;
                            break;
                    }

                    #endregion
                }

                playerCount = new PlayerCount
                {
                    Albion = Albion,
                    Midgard = Midgard,
                    Hibernia = Hibernia,
                    Total = Total
                };

                _cache.Set(_playerCountCacheKey, playerCount, DateTime.Now.AddMinutes(1));
            }

            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };


            string jsonString = JsonSerializer.Serialize(playerCount,options);
            return jsonString;
        }
        
        
    }
}
        
