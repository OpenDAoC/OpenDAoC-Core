using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.GS;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

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
                await c.Response.WriteAsJsonAsync(WorldMgr.GetAllClientsCount()));

            app.MapGet("/json", async c =>
                await c.Response.WriteAsJsonAsync(GetPlayers()));
            
            app.MapGet("/longshot", async c =>
                await c.Response.WriteAsJsonAsync(WorldMgr.GetAllClients().Select(x => x.Player.Realm == eRealm.Albion ? "Albion" : "Midgard")));

            app.Run();
        }
        
        public class RealmCount
        {
            public string name;
            public int count;

            public RealmCount(string name, int count)
            {
                this.name = name;
                this.count = count;
            }
        }

        private List<RealmCount> realmCount = new List<RealmCount>();

        public IList<string> GetPlayers()
        {

            {
                IList<GameClient> clients = WorldMgr.GetAllClients();
                IList<string> output = new List<string>();
                
                realmCount.Clear();
                realmCount.Add(new RealmCount("Albion", 0)); //0
                realmCount.Add(new RealmCount("Midgard", 0)); //1
                realmCount.Add(new RealmCount("Hibernia", 0)); //2
                realmCount.Add(new RealmCount("Total", 0)); //3

                foreach (GameClient c in clients)
                {
                    if (c == null)
                        continue;

                    #region realm specific counting

                    switch (c.Player.Realm)
                    {
                        case eRealm.Albion:
                            realmCount[0].count++;
                            realmCount[3].count++;
                            break;
                        case eRealm.Midgard:
                            realmCount[1].count++;
                            realmCount[3].count++;
                            break;
                        case eRealm.Hibernia:
                            realmCount[2].count++;
                            realmCount[3].count++;
                            break;
                        default:
                            realmCount[3].count++;
                            break;
                    }

                    #endregion
                }
                for (int c = 0; c < realmCount.Count; c++)
                {
                    output.Add(string.Format("{0}: {1}", realmCount[c].name, realmCount[c].count.ToString()));
                }
                return output;
                // return realmCount;
            }
        }
    }
}
        
