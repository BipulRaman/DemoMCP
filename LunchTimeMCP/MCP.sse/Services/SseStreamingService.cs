using MCP.sse.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;

namespace MCP.sse.Services;

public class SseStreamingService
{
    private readonly RestaurantService _restaurantService;
    private readonly ILogger<SseStreamingService> _logger;
    private readonly Dictionary<string, List<StreamWriter>> _activeConnections = new();
    private readonly Timer _heartbeatTimer;

    public SseStreamingService(RestaurantService restaurantService, ILogger<SseStreamingService> logger)
    {
        _restaurantService = restaurantService;
        _logger = logger;
        
        // Start heartbeat timer to keep connections alive
        _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void AddConnection(string connectionId, StreamWriter writer)
    {
        if (!_activeConnections.ContainsKey(connectionId))
        {
            _activeConnections[connectionId] = new List<StreamWriter>();
        }
        _activeConnections[connectionId].Add(writer);
        _logger.LogInformation("Added SSE connection: {ConnectionId}", connectionId);
    }

    public void RemoveConnection(string connectionId, StreamWriter writer)
    {
        if (_activeConnections.ContainsKey(connectionId))
        {
            _activeConnections[connectionId].Remove(writer);
            if (_activeConnections[connectionId].Count == 0)
            {
                _activeConnections.Remove(connectionId);
            }
        }
        _logger.LogInformation("Removed SSE connection: {ConnectionId}", connectionId);
    }

    public async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object>? arguments)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = toolName.ToLowerInvariant() switch
            {
                "get_restaurants" => await GetRestaurantsAsync(),
                "add_restaurant" => await AddRestaurantAsync(arguments),
                "pick_random_restaurant" => await PickRandomRestaurantAsync(),
                "get_visit_stats" => await GetVisitStatsAsync(),
                _ => $"Error: Unknown tool '{toolName}'"
            };

            var executionTime = DateTime.UtcNow - startTime;
            _logger.LogInformation("Tool {ToolName} executed in {ExecutionTime}ms", toolName, executionTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return $"Error executing tool '{toolName}': {ex.Message}";
        }
    }

    public async IAsyncEnumerable<SseEvent> StreamToolExecutionAsync(
        string toolName, 
        Dictionary<string, object>? arguments,
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SSE tool streaming for {ToolName} with request ID {RequestId}", toolName, requestId);

        // Send initial event
        yield return new SseEvent
        {
            Event = "tool-start",
            Id = requestId,
            Data = new { tool = toolName, status = "starting", timestamp = DateTime.UtcNow }
        };

        switch (toolName.ToLowerInvariant())
        {
            case "get_restaurants_stream":
                await foreach (var sseEvent in StreamRestaurantsAsync(requestId, cancellationToken))
                {
                    yield return sseEvent;
                }
                break;

            case "analyze_restaurants_stream":
                await foreach (var sseEvent in StreamRestaurantAnalysisAsync(arguments, requestId, cancellationToken))
                {
                    yield return sseEvent;
                }
                break;

            case "search_restaurants_stream":
                await foreach (var sseEvent in StreamRestaurantSearchAsync(arguments, requestId, cancellationToken))
                {
                    yield return sseEvent;
                }
                break;

            default:
                // Execute non-streaming tool
                var result = await ExecuteToolAsync(toolName, arguments);
                yield return new SseEvent
                {
                    Event = "tool-result",
                    Id = requestId,
                    Data = new { tool = toolName, result, timestamp = DateTime.UtcNow }
                };
                break;
        }

        // Send completion event
        yield return new SseEvent
        {
            Event = "tool-complete",
            Id = requestId,
            Data = new { tool = toolName, status = "completed", timestamp = DateTime.UtcNow }
        };
    }

    private async IAsyncEnumerable<SseEvent> StreamRestaurantsAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var restaurants = await _restaurantService.GetRestaurantsAsync();
        
        yield return new SseEvent
        {
            Event = "restaurant-stream-start",
            Id = requestId,
            Data = new { message = "??? **Loading Restaurants...**", totalCount = restaurants.Count }
        };

        await Task.Delay(500, cancellationToken); // Simulate processing

        for (int i = 0; i < restaurants.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var restaurant = restaurants[i];
            yield return new SseEvent
            {
                Event = "restaurant-data",
                Id = requestId,
                Data = new
                {
                    index = i + 1,
                    total = restaurants.Count,
                    restaurant = new
                    {
                        name = restaurant.Name,
                        location = restaurant.Location,
                        foodType = restaurant.FoodType,
                        dateAdded = restaurant.DateAdded.ToString("yyyy-MM-dd")
                    },
                    progress = Math.Round((double)(i + 1) / restaurants.Count * 100, 1)
                }
            };

            await Task.Delay(300, cancellationToken); // Simulate progressive loading
        }

        yield return new SseEvent
        {
            Event = "restaurant-stream-complete",
            Id = requestId,
            Data = new { message = "? **All restaurants loaded**", count = restaurants.Count }
        };
    }

    private async IAsyncEnumerable<SseEvent> StreamRestaurantAnalysisAsync(
        Dictionary<string, object>? arguments,
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var analysisType = arguments?.GetValueOrDefault("type", "general")?.ToString() ?? "general";
        var restaurants = await _restaurantService.GetRestaurantsAsync();

        var analysisSteps = new[]
        {
            "?? Analyzing restaurant data...",
            "?? Computing cuisine distribution...",
            "?? Mapping location clusters...",
            "? Calculating popularity metrics...",
            "?? Generating insights...",
            "? Analysis complete!"
        };

        for (int i = 0; i < analysisSteps.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var isLast = i == analysisSteps.Length - 1;
            
            if (isLast)
            {
                // Compute actual analysis
                var cuisineTypes = restaurants.GroupBy(r => r.FoodType).ToDictionary(g => g.Key, g => g.Count());
                
                yield return new SseEvent
                {
                    Event = "analysis-result",
                    Id = requestId,
                    Data = new
                    {
                        step = analysisSteps[i],
                        analysis = new
                        {
                            type = analysisType,
                            totalRestaurants = restaurants.Count,
                            uniqueCuisines = cuisineTypes.Count,
                            cuisineDistribution = cuisineTypes,
                            topCuisine = cuisineTypes.OrderByDescending(kv => kv.Value).FirstOrDefault()
                        },
                        isComplete = true
                    }
                };
            }
            else
            {
                yield return new SseEvent
                {
                    Event = "analysis-progress",
                    Id = requestId,
                    Data = new
                    {
                        step = analysisSteps[i],
                        progress = Math.Round((double)(i + 1) / analysisSteps.Length * 100, 1),
                        isComplete = false
                    }
                };
            }

            await Task.Delay(isLast ? 100 : 600, cancellationToken);
        }
    }

    private async IAsyncEnumerable<SseEvent> StreamRestaurantSearchAsync(
        Dictionary<string, object>? arguments,
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = arguments?.GetValueOrDefault("query", "")?.ToString()?.ToLowerInvariant() ?? "";
        var restaurants = await _restaurantService.GetRestaurantsAsync();

        yield return new SseEvent
        {
            Event = "search-start",
            Id = requestId,
            Data = new { query, message = $"?? **Searching for: '{query}'**" }
        };

        await Task.Delay(400, cancellationToken);

        var matchingRestaurants = restaurants.Where(r =>
            r.Name.ToLowerInvariant().Contains(query) ||
            r.Location.ToLowerInvariant().Contains(query) ||
            r.FoodType.ToLowerInvariant().Contains(query)
        ).ToList();

        if (matchingRestaurants.Any())
        {
            foreach (var restaurant in matchingRestaurants)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                yield return new SseEvent
                {
                    Event = "search-result",
                    Id = requestId,
                    Data = new
                    {
                        match = new
                        {
                            name = restaurant.Name,
                            location = restaurant.Location,
                            foodType = restaurant.FoodType
                        }
                    }
                };

                await Task.Delay(200, cancellationToken);
            }

            yield return new SseEvent
            {
                Event = "search-complete",
                Id = requestId,
                Data = new { message = $"?? **Search Complete** - Found {matchingRestaurants.Count} matching restaurant(s)", count = matchingRestaurants.Count }
            };
        }
        else
        {
            yield return new SseEvent
            {
                Event = "search-complete",
                Id = requestId,
                Data = new { message = $"? **No Results** - No restaurants found matching '{query}'", count = 0 }
            };
        }
    }

    private async Task<string> GetRestaurantsAsync()
    {
        var restaurants = await _restaurantService.GetRestaurantsAsync();
        var result = "**Restaurants:**\n";
        
        foreach (var restaurant in restaurants)
        {
            result += $"• **{restaurant.Name}** - {restaurant.Location} ({restaurant.FoodType})\n";
        }
        
        return result + $"\nTotal: {restaurants.Count} restaurants";
    }

    private async Task<string> AddRestaurantAsync(Dictionary<string, object>? arguments)
    {
        if (arguments == null)
            return "Error: Missing restaurant details";

        var name = arguments.GetValueOrDefault("name", "")?.ToString() ?? "";
        var location = arguments.GetValueOrDefault("location", "")?.ToString() ?? "";
        var foodType = arguments.GetValueOrDefault("foodType", "")?.ToString() ?? "";

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(location) || string.IsNullOrEmpty(foodType))
            return "Error: Name, location, and food type are required";

        var restaurant = await _restaurantService.AddRestaurantAsync(name, location, foodType);
        
        // Notify all connections about the new restaurant
        await BroadcastToAllConnections(new SseEvent
        {
            Event = "restaurant-update",
            Data = new { action = "added", restaurant = new { restaurant.Name, restaurant.Location, restaurant.FoodType } }
        });

        return $"? **Restaurant Added!**\n**{restaurant.Name}** - {restaurant.Location} ({restaurant.FoodType})";
    }

    private async Task<string> PickRandomRestaurantAsync()
    {
        var restaurant = await _restaurantService.PickRandomRestaurantAsync();
        
        if (restaurant == null)
            return "? No restaurants available";

        // Notify all connections about the random pick
        await BroadcastToAllConnections(new SseEvent
        {
            Event = "restaurant-update",
            Data = new { action = "picked", restaurant = new { restaurant.Name, restaurant.Location, restaurant.FoodType } }
        });

        return $"?? **Random Pick!**\n**{restaurant.Name}** - {restaurant.Location} ({restaurant.FoodType})";
    }

    private async Task<string> GetVisitStatsAsync()
    {
        var stats = await _restaurantService.GetFormattedVisitStatsAsync();
        var result = $"?? **{stats.Message}**\n\n";
        
        foreach (var stat in stats.Statistics.Take(10))
        {
            result += $"• **{stat.Restaurant}** - {stat.Location} ({stat.FoodType}) - {stat.TimesEaten}\n";
        }
        
        return result + $"\n**Total:** {stats.TotalRestaurants} restaurants, {stats.TotalVisits} visits";
    }

    private async Task BroadcastToAllConnections(SseEvent sseEvent)
    {
        var eventData = FormatSseEvent(sseEvent);
        var tasks = new List<Task>();

        foreach (var connectionWriters in _activeConnections.Values)
        {
            foreach (var writer in connectionWriters.ToList())
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await writer.WriteAsync(eventData);
                        await writer.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to broadcast to SSE connection");
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);
    }

    private void SendHeartbeat(object? state)
    {
        var heartbeatEvent = new SseEvent
        {
            Event = "heartbeat",
            Data = new { timestamp = DateTime.UtcNow, activeConnections = _activeConnections.Count }
        };

        _ = BroadcastToAllConnections(heartbeatEvent);
    }

    public static string FormatSseEvent(SseEvent sseEvent)
    {
        var sb = new StringBuilder();
        
        if (!string.IsNullOrEmpty(sseEvent.Event))
            sb.AppendLine($"event: {sseEvent.Event}");
        
        if (!string.IsNullOrEmpty(sseEvent.Id))
            sb.AppendLine($"id: {sseEvent.Id}");
        
        if (sseEvent.Data != null)
        {
            var dataJson = JsonSerializer.Serialize(sseEvent.Data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            sb.AppendLine($"data: {dataJson}");
        }
        
        sb.AppendLine(); // Empty line to terminate the event
        return sb.ToString();
    }

    public void Dispose()
    {
        _heartbeatTimer?.Dispose();
    }
}