using MCP.http.Models;
using MCP.http.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MCP.http.Tools;

[McpServerToolType]
public sealed class RestaurantTools
{
    private readonly RestaurantService _restaurantService;
    private readonly IHttpClientFactory _httpClientFactory;

    public RestaurantTools(RestaurantService restaurantService, IHttpClientFactory httpClientFactory)
    {
        _restaurantService = restaurantService;
        _httpClientFactory = httpClientFactory;
    }

    [McpServerTool, Description("Get a list of all restaurants available for lunch.")]
    public async Task<string> GetRestaurants()
    {
        var restaurants = await _restaurantService.GetRestaurantsAsync();
        return JsonSerializer.Serialize(restaurants, RestaurantContext.Default.ListRestaurant);
    }

    [McpServerTool, Description("Add a new restaurant to the lunch options.")]
    public async Task<string> AddRestaurant(
        [Description("The name of the restaurant")] string name,
        [Description("The location/address of the restaurant")] string location,
        [Description("The type of food served (e.g., Italian, Mexican, Thai, etc.)")] string foodType)
    {
        var restaurant = await _restaurantService.AddRestaurantAsync(name, location, foodType);
        return JsonSerializer.Serialize(restaurant, RestaurantContext.Default.Restaurant);
    }

    [McpServerTool, Description("Pick a random restaurant from the available options for lunch.")]
    public async Task<string> PickRandomRestaurant()
    {
        var selectedRestaurant = await _restaurantService.PickRandomRestaurantAsync();

        if (selectedRestaurant == null)
        {
            return JsonSerializer.Serialize(new
            {
                message = "No restaurants available. Please add some restaurants first!"
            });
        }

        return JsonSerializer.Serialize(new
        {
            message = $"??? Time for lunch at {selectedRestaurant.Name}!",
            restaurant = selectedRestaurant
        });
    }

    [McpServerTool, Description("Get statistics about how many times each restaurant has been visited.")]
    public async Task<string> GetVisitStatistics()
    {
        var formattedStats = await _restaurantService.GetFormattedVisitStatsAsync();
        return JsonSerializer.Serialize(formattedStats, RestaurantContext.Default.FormattedRestaurantStats);
    }

    // HTTP-specific tools for enhanced functionality
    [McpServerTool, Description("Stream all restaurants with progressive loading (chunked JSON).")]
    public async Task<string> GetRestaurantsStream()
    {
        var restaurants = await _restaurantService.GetRestaurantsAsync();

        // Simulate streaming by adding metadata
        var result = new StreamingResponse<List<Restaurant>>
        {
            Streaming = true,
            Protocol = "chunked-json",
            Data = restaurants,
            Metadata = new StreamingMetadata
            {
                ChunkNumber = 1,
                TotalChunks = 1,
                IsLast = true,
                Timestamp = DateTime.UtcNow
            }
        };

        return JsonSerializer.Serialize(result, RestaurantContext.Default.StreamingResponseListRestaurant);
    }

    [McpServerTool, Description("Stream real-time analysis of restaurant data (chunked JSON).")]
    public async Task<string> AnalyzeRestaurantsStream(
        [Description("Type of analysis to perform")] string type = "general")
    {
        var restaurants = await _restaurantService.GetRestaurantsAsync();
        var stats = await _restaurantService.GetFormattedVisitStatsAsync();

        object analysis = type.ToLowerInvariant() switch
        {
            "cuisine" => AnalyzeByCuisine(restaurants),
            "location" => AnalyzeByLocation(restaurants),
            "popularity" => AnalyzeByPopularity(stats),
            _ => AnalyzeGeneral(restaurants, stats)
        };

        var result = new
        {
            streaming = true,
            protocol = "chunked-json",
            analysisType = type,
            analysis = analysis,
            metadata = new
            {
                chunkNumber = 1,
                totalChunks = 1,
                isLast = true,
                timestamp = DateTime.UtcNow
            }
        };

        return JsonSerializer.Serialize(result);
    }

    [McpServerTool, Description("Stream search results as they're found (chunked JSON).")]
    public async Task<string> SearchRestaurantsStream(
        [Description("Search query for restaurants")] string query)
    {
        var restaurants = await _restaurantService.GetRestaurantsAsync();
        var searchResults = restaurants.Where(r =>
            r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            r.Location.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            r.FoodType.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        var result = new StreamingResponse<SearchResults>
        {
            Streaming = true,
            Protocol = "chunked-json",
            Data = new SearchResults
            {
                Query = query,
                Results = searchResults,
                TotalFound = searchResults.Count
            },
            Metadata = new StreamingMetadata
            {
                ChunkNumber = 1,
                TotalChunks = 1,
                IsLast = true,
                Timestamp = DateTime.UtcNow
            }
        };

        return JsonSerializer.Serialize(result, RestaurantContext.Default.StreamingResponseSearchResults);
    }

    private CuisineAnalysis AnalyzeByCuisine(List<Restaurant> restaurants)
    {
        var cuisineGroups = restaurants.GroupBy(r => r.FoodType)
            .Select(g => new CuisineGroup
            {
                Cuisine = g.Key,
                Count = g.Count(),
                Restaurants = g.Select(r => r.Name).ToArray()
            })
            .OrderByDescending(g => g.Count)
            .ToArray();

        return new CuisineAnalysis
        {
            TotalCuisines = cuisineGroups.Length,
            MostPopularCuisine = cuisineGroups.FirstOrDefault()?.Cuisine ?? "None",
            CuisineGroups = cuisineGroups
        };
    }

    private LocationAnalysis AnalyzeByLocation(List<Restaurant> restaurants)
    {
        var locationGroups = restaurants.GroupBy(r => r.Location.Split(',')[0].Trim())
            .Select(g => new LocationGroup
            {
                Area = g.Key,
                Count = g.Count(),
                Restaurants = g.Select(r => r.Name).ToArray()
            })
            .OrderByDescending(g => g.Count)
            .ToArray();

        return new LocationAnalysis
        {
            TotalAreas = locationGroups.Length,
            MostPopularArea = locationGroups.FirstOrDefault()?.Area ?? "None",
            LocationGroups = locationGroups
        };
    }

    private PopularityAnalysis AnalyzeByPopularity(FormattedRestaurantStats stats)
    {
        var popularRestaurants = stats.Statistics.Where(s => s.VisitCount > 0)
            .OrderByDescending(s => s.VisitCount)
            .Take(5)
            .Select(s => new PopularRestaurant
            {
                Name = s.Restaurant,
                VisitCount = s.VisitCount,
                TimesEaten = s.TimesEaten
            })
            .ToArray();

        var neverVisited = stats.Statistics.Where(s => s.VisitCount == 0).Count();

        return new PopularityAnalysis
        {
            TotalVisits = stats.TotalVisits,
            TotalRestaurants = stats.TotalRestaurants,
            TopRestaurants = popularRestaurants,
            AverageVisitsPerRestaurant = stats.TotalRestaurants > 0 ? (double)stats.TotalVisits / stats.TotalRestaurants : 0
        };
    }

    private GeneralAnalysis AnalyzeGeneral(List<Restaurant> restaurants, FormattedRestaurantStats stats)
    {
        return new GeneralAnalysis
        {
            TotalRestaurants = restaurants.Count,
            TotalCuisines = restaurants.Select(r => r.FoodType).Distinct().Count(),
            TotalVisits = stats.TotalVisits,
            AverageVisitsPerRestaurant = restaurants.Count > 0 ? (double)stats.TotalVisits / restaurants.Count : 0,
            Recommendations = GetRecommendations(restaurants, stats)
        };
    }

    private string[] GetRecommendations(List<Restaurant> restaurants, FormattedRestaurantStats stats)
    {
        var recommendations = new List<string>();

        var neverVisited = stats.Statistics.Where(s => s.VisitCount == 0).ToList();
        if (neverVisited.Any())
        {
            var randomUnvisited = neverVisited[new Random().Next(neverVisited.Count)];
            recommendations.Add($"Try {randomUnvisited.Restaurant} - you haven't been there yet!");
        }

        var leastVisited = stats.Statistics.Where(s => s.VisitCount > 0)
            .OrderBy(s => s.VisitCount).FirstOrDefault();
        if (leastVisited != null)
        {
            recommendations.Add($"Consider revisiting {leastVisited.Restaurant} - it's been a while!");
        }

        return recommendations.ToArray();
    }

    [McpServerTool, Description("Get nearby restaurants using an external API (simulated).")]
    public Task<string> GetNearbyRestaurants(
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude,
        [Description("Search radius in miles.")] double radiusMiles = 5.0)
    {
        // Simulate an external API call for demonstration
        var client = _httpClientFactory.CreateClient("RestaurantApi");

        // This would normally be a real API call like:
        // using var jsonDocument = await client.ReadJsonDocumentAsync($"/restaurants/search?lat={latitude}&lng={longitude}&radius={radiusMiles}");

        // For now, simulate the response
        var nearbyRestaurants = new
        {
            searchLocation = new { latitude, longitude },
            radiusMiles,
            results = new[]
            {
                new { name = "Pizza Palace", address = "123 Main St", cuisine = "Italian", rating = 4.5, distance = 0.8 },
                new { name = "Taco Fiesta", address = "456 Oak Ave", cuisine = "Mexican", rating = 4.2, distance = 1.2 },
                new { name = "Sushi Zen", address = "789 Pine Rd", cuisine = "Japanese", rating = 4.7, distance = 2.1 }
            },
            metadata = new
            {
                apiProvider = "SimulatedRestaurantAPI",
                timestamp = DateTime.UtcNow,
                totalFound = 3
            }
        };

        return Task.FromResult(JsonSerializer.Serialize(nearbyRestaurants));
    }

    [McpServerTool, Description("Get restaurant reviews from external sources (simulated).")]
    public Task<string> GetRestaurantReviews(
        [Description("Name of the restaurant to get reviews for.")] string restaurantName)
    {
        var client = _httpClientFactory.CreateClient("RestaurantApi");

        // Simulate getting reviews from external API
        var reviews = new
        {
            restaurant = restaurantName,
            averageRating = 4.3,
            totalReviews = 127,
            recentReviews = new[]
            {
                new { rating = 5, comment = "Amazing food and great service!", author = "FoodLover123", date = DateTime.UtcNow.AddDays(-2) },
                new { rating = 4, comment = "Good atmosphere, decent prices.", author = "HungryTraveler", date = DateTime.UtcNow.AddDays(-5) },
                new { rating = 5, comment = "Best restaurant in town!", author = "LocalFoodie", date = DateTime.UtcNow.AddDays(-7) }
            },
            metadata = new
            {
                reviewSource = "SimulatedReviewAPI",
                timestamp = DateTime.UtcNow,
                lastUpdated = DateTime.UtcNow.AddHours(-2)
            }
        };

        return Task.FromResult(JsonSerializer.Serialize(reviews));
    }
}