using MCP.sse.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MCP.sse.Services;

public class StreamingService
{
    private readonly RestaurantService _restaurantService;
    private readonly ILogger<StreamingService> _logger;

    public StreamingService(RestaurantService restaurantService, ILogger<StreamingService> logger)
    {
        _restaurantService = restaurantService;
        _logger = logger;
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

    /// <summary>
    /// Creates a streaming response for tool calls that simulate progressive data loading
    /// </summary>
    public async IAsyncEnumerable<string> StreamToolCallAsync(
        string toolName, 
        Dictionary<string, object>? arguments, 
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting streaming tool call for {ToolName} with request ID {RequestId}", toolName, requestId);

        switch (toolName.ToLowerInvariant())
        {
            case "get_restaurants_stream":
                await foreach (var chunk in StreamRestaurantsAsync(requestId, cancellationToken))
                {
                    yield return chunk;
                }
                break;

            case "analyze_restaurants_stream":
                await foreach (var chunk in StreamRestaurantAnalysisAsync(arguments, requestId, cancellationToken))
                {
                    yield return chunk;
                }
                break;

            case "search_restaurants_stream":
                await foreach (var chunk in StreamRestaurantSearchAsync(arguments, requestId, cancellationToken))
                {
                    yield return chunk;
                }
                break;

            default:
                yield return CreateErrorChunk(requestId, $"Unknown streaming tool: {toolName}", 1);
                break;
        }
    }

    private async IAsyncEnumerable<string> StreamRestaurantsAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simulate loading restaurants progressively
        var restaurants = await _restaurantService.GetRestaurantsAsync();
        var totalChunks = restaurants.Count + 1; // +1 for final summary

        // Send header chunk
        yield return CreateStreamChunk(requestId, 1, totalChunks, 
            "??? **Streaming Restaurant Data**\n\n? Loading restaurants...", false);

        await Task.Delay(300, cancellationToken); // Simulate processing time

        // Stream each restaurant
        for (int i = 0; i < restaurants.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var restaurant = restaurants[i];
            var content = $"**{i + 1}. {restaurant.Name}**\n" +
                         $"?? Location: {restaurant.Location}\n" +
                         $"?? Food Type: {restaurant.FoodType}\n" +
                         $"?? Added: {restaurant.DateAdded:yyyy-MM-dd}\n";

            yield return CreateStreamChunk(requestId, i + 2, totalChunks, content, false);
            
            await Task.Delay(200, cancellationToken); // Simulate processing time
        }

        // Send final summary
        var summary = $"\n? **Streaming Complete**\n?? Total restaurants loaded: {restaurants.Count}";
        yield return CreateStreamChunk(requestId, totalChunks, totalChunks, summary, true);
    }

    private async IAsyncEnumerable<string> StreamRestaurantAnalysisAsync(
        Dictionary<string, object>? arguments,
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var analysisType = arguments?.GetValueOrDefault("type", "general")?.ToString() ?? "general";
        var restaurants = await _restaurantService.GetRestaurantsAsync();
        
        // Progressive analysis simulation
        var steps = new[]
        {
            "?? Analyzing restaurant data...",
            "?? Computing cuisine distribution...",
            "?? Mapping location clusters...",
            "? Calculating popularity metrics...",
            "?? Generating insights...",
            "? Analysis complete!"
        };

        for (int i = 0; i < steps.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var isLast = i == steps.Length - 1;
            var content = steps[i];

            if (isLast)
            {
                // Add actual analysis results
                var cuisineTypes = restaurants.GroupBy(r => r.FoodType).ToDictionary(g => g.Key, g => g.Count());
                var analysisResults = string.Join("\n", cuisineTypes.Select(kv => $"• {kv.Key}: {kv.Value} restaurant(s)"));
                
                content += $"\n\n?? **{analysisType.ToUpperInvariant()} ANALYSIS RESULTS:**\n\n" +
                          $"**Cuisine Distribution:**\n{analysisResults}\n\n" +
                          $"**Total Restaurants:** {restaurants.Count}\n" +
                          $"**Unique Cuisines:** {cuisineTypes.Count}\n" +
                          $"**Analysis Type:** {analysisType}";
            }

            yield return CreateStreamChunk(requestId, i + 1, steps.Length, content, isLast);
            
            if (!isLast)
                await Task.Delay(500, cancellationToken);
        }
    }

    private async IAsyncEnumerable<string> StreamRestaurantSearchAsync(
        Dictionary<string, object>? arguments,
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = arguments?.GetValueOrDefault("query", "")?.ToString()?.ToLowerInvariant() ?? "";
        var restaurants = await _restaurantService.GetRestaurantsAsync();

        yield return CreateStreamChunk(requestId, 1, null, 
            $"?? **Searching for: '{query}'**\n\n? Scanning restaurants...", false);

        await Task.Delay(300, cancellationToken);

        var matchingRestaurants = restaurants.Where(r => 
            r.Name.ToLowerInvariant().Contains(query) ||
            r.Location.ToLowerInvariant().Contains(query) ||
            r.FoodType.ToLowerInvariant().Contains(query)
        ).ToList();

        if (matchingRestaurants.Any())
        {
            for (int i = 0; i < matchingRestaurants.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var restaurant = matchingRestaurants[i];
                var content = $"? **Match {i + 1}: {restaurant.Name}**\n" +
                             $"?? Location: {restaurant.Location}\n" +
                             $"?? Food Type: {restaurant.FoodType}\n";

                yield return CreateStreamChunk(requestId, i + 2, null, content, false);
                await Task.Delay(200, cancellationToken);
            }

            yield return CreateStreamChunk(requestId, matchingRestaurants.Count + 2, 
                matchingRestaurants.Count + 2,
                $"\n?? **Search Complete**\nFound {matchingRestaurants.Count} matching restaurant(s)", true);
        }
        else
        {
            yield return CreateStreamChunk(requestId, 2, 2,
                $"? **No Results**\nNo restaurants found matching '{query}'", true);
        }
    }

    private string CreateStreamChunk(string requestId, int sequence, int? total, string content, bool isLast)
    {
        var streamingResponse = new StreamingJsonRpcResponse
        {
            Id = requestId,
            Result = new McpToolCallResult
            {
                Content = new[]
                {
                    new McpContent
                    {
                        Type = "text",
                        Text = content
                    }
                },
                Metadata = new McpToolCallMetadata
                {
                    Timestamp = DateTime.UtcNow,
                    Streaming = new StreamingMetadata
                    {
                        ChunkNumber = sequence,
                        TotalChunks = total,
                        IsLast = isLast
                    }
                }
            },
            Streaming = new StreamingInfo
            {
                Type = isLast ? "complete" : "partial",
                Sequence = sequence,
                Total = total,
                Timestamp = DateTime.UtcNow
            }
        };

        return JsonSerializer.Serialize(streamingResponse, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private string CreateErrorChunk(string requestId, string error, int sequence)
    {
        var errorResponse = new StreamingJsonRpcResponse
        {
            Id = requestId,
            Error = new JsonRpcError
            {
                Code = -32603,
                Message = error
            },
            Streaming = new StreamingInfo
            {
                Type = "error",
                Sequence = sequence,
                Timestamp = DateTime.UtcNow
            }
        };

        return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
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

        return $"? **Restaurant Added!**\n**{restaurant.Name}** - {restaurant.Location} ({restaurant.FoodType})";
    }

    private async Task<string> PickRandomRestaurantAsync()
    {
        var restaurant = await _restaurantService.PickRandomRestaurantAsync();
        
        if (restaurant == null)
            return "? No restaurants available";

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
}