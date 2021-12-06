using System;
using System.Collections.Generic;
using DOL.GS;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Xml;

namespace DOL.GS.API
{
    internal class ApiHost
    {
        public ApiHost()
        {
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
            
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            

            var playerCount = new PlayerCount
            {
                Albion = Albion,
                Midgard = Midgard,
                Hibernia = Hibernia,
                Total = Total
            };

            string jsonString = JsonSerializer.Serialize(playerCount,options);
            return jsonString;
        }
        
        
    }
}
        
