# Building Your Own MCP Server - Lunch Roulette

## Overview
Learn to build your own MCP server from scratch by creating a lunch recommendation system. This tutorial will teach you the complete process of designing, implementing, and deploying an MCP server using C#.

## 3.1 Project Foundation

### Create console app

Create a new folder called **LunchTime** and then create a new C# console app named **LunchTimeMCP** inside of the folder. 

### NuGet Package Installation
Essential packages for MCP server development:
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.6" />
    <PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.1" />
    <PackageReference Include="System.Text.Json" Version="9.0.6" />
</ItemGroup>
```

### Program.cs Setup
- Configure the host builder
- Set up dependency injection
- Register services and MCP tools
- Initialize MCP server components
- Handle application lifecycle

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport();

await builder.Build().RunAsync();
```

**Key Points**:
- The MCP library handles the rest of the server setup and protocol implementation
- We will come back and register our services and tools

## 3.2 Core Components

### RestaurantService Implementation
You'll copy the complete **RestaurantService** below into a new file named **RestaurantService.cs**. This service provides the core business logic and includes:

**Key Features:**
- **Data Persistence**: JSON file storage in user's AppData directory
- **Restaurant Management**: Add, retrieve, and track restaurants
- **Visit Tracking**: Count and statistics for restaurant selections
- **Seed Data**: Pre-populated with trendy West Hollywood restaurants
- **Async Operations**: All methods use async/await pattern
- **Error Handling**: Try-catch blocks for file operations

**Core Methods:**
- `GetRestaurantsAsync()` - Retrieve all restaurants
- `AddRestaurantAsync()` - Add new restaurant with validation
- `PickRandomRestaurantAsync()` - Random selection with visit tracking
- `GetVisitStatsAsync()` - Raw visit statistics
- `GetFormattedVisitStatsAsync()` - User-friendly formatted statistics

**Data Models:**
- `Restaurant` - Core restaurant entity with Id, Name, Location, FoodType, DateAdded
- `RestaurantVisitInfo` - Visit tracking with count and last visited date
- `RestaurantData` - Container for JSON serialization of restaurants and visit counts
- `FormattedRestaurantStats` - Presentation layer for statistics display

**Technical Architecture:**
- **File Storage**: JSON persistence in `%AppData%/LunchTimeMCP/restaurants.json`
- **JSON Serialization**: Uses System.Text.Json with source generators (`RestaurantContext`)
- **Memory Management**: In-memory collections with periodic file saves
- **Initialization**: Automatic seeding with 10 trendy LA restaurants if empty

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunchTimeMCP;

public class RestaurantService
{
    private readonly string dataFilePath;
    private List<Restaurant> restaurants = new();
    private Dictionary<string, int> visitCounts = new();

    public RestaurantService()
    {
        // Store data in user's app data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appDataPath, "LunchTimeMCP");
        Directory.CreateDirectory(appDir);
        
        dataFilePath = Path.Combine(appDir, "restaurants.json");
        LoadData();
        
        // Initialize with trendy West Hollywood restaurants if empty
        if (restaurants.Count == 0)
        {
            InitializeWithTrendyRestaurants();
            SaveData();
        }
    }

    public async Task<List<Restaurant>> GetRestaurantsAsync()
    {
        return await Task.FromResult(restaurants.ToList());
    }

    public async Task<Restaurant> AddRestaurantAsync(string name, string location, string foodType)
    {
        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Location = location,
            FoodType = foodType,
            DateAdded = DateTime.UtcNow
        };

        restaurants.Add(restaurant);
        SaveData();
        
        return await Task.FromResult(restaurant);
    }

    public async Task<Restaurant?> PickRandomRestaurantAsync()
    {
        if (restaurants.Count == 0)
            return null;

        var random = new Random();
        var selectedRestaurant = restaurants[random.Next(restaurants.Count)];
        
        // Track the visit
        if (visitCounts.ContainsKey(selectedRestaurant.Id))
            visitCounts[selectedRestaurant.Id]++;
        else
            visitCounts[selectedRestaurant.Id] = 1;

        SaveData();
        
        return await Task.FromResult(selectedRestaurant);
    }

    public async Task<Dictionary<string, RestaurantVisitInfo>> GetVisitStatsAsync()
    {
        var stats = new Dictionary<string, RestaurantVisitInfo>();
        
        foreach (var restaurant in restaurants)
        {
            var visitCount = visitCounts.GetValueOrDefault(restaurant.Id, 0);
            stats[restaurant.Name] = new RestaurantVisitInfo
            {
                Restaurant = restaurant,
                VisitCount = visitCount,
                LastVisited = visitCount > 0 ? DateTime.UtcNow : null // In a real app, you'd track actual visit dates
            };
        }

        return await Task.FromResult(stats);
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
                TimesEaten = stat.VisitCount == 0 ? "Never" : 
                            stat.VisitCount == 1 ? "Once" : 
                            $"{stat.VisitCount} times"
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
        if (!File.Exists(dataFilePath))
            return;

        try
        {
            var json = File.ReadAllText(dataFilePath);
            var data = JsonSerializer.Deserialize<RestaurantData>(json, RestaurantContext.Default.RestaurantData);
            
            if (data != null)
            {
                restaurants = data.Restaurants ?? new List<Restaurant>();
                visitCounts = data.VisitCounts ?? new Dictionary<string, int>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data: {ex.Message}");
        }
    }

    private void SaveData()
    {
        try
        {
            var data = new RestaurantData
            {
                Restaurants = restaurants,
                VisitCounts = visitCounts
            };
            
            var json = JsonSerializer.Serialize(data, RestaurantContext.Default.RestaurantData);
            File.WriteAllText(dataFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving data: {ex.Message}");
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

        restaurants.AddRange(trendyRestaurants);
    }
}

public partial class Restaurant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
}

public partial class RestaurantVisitInfo
{
    public Restaurant Restaurant { get; set; } = new();
    public int VisitCount { get; set; }
    public DateTime? LastVisited { get; set; }
}

public partial class RestaurantData
{
    public List<Restaurant> Restaurants { get; set; } = new();
    public Dictionary<string, int> VisitCounts { get; set; } = new();
}

public class FormattedRestaurantStat
{
    public string Restaurant { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public string TimesEaten { get; set; } = string.Empty;
}

public class FormattedRestaurantStats
{
    public string Message { get; set; } = string.Empty;
    public List<FormattedRestaurantStat> Statistics { get; set; } = new();
    public int TotalRestaurants { get; set; }
    public int TotalVisits { get; set; }
}

[JsonSerializable(typeof(List<Restaurant>))]
[JsonSerializable(typeof(Restaurant))]
[JsonSerializable(typeof(RestaurantVisitInfo))]
[JsonSerializable(typeof(Dictionary<string, RestaurantVisitInfo>))]
[JsonSerializable(typeof(RestaurantData))]
[JsonSerializable(typeof(FormattedRestaurantStat))]
[JsonSerializable(typeof(FormattedRestaurantStats))]
internal sealed partial class RestaurantContext : JsonSerializerContext
{
}
```

### Service Architecture
The RestaurantService follows several important patterns:

**Dependency Injection Ready**
- Parameterless constructor for easy DI registration
- No external dependencies, self-contained service
- Singleton-safe with proper state management

**Data Layer Separation**
- Private methods handle file I/O (`LoadData()`, `SaveData()`)
- Public async methods provide business operations
- Clear separation between persistence and business logic

**Error Resilience**
- Graceful handling of missing or corrupt data files
- Console logging for debugging file operations
- Automatic recovery with seed data initialization

**Performance Considerations**
- In-memory operations for fast access
- Lazy loading from file system
- Minimal file I/O with batched saves

**JSON Source Generation**
- `RestaurantContext` provides AOT-friendly serialization
- All model types registered for optimal performance
- Type-safe serialization with compile-time validation

## 3.3 MCP Tools Development

Now we'll implement the MCP tools that expose our restaurant functionality to AI assistants. Create a new file called **RestaurantTools.cs** and implement each tool step by step.

### RestaurantTools.cs File Structure

Let's start by creating the basic structure for our tools.

```csharp
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace LunchTimeMCP;

[McpServerToolType]
public sealed class RestaurantTools
{
    private readonly RestaurantService restaurantService;

    public RestaurantTools(RestaurantService restaurantService)
    {
        this.restaurantService = restaurantService;
    }

    // All tools implemented here...
}
```

**Architecture Notes**:
- `[McpServerToolType]` marks the class as containing MCP tools
- Constructor injection provides access to `RestaurantService`

### Tool Implementation Overview
Each tool follows the same pattern:
1. Use `[McpServerTool]` attribute to register with MCP
2. Add descriptive `[Description]` attributes for the AI to understand the tool's purpose
3. Use dependency injection to access the `RestaurantService`
4. Return JSON-serialized responses using the `RestaurantContext`

### Tool 1: Get All Restaurants
**Purpose**: Retrieve a list of all available restaurants
**Parameters**: None
**Implementation**:

```csharp
[McpServerTool, Description("Get a list of all restaurants available for lunch.")]
public async Task<string> GetRestaurants()
{
    var restaurants = await restaurantService.GetRestaurantsAsync();
    return JsonSerializer.Serialize(restaurants, RestaurantContext.Default.ListRestaurant);
}
```

**Key Points**:
- Simple method with no parameters
- Returns JSON-serialized list of restaurants
- Uses `RestaurantContext.Default.ListRestaurant` for optimized serialization

### Tool 2: Add New Restaurant
**Purpose**: Add a new restaurant to the available options
**Parameters**: `name`, `location`, `foodType`
**Implementation**:

```csharp
[McpServerTool, Description("Add a new restaurant to the lunch options.")]
public async Task<string> AddRestaurant(
    [Description("The name of the restaurant")] string name,
    [Description("The location/address of the restaurant")] string location,
    [Description("The type of food served (e.g., Italian, Mexican, Thai, etc.)")] string foodType)
{
    var restaurant = await restaurantService.AddRestaurantAsync(name, location, foodType);
    return JsonSerializer.Serialize(restaurant, RestaurantContext.Default.Restaurant);
}
```

**Key Points**:
- Each parameter has a descriptive `[Description]` attribute
- Calls the service method to add the restaurant
- Returns the newly created restaurant object as JSON

### Tool 3: Pick Random Restaurant
**Purpose**: Select a random restaurant and track the visit
**Parameters**: None
**Implementation**:

```csharp
[McpServerTool, Description("Pick a random restaurant from the available options for lunch.")]
public async Task<string> PickRandomRestaurant()
{
    var selectedRestaurant = await restaurantService.PickRandomRestaurantAsync();
    
    if (selectedRestaurant == null)
    {
        return JsonSerializer.Serialize(new { 
            message = "No restaurants available. Please add some restaurants first!" 
        });
    }

    return JsonSerializer.Serialize(new { 
        message = $"🍽️ Time for lunch at {selectedRestaurant.Name}!",
        restaurant = selectedRestaurant 
    });
}
```

**Key Points**:
- Handles the case when no restaurants are available
- Returns a friendly message with emoji
- Includes both a message and the selected restaurant data
- Automatically tracks the visit in the service

### Tool 4: Get Visit Statistics
**Purpose**: Retrieve formatted statistics about restaurant visits
**Parameters**: None
**Implementation**:

```csharp
[McpServerTool, Description("Get statistics about how many times each restaurant has been visited.")]
public async Task<string> GetVisitStatistics()
{
    var formattedStats = await restaurantService.GetFormattedVisitStatsAsync();
    return JsonSerializer.Serialize(formattedStats, RestaurantContext.Default.FormattedRestaurantStats);
}
```

**Key Points**:
- Uses the formatted stats method for user-friendly output
- Returns comprehensive statistics including visit counts and totals
- Leverages the pre-built formatting logic in the service

### Register Tools & Services in Program.cs
Finally, we need to register our tools and the service in the `Program.cs` file. Update your `Program.cs` to include:

```csharp
builder.Services.AddSingleton<RestaurantService>();
```

Add the tool registration line to the server configuration:

```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RestaurantTools>();
```

The finished file will look like this:

```csharp
using LunchTimeMCP;
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
```

## 3.4 Testing Your MCP Server

### Local Testing with MCP Inspector

**Step 1: Navigate to Your Project**
```bash
cd LunchTimeMCP
```

**Step 2: Build and Test Your Server**
```bash
dotnet build
```

**Step 3: Install and Run MCP Inspector**

**Install MCP Inspector with npm**
```bash
npm install -g @modelcontextprotocol/inspector
```

**`Run MCP Inspector with npx**
```bash
npx @modelcontextprotocol/inspector dotnet run
```

When run you will receive a URL with a pre-filled token to open in the browser.

**Step 4: Connect to Your MCP Server**
1. In the MCP Inspector web interface:
   - **Transport Type**: Select "STDIO" 
   - **Command**: `dotnet`
   - **Arguments**: `run`
2. Click "Connect" to establish the connection
3. Click **List Tools**
3. You should see your four tools appear in the inspector:
   - `GetRestaurants`
   - `AddRestaurant` 
   - `PickRandomRestaurant`
   - `GetVisitStatistics`

**Step 5: Test Each Tool**
- **GetRestaurants**: Should return the 10 seeded restaurants
- **AddRestaurant**: Try adding a new restaurant with name, location, and food type
- **PickRandomRestaurant**: Should select and track a random restaurant
- **GetVisitStatistics**: Should show access counts for visited restaurants

**Troubleshooting**:
- Ensure you're in the correct project directory
- Verify `dotnet run` works standalone before using MCP Inspector
- Check the console output for any error messages
- Make sure the stdio transport is selected (not TCP or other options)

### Integration with AI Assistants

#### VS Code Configuration with GitHub Copilot

To integrate your MCP server with VS Code and GitHub Copilot, you'll need to create a configuration file that tells VS Code how to connect to your server.

**Step 1: Create MCP Configuration File**

Create a `.vscode` folder in your workspace root (if it doesn't exist) and add a `mcp.json` file:

```json
{
    "inputs": [],
    "servers": {
        "lunchroulette": {
            "type": "stdio",
            "command": "dotnet",
            "args": [
                "run",
                "--project",
                "PATH_TO_YOUR_PROJECT\\LunchTimeMCP.csproj"
            ],
            "env": {}
        }
    }
}
```

**Important**: Update the project path in the `args` array to match your actual project location. The path should point to your `.csproj` file. You can right click the project in VS Code and select "Copy Path" to get the correct path. Make sure you use double backslashes (`\\`) in the path for Windows or single forward slashes (`/`) for Unix-based systems.

**Step 2: Configuration Details**

- **`lunchroulette`**: A unique identifier for your MCP server
- **`type`**: Set to "stdio" for standard input/output communication
- **`command`**: The executable command (`dotnet`)
- **`args`**: Arguments passed to the command, including the project path
- **`env`**: Environment variables (empty object for default environment)

**Step 3: Verify Configuration**

1. In the **mcp.json** file you will see a **Start** button become available over the server name.
2. Click the **Start** button to launch your MCP server.
3. If everything is set up correctly, you should see the MCP server start, and it will be ready to accept commands from GitHub Copilot chat.
4. Open the GitHub Copilot chat in VS Code and ensure it recognizes your MCP server.
5. Click on the tools icon in Agent mode to see all available tools.

**Step 4: Using Your MCP Server in VS Code**

Once configured, you can interact with your lunch roulette server through GitHub Copilot chat:

- **"Pick a random restaurant for lunch"**
- **"Add a new restaurant called 'Spago' in Beverly Hills serving Californian cuisine"**
- **"Show me all available restaurants"**
- **"Get statistics on restaurant visits"**

You can also use the `#` prefix to directly call tools in the chat, like `#GetRestaurants` or `#PickRandomRestaurant`.

---

> **Congratulations!** You've completed the MCP with C#.
