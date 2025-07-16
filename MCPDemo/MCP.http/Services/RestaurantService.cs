using System.Text.Json;
using MCP.http.Models;
using Microsoft.Extensions.Logging;

namespace MCP.http.Services;

public class RestaurantService
{
    private readonly string _dataFilePath;
    private readonly List<Restaurant> _restaurants = new();
    private readonly Dictionary<string, int> _visitCounts = new();
    private readonly ILogger<RestaurantService>? _logger;

    public RestaurantService(ILogger<RestaurantService>? logger = null)
    {
        _logger = logger;
        
        // Store data in user's app data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appDataPath, "MCP.http");
        Directory.CreateDirectory(appDir);

        _dataFilePath = Path.Combine(appDir, "restaurants.json");
        LoadData();

        // Initialize with trendy restaurants if empty
        if (_restaurants.Count == 0)
        {
            InitializeWithTrendyRestaurants();
            SaveData();
        }
    }

    public Task<List<Restaurant>> GetRestaurantsAsync()
    {
        return Task.FromResult(_restaurants.ToList());
    }

    public Task<Restaurant> AddRestaurantAsync(string name, string location, string foodType)
    {
        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Location = location,
            FoodType = foodType,
            DateAdded = DateTime.UtcNow
        };

        _restaurants.Add(restaurant);
        SaveData();

        _logger?.LogInformation("Added restaurant: {Name} at {Location}", name, location);

        return Task.FromResult(restaurant);
    }

    public Task<Restaurant?> PickRandomRestaurantAsync()
    {
        if (_restaurants.Count == 0)
            return Task.FromResult<Restaurant?>(null);

        var random = new Random();
        var selectedRestaurant = _restaurants[random.Next(_restaurants.Count)];

        // Track the visit
        _visitCounts[selectedRestaurant.Id] = _visitCounts.GetValueOrDefault(selectedRestaurant.Id, 0) + 1;
        SaveData();

        _logger?.LogInformation("Selected random restaurant: {Name} (Visit #{Count})", 
            selectedRestaurant.Name, _visitCounts[selectedRestaurant.Id]);

        return Task.FromResult<Restaurant?>(selectedRestaurant);
    }

    public Task<Dictionary<string, RestaurantVisitInfo>> GetVisitStatsAsync()
    {
        var stats = new Dictionary<string, RestaurantVisitInfo>();

        foreach (var restaurant in _restaurants)
        {
            var visitCount = _visitCounts.GetValueOrDefault(restaurant.Id, 0);
            stats[restaurant.Name] = new RestaurantVisitInfo
            {
                Restaurant = restaurant,
                VisitCount = visitCount,
                LastVisited = visitCount > 0 ? DateTime.UtcNow : null // In a real app, you'd track actual visit dates
            };
        }

        return Task.FromResult(stats);
    }

    public async Task<FormattedRestaurantStats> GetFormattedVisitStatsAsync()
    {
        var stats = await GetVisitStatsAsync();

        var formattedStats = stats.Values
            .OrderByDescending(x => x.VisitCount)
            .Select(stat => new FormattedRestaurantStat
            {
                Restaurant = stat.Restaurant.Name,
                Location = stat.Restaurant.Location,
                FoodType = stat.Restaurant.FoodType,
                VisitCount = stat.VisitCount,
                TimesEaten = stat.VisitCount switch
                {
                    0 => "Never",
                    1 => "Once",
                    _ => $"{stat.VisitCount} times"
                }
            })
            .ToList();

        return new FormattedRestaurantStats
        {
            Message = "Restaurant visit statistics:",
            Statistics = formattedStats,
            TotalRestaurants = stats.Count,
            TotalVisits = stats.Values.Sum(x => x.VisitCount)
        };
    }

    private void LoadData()
    {
        if (!File.Exists(_dataFilePath))
            return;

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            var data = JsonSerializer.Deserialize<RestaurantData>(json, RestaurantContext.Default.RestaurantData);

            if (data != null)
            {
                _restaurants.Clear();
                _restaurants.AddRange(data.Restaurants ?? []);
                _visitCounts.Clear();
                foreach (var (key, value) in data.VisitCounts ?? new Dictionary<string, int>())
                {
                    _visitCounts[key] = value;
                }
                
                _logger?.LogInformation("Loaded {Count} restaurants from {Path}", 
                    _restaurants.Count, _dataFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading data from {Path}", _dataFilePath);
        }
    }

    private void SaveData()
    {
        try
        {
            var data = new RestaurantData
            {
                Restaurants = _restaurants,
                VisitCounts = _visitCounts
            };

            var json = JsonSerializer.Serialize(data, RestaurantContext.Default.RestaurantData);
            File.WriteAllText(_dataFilePath, json);
            
            _logger?.LogDebug("Saved restaurant data to {Path}", _dataFilePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving data to {Path}", _dataFilePath);
        }
    }

    private void InitializeWithTrendyRestaurants()
    {
        var trendyRestaurants = new List<Restaurant>
        {
            new() { Id = Guid.NewGuid().ToString(), Name = "Guelaguetza", Location = "3014 W Olympic Blvd", FoodType = "Oaxacan Mexican", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Republique", Location = "624 S La Brea Ave", FoodType = "French Bistro", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Night + Market WeHo", Location = "9041 Sunset Blvd", FoodType = "Thai Street Food", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Gracias Madre", Location = "8905 Melrose Ave", FoodType = "Vegan Mexican", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "The Ivy", Location = "113 N Robertson Blvd", FoodType = "Californian", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Catch LA", Location = "8715 Melrose Ave", FoodType = "Seafood", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Cecconi's", Location = "8764 Melrose Ave", FoodType = "Italian", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Earls Kitchen + Bar", Location = "8730 W Sunset Blvd", FoodType = "Global Comfort Food", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Pump Restaurant", Location = "8948 Santa Monica Blvd", FoodType = "Mediterranean", DateAdded = DateTime.UtcNow },
            new() { Id = Guid.NewGuid().ToString(), Name = "Craig's", Location = "8826 Melrose Ave", FoodType = "American Contemporary", DateAdded = DateTime.UtcNow }
        };

        _restaurants.AddRange(trendyRestaurants);
        
        _logger?.LogInformation("Initialized with {Count} trendy Los Angeles restaurants", trendyRestaurants.Count);
    }
}