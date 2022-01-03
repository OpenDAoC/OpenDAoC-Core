using System;
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
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Directory.GetCurrentDirectory();
            DateTime startupTime = DateTime.Now;
            
            var webRoot = Path.Combine(contentRoot,"wwwroot", "docs");
            
            builder.Services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = webRoot;
            });
            
            var app = builder.Build();
            
            var _player = new Player();
            var _guild = new Guild();
            var _stats = new Stats();

            // API DOCS
            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    webRoot),
                RequestPath = new PathString("/docs")
            });
            app.Map("/docs", spaApp=>
            {
                spaApp.UseSpa(spa =>
                {
                    spa.Options.SourcePath = webRoot; // source path
                });
            });

            app.Map("/", async c =>
            {
                c.Response.Redirect("/docs");
            });

            // STATS
            app.MapGet("/stats", async c =>
                await c.Response.WriteAsync(_stats.GetPlayerCount()));
            app.MapGet("/stats/rp", (string guildName) =>
            {
                var TopRpPlayers = _stats.GetTopRP();
                
                if (TopRpPlayers == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(TopRpPlayers);
                
            });
            app.MapGet("/stats/uptime", async c =>
                await c.Response.WriteAsJsonAsync(_stats.GetUptime(startupTime)));
            
            // PLAYER
            app.MapGet("/player", () => "Usage /player/{playerName}");
            app.MapGet("/player/{playerName}", (string playerName) =>
            {
                var playerInfo = _player.GetPlayerInfo(playerName);
                
                if (playerInfo == null)
                {
                    return Results.NotFound("Not found");
                }
                return Results.Ok(playerInfo);
                
            });
            app.MapGet("/player/getAll", async c => await c.Response.WriteAsJsonAsync(_player.GetAllPlayers()));
            
            // GUILD
            app.MapGet("/guild", () => "Usage /guild/{guildName}");
            app.MapGet("/guild/{guildName}", (string guildName) =>
            {
                var guildInfo = _guild.GetGuildInfo(guildName);
                
                if (guildInfo == null)
                {
                    return Results.NotFound($"Guild {guildName} not found");
                }
                return Results.Ok(guildInfo);
                
            });
            app.MapGet("/guild/{guildName}/members", (string guildName) =>
            {
                var guildMembers = _player.GetPlayersByGuild(guildName);
                
                if (guildMembers == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(guildMembers);
                
            });
            
            app.MapGet("/bread", () => Properties.BREAD);

            app.Run();
        }
        
    }
}
        
