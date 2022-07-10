using System;
using System.Collections.Generic;
using System.IO;
using DOL.GS.ServerProperties;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace DOL.GS.API
{
    internal class ApiHost
    {
        public ApiHost()
        {
            #region Config

            var builder = WebApplication.CreateBuilder();

            var contentRoot = Directory.GetCurrentDirectory();

            // builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(9874));

            var webRoot = Path.Combine(contentRoot, "wwwroot", "docs");

            builder.Services.AddSpaStaticFiles(configuration => { configuration.RootPath = webRoot; });

            var api = builder.Build();

            var _player = new Player();
            var _guild = new Guild();
            var _utils = new Utils();
            var _realm = new Realm();
            var _shutdown = new Shutdown();
            var _news = new News();
            var _passwordVerification = new PasswordVerification();

            #endregion

            #region API Docs

            api.UseStaticFiles();

            api.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    webRoot),
                RequestPath = new PathString("/docs")
            });
            api.Map("/docs", spaApp =>
            {
                spaApp.UseSpa(spa =>
                {
                    spa.Options.SourcePath = webRoot; // source path
                });
            });

            api.Map("/", async c => { c.Response.Redirect("/docs"); });

            #endregion

            #region Stats

            api.MapGet("/stats", async c =>
                await c.Response.WriteAsync(_utils.GetPlayerCount()));
            api.MapGet("/stats/rp", (string guildName) =>
            {
                var TopRpPlayers = _utils.GetTopRP();

                return TopRpPlayers == null ? Results.NotFound() : Results.Ok(TopRpPlayers);
            });
            api.MapGet("/stats/uptime", async c =>
                await c.Response.WriteAsJsonAsync(_utils.GetUptime(GameServer.Instance.StartupTime)));

            #endregion

            #region Player

            api.MapGet("/player", () => "Usage /player/{playerName}");
            api.MapGet("/player/{playerName}", (string playerName) =>
            {
                var playerInfo = _player.GetPlayerInfo(playerName);

                return playerInfo == null ? Results.NotFound("Not found") : Results.Ok(playerInfo);
            });
            api.MapGet("/player/{playerName}/specs", async c  => await c.Response.WriteAsJsonAsync(_player.GetPlayerSpec(c.Request.RouteValues["playerName"].ToString())));
            
            api.MapGet("/player/{playerName}/tradeskills", async c  => await c.Response.WriteAsJsonAsync(_player.GetPlayerTradeSkills(c.Request.RouteValues["playerName"].ToString())));
            
            api.MapGet("/player/getAll", async c => await c.Response.WriteAsJsonAsync(_player.GetAllPlayers()));

            #endregion

            #region Guild

            api.MapGet("/guild", () => "Usage /guild/{guildName}");
            api.MapGet("/guild/{guildName}", (string guildName) =>
            {
                var guildInfo = _guild.GetGuildInfo(guildName);

                if (guildInfo == null)
                {
                    return Results.NotFound($"Guild {guildName} not found");
                }

                return Results.Ok(guildInfo);
            });
            api.MapGet("/guild/{guildName}/members", (string guildName) =>
            {
                var guildMembers = _player.GetPlayersByGuild(guildName);

                return guildMembers == null ? Results.NotFound() : Results.Ok(guildMembers);
            });
            api.MapGet("/guild/getAll", async c => await c.Response.WriteAsJsonAsync(_guild.GetAllGuilds()));
            api.MapGet("/guild/topRP", async c => await c.Response.WriteAsJsonAsync(_guild.GetTopRPGuilds()));

            #endregion

            #region Realm

            api.MapGet("/realm", () => "Usage /realm/{realmName}");
            api.MapGet("/realm/df", async c =>
                await c.Response.WriteAsJsonAsync(_realm.GetDFOwner()));
            api.MapGet("/realm/bg", async c =>
                await c.Response.WriteAsJsonAsync(_realm.GetBGStatus()));
            api.MapGet("/realm/{realmName}", (string realmName) =>
            {
                if (realmName == null)
                {
                    return Results.NotFound();
                }

                eRealm realm = eRealm.None;
                switch (realmName.ToLower())
                {
                    case "alb":
                    case "albion":
                        realm = eRealm.Albion;
                        break;
                    case "mid":
                    case "midgard":
                        realm = eRealm.Midgard;
                        break;
                    case "hib":
                    case "hibernia":
                        realm = eRealm.Hibernia;
                        break;
                }

                List<Realm.KeepInfo> realmInfo = _realm.GetKeepsByRealm(realm);

                return realmInfo == null ? Results.NotFound($"Realm {realmName} not found") : Results.Ok(realmInfo);
            });

            #endregion

            #region Relic
            api.MapGet("/relic", async c =>
                await c.Response.WriteAsJsonAsync(_realm.GetAllRelics()));
            #endregion
            
            #region News
            api.MapGet("/news/all", async c => await c.Response.WriteAsJsonAsync(_news.GetAllNews()));
            
            api.MapGet("/news/realm/{realm}", (string realm) =>
            {
                var realmNews = _news.GetRealmNews(realm);
                return Results.Ok(realmNews);
            });
            
            api.MapGet("/news/type/{type}", (string type) =>
            {
                var typeNews = _news.GetTypeNews(type);
                return Results.Ok(typeNews);
            });

            #endregion

            #region Misc

            api.MapGet("/bread", () => Properties.BREAD);
            
            api.MapGet("/utils/discordstatus/{accountName}", (string accountName) =>
            {
                var discordStatus = Player.GetDiscord(accountName);
                return Results.Ok(discordStatus);
            });

            api.MapGet("/utils/query_clients/{password}", (string password) =>
            {
                if (!_passwordVerification.VerifyAPIPassword(password))
                {
                    return Results.Problem("No bread for you!", null, 401);
                }
                var activePlayers = _utils.GetAllClientStatuses();
                return Results.Ok(activePlayers);
            });

            api.MapGet("/utils/shutdown/{password}", (string password) =>
            {
                if (!_passwordVerification.VerifyAPIPassword(password))
                {
                    return Results.Problem("No bread for you!", null, 401);
                }
                var shutdownStatus = _shutdown.ShutdownServer();
                return Results.Ok(shutdownStatus);
            });
            
            api.MapGet("/utils/discordrequired", async c =>
                await c.Response.WriteAsync(_utils.IsDiscordRequired()));
            #endregion
            

            api.Run();
        }
    }
}