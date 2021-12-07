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
        private readonly Player _player;

        public ApiHost()
        {
            var builder = WebApplication.CreateBuilder();

            var app = builder.Build();

            app.MapGet("/hello", () => "Hello World");

            _player = new Player();
            app.MapGet("/players", async c =>
                await c.Response.WriteAsync(_player.GetPlayerCount()));
            
            app.MapGet("/who", () => "Usage /who/{playerName}");
            app.MapGet("/who/{playerName}", (string playerName) => _player.GetPlayerInfo(playerName));
            
            app.Run();
        }
        
    }
}
        
