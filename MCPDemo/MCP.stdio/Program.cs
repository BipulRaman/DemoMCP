﻿using LunchTimeMCP;
using MCP.stdio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RestaurantTools>();

builder.Services.AddSingleton<RestaurantService>();

await builder.Build().RunAsync();