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
            DateTime startupTime = DateTime.Now;

            // builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(9874));

            var webRoot = Path.Combine(contentRoot, "wwwroot", "docs");

            builder.Services.AddSpaStaticFiles(configuration => { configuration.RootPath = webRoot; });

            var api = builder.Build();

            var _player = new Player();
            var _guild = new Guild();
            var _stats = new Stats();
            var _realm = new Realm();

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
                await c.Response.WriteAsync(_stats.GetPlayerCount()));
            api.MapGet("/stats/rp", (string guildName) =>
            {
                var TopRpPlayers = _stats.GetTopRP();

                if (TopRpPlayers == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(TopRpPlayers);
            });
            api.MapGet("/stats/uptime", async c =>
                await c.Response.WriteAsJsonAsync(_stats.GetUptime(startupTime)));

            #endregion

            #region Player

            api.MapGet("/player", () => "Usage /player/{playerName}");
            api.MapGet("/player/{playerName}", (string playerName) =>
            {
                var playerInfo = _player.GetPlayerInfo(playerName);

                if (playerInfo == null)
                {
                    return Results.NotFound("Not found");
                }

                return Results.Ok(playerInfo);
            });
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

                if (guildMembers == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(guildMembers);
            });
            api.MapGet("/guild/getAll", async c => await c.Response.WriteAsJsonAsync(_guild.GetAllGuilds()));
            api.MapGet("/guild/topRP", async c => await c.Response.WriteAsJsonAsync(_guild.GetTopRPGuilds()));

            #endregion

            #region Realm

            api.MapGet("/realm", () => "Usage /realm/{realmName}");
            api.MapGet("/realm/df", async c =>
                await c.Response.WriteAsJsonAsync(_realm.GetDFOwner()));
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

                if (realmInfo == null)
                {
                    return Results.NotFound($"Realm {realmName} not found");
                }

                return Results.Ok(realmInfo);
            });

            #endregion

            #region Relic
            api.MapGet("/relic", async c =>
                await c.Response.WriteAsJsonAsync(_realm.GetAllRelics()));
            #endregion

            #region Misc

            api.MapGet("/bread", () => Properties.BREAD);
            
            api.MapGet("/utils/discordstatus/{accountName}", (string accountName) =>
            {
                var discordStatus = _player.GetDiscord(accountName);
                return Results.Ok(discordStatus);
            });
            #endregion

            api.Run();
        }
    }
}