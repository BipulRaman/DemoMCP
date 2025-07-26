using System.ComponentModel;
using System.Text.Json;
using LunchTimeMCP;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace MCP.stdio;

[McpServerToolType]
public sealed class RestaurantTools
{
    private readonly RestaurantService restaurantService;
    private readonly ILogger<RestaurantTools> logger;

    public RestaurantTools(RestaurantService restaurantService, ILogger<RestaurantTools> logger)
    {
        this.restaurantService = restaurantService;
        this.logger = logger;
    }

    [McpServerTool, Description("Get a list of all restaurants available for lunch.")]
    public async Task<string> GetRestaurants()
    {
        try
        {
            var restaurants = await restaurantService.GetRestaurantsAsync();
            return JsonSerializer.Serialize(restaurants, RestaurantContext.Default.ListRestaurant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to get restaurants: {ErrorMessage}", 
                nameof(RestaurantTools), nameof(GetRestaurants), ex.Message);
            throw;
        }
    }

    [McpServerTool, Description("Add a new restaurant to the lunch options.")]
    public async Task<string> AddRestaurant(
    [Description("The name of the restaurant")] string name,
    [Description("The location/address of the restaurant")] string location,
    [Description("The type of food served (e.g., Italian, Mexican, Thai, etc.)")] string foodType)
    {
        try
        {
            var restaurant = await restaurantService.AddRestaurantAsync(name, location, foodType);
            return JsonSerializer.Serialize(restaurant, RestaurantContext.Default.Restaurant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to add restaurant '{RestaurantName}': {ErrorMessage}", 
                nameof(RestaurantTools), nameof(AddRestaurant), name, ex.Message);
            throw;
        }
    }

    [McpServerTool, Description("Pick a random restaurant from the available options for lunch.")]
    public async Task<string> PickRandomRestaurant()
    {
        try
        {
            var selectedRestaurant = await restaurantService.PickRandomRestaurantAsync();

            if (selectedRestaurant == null)
            {
                return JsonSerializer.Serialize(new
                {
                    message = "No restaurants available. Please add some restaurants first!"
                });
            }

            return JsonSerializer.Serialize(new
            {
                message = $"🍽️ Time for lunch at {selectedRestaurant.Name}!",
                restaurant = selectedRestaurant
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to pick random restaurant: {ErrorMessage}", 
                nameof(RestaurantTools), nameof(PickRandomRestaurant), ex.Message);
            throw;
        }
    }

    [McpServerTool, Description("Get statistics about how many times each restaurant has been visited.")]
    public async Task<string> GetVisitStatistics()
    {
        try
        {
            var formattedStats = await restaurantService.GetFormattedVisitStatsAsync();
            return JsonSerializer.Serialize(formattedStats, RestaurantContext.Default.FormattedRestaurantStats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to get visit statistics: {ErrorMessage}", 
                nameof(RestaurantTools), nameof(GetVisitStatistics), ex.Message);
            throw;
        }
    }
}