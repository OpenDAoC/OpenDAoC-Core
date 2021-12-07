using System;
using System.Collections.Generic;
using System.IO;
using DOL.GS;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace DOL.GS.API
{
    internal class ApiHost
    {
        private readonly Player _player;
        private readonly Guild _guild;

        public ApiHost()
        {
            var builder = WebApplication.CreateBuilder();

            var contentRoot = Directory.GetCurrentDirectory();
            var webRoot = Path.Combine(contentRoot,"wwwroot", "docs");
            
            builder.Services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = webRoot;
            });
            
            var app = builder.Build();

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
            
            
            _player = new Player();
            _guild = new Guild();
            
            // stats
            app.MapGet("/stats", async c =>
                await c.Response.WriteAsync(_player.GetPlayerCount()));
            // player
            app.MapGet("/player", () => "Usage /player/{playerName}");
            app.MapGet("/player/{playerName}", (string playerName) =>
            {
                var playerInfo = _player.GetPlayerInfo(playerName);
                
                if (playerInfo == null)
                {
                    return Results.NotFound("Player not found");
                }
                return Results.Ok(playerInfo);
                
            });
            
            // guild
            app.MapGet("/guild", () => "Usage /guild/{guildName}");
            app.MapGet("/guild/{guildName}", (string guildName) =>
            {
                var guildInfo = _guild.GetGuildInfo(guildName);
                
                if (guildInfo == null)
                {
                    return Results.NotFound("Guild not found");
                }
                return Results.Ok(guildInfo);
                
            });
            
            // app.MapGet("/guild/{guildName}/members", (string guildName) =>
            // {
            //     var guildMembers = _guild.GetGuildMembers(guildName);
            //     
            //     if (guildMembers == null)
            //     {
            //         return Results.NotFound();
            //     }
            //     return Results.Ok(guildMembers);
            //     
            // });
            
            app.Run();
        }
        
    }
}
        
