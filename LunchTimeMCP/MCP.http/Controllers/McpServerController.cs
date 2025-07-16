using MCP.http.Models;
using MCP.http.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MCP.http.Controllers;

/// <summary>
/// Pure MCP (Model Context Protocol) server implementation using JSON-RPC 2.0 with streaming capabilities
/// </summary>
[ApiController]
[Route("mcp")]
[Produces("application/json")]
public class McpServerController : ControllerBase
{
    private readonly RestaurantService _restaurantService;
    private readonly ILogger<McpServerController> _logger;

    public McpServerController(RestaurantService restaurantService, ILogger<McpServerController> logger)
    {
        _restaurantService = restaurantService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleMcpRequest([FromBody] JsonRpcRequest request)
    {
        try
        {
            _logger.LogInformation("Received MCP request: {Method} with ID: {Id}", request.Method, request.Id);

            // Handle notifications (requests without ID)
            if (request.Id == null && request.Method == "notifications/initialized")
            {
                _logger.LogInformation("Client initialization complete");
                return Ok(); // No response for notifications
            }

            var response = request.Method switch
            {
                "initialize" => (object)HandleInitialize(request),
                "tools/list" => (object)HandleToolsList(request),
                "tools/call" => (object)await HandleToolCall(request),
                "prompts/list" => (object)HandlePromptsList(request),
                "prompts/get" => (object)HandlePromptsGet(request),
                "resources/list" => (object)HandleResourcesList(request),
                _ => (object)CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}")
            };

            _logger.LogInformation("Sending response for method: {Method}", request.Method);
            return Ok(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in MCP request");
            return Ok(CreateErrorResponse(request?.Id, -32700, "Parse error", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return Ok(CreateErrorResponse(request?.Id, -32603, "Internal error", ex.Message));
        }
    }

    /// <summary>
    /// HTTP GET endpoint for MCP initialization (for direct HTTP access)
    /// </summary>
    [HttpGet("initialize")]
    public IActionResult GetInitialize()
    {
        try
        {
            _logger.LogInformation("Received HTTP GET request for MCP initialization");
            
            var result = new McpInitializeResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new McpServerCapabilities
                {
                    Tools = new McpToolsCapability { ListChanged = false, Streaming = true },
                    Prompts = new McpPromptsCapability { ListChanged = false },
                    Resources = new McpResourcesCapability { ListChanged = false },
                    Streaming = new McpStreamingServerCapability
                    {
                        Supported = true,
                        Protocols = new[] { "sse", "chunked-json" },
                        ToolStreaming = true,
                        PromptStreaming = true
                    }
                },
                ServerInfo = new McpServerInfo
                {
                    Name = "LunchTime MCP Streaming Server",
                    Version = "1.0.0",
                    Description = "Streamable HTTP-based MCP server for lunch restaurant management"
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GET initialize request");
            var errorResponse = CreateErrorResponse(null, -32603, "Internal error", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// HTTP GET endpoint to list available tools (for direct HTTP access)
    /// </summary>
    [HttpGet("tools")]
    public IActionResult GetTools()
    {
        try
        {
            _logger.LogInformation("Received HTTP GET request for tools list");
            
            var tools = new McpTool[]
            {
                new()
                {
                    Name = "get_restaurants",
                    Description = "Get a list of all restaurants available for lunch.",
                    Streaming = false,
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>(),
                        additionalProperties = false
                    }
                },
                new()
                {
                    Name = "get_restaurants_stream",
                    Description = "Stream a list of all restaurants progressively with real-time loading.",
                    Streaming = true,
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>(),
                        additionalProperties = false
                    }
                },
                new()
                {
                    Name = "add_restaurant",
                    Description = "Add a new restaurant to the lunch options.",
                    Streaming = false,
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new 
                            { 
                                type = "string", 
                                description = "The name of the restaurant",
                                minLength = 1,
                                maxLength = 100
                            },
                            location = new 
                            { 
                                type = "string", 
                                description = "The location/address of the restaurant",
                                minLength = 1,
                                maxLength = 200
                            },
                            foodType = new 
                            { 
                                type = "string", 
                                description = "The type of food served (e.g., Italian, Mexican, Thai, etc.)",
                                minLength = 1,
                                maxLength = 50
                            }
                        },
                        required = new[] { "name", "location", "foodType" },
                        additionalProperties = false
                    }
                },
                new()
                {
                    Name = "analyze_restaurants_stream",
                    Description = "Stream progressive analysis of restaurant data including cuisine distribution and insights.",
                    Streaming = true,
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            type = new
                            {
                                type = "string",
                                description = "Type of analysis to perform (general, cuisine, location)",
                                @enum = new[] { "general", "cuisine", "location" },
                                @default = "general"
                            }
                        },
                        required = Array.Empty<string>(),
                        additionalProperties = false
                    }
                },
                new()
                {
                    Name = "search_restaurants_stream",
                    Description = "Stream real-time search results for restaurants based on query criteria.",
                    Streaming = true,
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new
                            {
                                type = "string",
                                description = "Search query for restaurant name, location, or cuisine type",
                                minLength = 1,
                                maxLength = 100
                            }
                        },
                        required = new[] { "query" },
                        additionalProperties = false
                    }
                },
                new()
                {
                    Name = "pick_random_restaurant",
                    Description = "Pick a random restaurant from the available options for lunch.",
                    Streaming = false,
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>(),
                        additionalProperties = false
                    }
                },
                new()
                {
                    Name = "get_visit_statistics",
                    Description = "Get statistics about how many times each restaurant has been visited.",
                    Streaming = false,
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>(),
                        additionalProperties = false
                    }
                }
            };

            var result = new McpToolsListResult
            {
                Tools = tools
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GET tools request");
            var errorResponse = CreateErrorResponse(null, -32603, "Internal error", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        var result = new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new McpServerCapabilities
            {
                Tools = new McpToolsCapability { ListChanged = false, Streaming = true },
                Prompts = new McpPromptsCapability { ListChanged = false },
                Resources = new McpResourcesCapability { ListChanged = false },
                Streaming = new McpStreamingServerCapability
                {
                    Supported = true,
                    Protocols = new[] { "sse", "chunked-json" },
                    ToolStreaming = true,
                    PromptStreaming = true
                }
            },
            ServerInfo = new McpServerInfo
            {
                Name = "LunchTime MCP Streaming Server",
                Version = "1.0.0",
                Description = "Streamable HTTP-based MCP server for lunch restaurant management"
            }
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var tools = new McpTool[]
        {
            new()
            {
                Name = "get_restaurants",
                Description = "Get a list of all restaurants available for lunch.",
                Streaming = false,
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>(),
                    additionalProperties = false
                }
            },
            new()
            {
                Name = "get_restaurants_stream",
                Description = "Stream a list of all restaurants progressively with real-time loading.",
                Streaming = true,
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>(),
                    additionalProperties = false
                }
            },
            new()
            {
                Name = "add_restaurant",
                Description = "Add a new restaurant to the lunch options.",
                Streaming = false,
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new 
                        { 
                            type = "string", 
                            description = "The name of the restaurant",
                            minLength = 1,
                            maxLength = 100
                        },
                        location = new 
                        { 
                            type = "string", 
                            description = "The location/address of the restaurant",
                            minLength = 1,
                            maxLength = 200
                        },
                        foodType = new 
                        { 
                            type = "string", 
                            description = "The type of food served (e.g., Italian, Mexican, Thai, etc.)",
                            minLength = 1,
                            maxLength = 50
                        }
                    },
                    required = new[] { "name", "location", "foodType" },
                    additionalProperties = false
                }
            },
            new()
            {
                Name = "analyze_restaurants_stream",
                Description = "Stream progressive analysis of restaurant data including cuisine distribution and insights.",
                Streaming = true,
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        type = new
                        {
                            type = "string",
                            description = "Type of analysis to perform (general, cuisine, location)",
                            @enum = new[] { "general", "cuisine", "location" },
                            @default = "general"
                        }
                    },
                    required = Array.Empty<string>(),
                    additionalProperties = false
                }
            },
            new()
            {
                Name = "search_restaurants_stream",
                Description = "Stream real-time search results for restaurants based on query criteria.",
                Streaming = true,
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "Search query for restaurant name, location, or cuisine type",
                            minLength = 1,
                            maxLength = 100
                        }
                    },
                    required = new[] { "query" },
                    additionalProperties = false
                }
            },
            new()
            {
                Name = "pick_random_restaurant",
                Description = "Pick a random restaurant from the available options for lunch.",
                Streaming = false,
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>(),
                    additionalProperties = false
                }
            },
            new()
            {
                Name = "get_visit_statistics",
                Description = "Get statistics about how many times each restaurant has been visited.",
                Streaming = false,
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>(),
                    additionalProperties = false
                }
            }
        };

        var result = new McpToolsListResult
        {
            Tools = tools
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    private async Task<object> HandleToolCall(JsonRpcRequest request)
    {
        try
        {
            var paramsElement = (JsonElement)request.Params!;
            var toolCallParams = JsonSerializer.Deserialize<McpToolCallParams>(paramsElement.GetRawText());

            if (toolCallParams == null)
            {
                return CreateErrorResponse(request.Id, -32602, "Invalid params");
            }

            // Check if this is a streaming tool call request
            if (toolCallParams.Streaming == true || IsStreamingTool(toolCallParams.Name))
            {
                // For streaming tools, return a response that directs to streaming endpoints
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = $"🔄 **Streaming Tool: {toolCallParams.Name}**\n\n" +
                                       "This tool supports streaming responses. Use the streaming endpoints for real-time data:\n\n" +
                                       "📡 **Server-Sent Events:** POST /mcp/stream/tools/call/sse\n" +
                                       "🔗 **Chunked JSON:** POST /mcp/stream/tools/call/chunked\n" +
                                       "📊 **Restaurant Stream:** GET /mcp/stream/restaurants/sse\n\n" +
                                       "💡 Check /mcp/stream/capabilities for detailed streaming information."
                            }
                        },
                        Metadata = new McpToolCallMetadata
                        {
                            Timestamp = DateTime.UtcNow,
                            Streaming = new StreamingMetadata
                            {
                                ChunkNumber = 1,
                                TotalChunks = 1,
                                IsLast = true
                            }
                        }
                    }
                };
            }

            var result = await ExecuteTool(toolCallParams.Name, toolCallParams.Arguments);
            
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call");
            return CreateErrorResponse(request.Id, -32603, "Tool execution error", ex.Message);
        }
    }

    private bool IsStreamingTool(string toolName)
    {
        var streamingTools = new[] { "get_restaurants_stream", "analyze_restaurants_stream", "search_restaurants_stream" };
        return streamingTools.Contains(toolName.ToLowerInvariant());
    }

    private async Task<McpToolCallResult> ExecuteTool(string toolName, Dictionary<string, object>? arguments)
    {
        switch (toolName.ToLowerInvariant())
        {
            case "get_restaurants":
                var restaurants = await _restaurantService.GetRestaurantsAsync();
                if (!restaurants.Any())
                {
                    return new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = "📭 No restaurants found. Add some restaurants first using the add_restaurant tool!"
                            }
                        }
                    };
                }

                var restaurantList = string.Join("\n\n", restaurants.Select((r, i) => 
                    $"**{i + 1}. {r.Name}**\n" +
                    $"🏢 Location: {r.Location}\n" +
                    $"🍽️ Food Type: {r.FoodType}\n" +
                    $"🆔 ID: {r.Id}\n" +
                    $"📅 Added: {r.DateAdded:yyyy-MM-dd HH:mm:ss} UTC"));

                return new McpToolCallResult
                {
                    Content = new[]
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = $"🍽️ **Available Restaurants ({restaurants.Count()} total):**\n\n{restaurantList}"
                        }
                    }
                };

            case "add_restaurant":
                if (arguments == null ||
                    !arguments.TryGetValue("name", out var nameObj) ||
                    !arguments.TryGetValue("location", out var locationObj) ||
                    !arguments.TryGetValue("foodType", out var foodTypeObj))
                {
                    return new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = "❌ Error: Missing required parameters. Please provide: name, location, and foodType"
                            }
                        },
                        IsError = true
                    };
                }

                var name = nameObj?.ToString()?.Trim() ?? "";
                var location = locationObj?.ToString()?.Trim() ?? "";
                var foodType = foodTypeObj?.ToString()?.Trim() ?? "";

                // Validate input lengths
                if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                {
                    return new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = "❌ Error: Restaurant name must be between 1 and 100 characters"
                            }
                        },
                        IsError = true
                    };
                }

                if (string.IsNullOrWhiteSpace(location) || location.Length > 200)
                {
                    return new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = "❌ Error: Location must be between 1 and 200 characters"
                            }
                        },
                        IsError = true
                    };
                }

                if (string.IsNullOrWhiteSpace(foodType) || foodType.Length > 50)
                {
                    return new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = "❌ Error: Food type must be between 1 and 50 characters"
                            }
                        },
                        IsError = true
                    };
                }

                var restaurant = await _restaurantService.AddRestaurantAsync(name, location, foodType);
                return new McpToolCallResult
                {
                    Content = new[]
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = $"✅ Successfully added restaurant:\n\n📍 **{restaurant.Name}**\n🏢 Location: {restaurant.Location}\n🍽️ Food Type: {restaurant.FoodType}\n🆔 ID: {restaurant.Id}\n📅 Added: {restaurant.DateAdded:yyyy-MM-dd HH:mm:ss} UTC"
                        }
                    }
                };

            case "pick_random_restaurant":
                var selectedRestaurant = await _restaurantService.PickRandomRestaurantAsync();

                if (selectedRestaurant == null)
                {
                    return new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = "😔 No restaurants available. Please add some restaurants first using the add_restaurant tool!"
                            }
                        }
                    };
                }

                return new McpToolCallResult
                {
                    Content = new[]
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = $"🎲 **Time for lunch at {selectedRestaurant.Name}!**\n\n" +
                                   $"📍 **Selected Restaurant:**\n" +
                                   $"🏢 Location: {selectedRestaurant.Location}\n" +
                                   $"🍽️ Food Type: {selectedRestaurant.FoodType}\n" +
                                   $"🆔 ID: {selectedRestaurant.Id}\n" +
                                   $"📅 Added: {selectedRestaurant.DateAdded:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                                   $"Enjoy your meal! 🍽️"
                        }
                    }
                };

            case "get_visit_statistics":
                var stats = await _restaurantService.GetFormattedVisitStatsAsync();
                if (stats == null || !stats.Statistics.Any())
                {
                    return new McpToolCallResult
                    {
                        Content = new[]
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = "📊 No visit statistics available yet. Start picking restaurants to generate statistics!"
                            }
                        }
                    };
                }

                var statsText = string.Join("\n", stats.Statistics.Select((stat, i) => 
                    $"**{i + 1}. {stat.Restaurant}** - {stat.VisitCount} visits"));

                return new McpToolCallResult
                {
                    Content = new[]
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = $"📊 **Restaurant Visit Statistics:**\n\n{statsText}\n\n" +
                                   $"📈 **Summary:** {stats.TotalVisits} total visits across {stats.TotalRestaurants} restaurants"
                        }
                    }
                };

            default:
                return new McpToolCallResult
                {
                    Content = new[]
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = $"Error: Unknown tool: {toolName}"
                        }
                    },
                    IsError = true
                };
        }
    }

    private JsonRpcResponse HandlePromptsList(JsonRpcRequest request)
    {
        var prompts = new McpPrompt[]
        {
            new()
            {
                Name = "lunch_decision_helper",
                Description = "Get help deciding where to go for lunch based on your preferences and mood.",
                Arguments = new[]
                {
                    new McpPromptArgument 
                    { 
                        Name = "mood", 
                        Description = "Your current mood (e.g., adventurous, comfort, healthy, quick)", 
                        Required = false 
                    },
                    new McpPromptArgument 
                    { 
                        Name = "dietary_restrictions", 
                        Description = "Any dietary restrictions or preferences", 
                        Required = false 
                    }
                }
            },
            new()
            {
                Name = "restaurant_explorer",
                Description = "Explore restaurant options and get detailed information about different cuisines.",
                Arguments = new[]
                {
                    new McpPromptArgument 
                    { 
                        Name = "cuisine_type", 
                        Description = "Type of cuisine you're interested in exploring", 
                        Required = false 
                    }
                }
            },
            new()
            {
                Name = "lunch_planning",
                Description = "Plan your lunch schedule and track your restaurant visits over time.",
                Arguments = Array.Empty<McpPromptArgument>()
            },
            new()
            {
                Name = "add_new_restaurant",
                Description = "Get guided help to add a new restaurant to your lunch options.",
                Arguments = Array.Empty<McpPromptArgument>()
            }
        };

        var result = new McpPromptsListResult
        {
            Prompts = prompts
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    private object HandlePromptsGet(JsonRpcRequest request)
    {
        try
        {
            var paramsElement = (JsonElement)request.Params!;
            var promptParams = JsonSerializer.Deserialize<McpPromptGetParams>(paramsElement.GetRawText());

            if (promptParams == null)
            {
                return CreateErrorResponse(request.Id, -32602, "Invalid params");
            }

            var result = promptParams.Name.ToLowerInvariant() switch
            {
                "lunch_decision_helper" => CreateLunchDecisionHelperPrompt(promptParams.Arguments),
                "restaurant_explorer" => CreateRestaurantExplorerPrompt(promptParams.Arguments),
                "lunch_planning" => CreateLunchPlanningPrompt(),
                "add_new_restaurant" => CreateAddNewRestaurantPrompt(),
                _ => throw new ArgumentException($"Unknown prompt: {promptParams.Name}")
            };

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling prompts/get request");
            return CreateErrorResponse(request.Id, -32603, "Error retrieving prompt", ex.Message);
        }
    }

    private McpPromptGetResult CreateLunchDecisionHelperPrompt(Dictionary<string, string>? arguments)
    {
        var mood = arguments?.GetValueOrDefault("mood", "undecided");
        var dietaryRestrictions = arguments?.GetValueOrDefault("dietary_restrictions", "none");

        var promptText = $@"🍽️ **Lunch Decision Helper**

I need help deciding where to go for lunch today! Here are my preferences:

**Current Mood:** {mood}
**Dietary Restrictions:** {dietaryRestrictions}

Please help me by:
1. First, showing me all available restaurants
2. Then, picking a random restaurant that might match my mood
3. Finally, showing me the visit statistics to see if I should try somewhere new

Let's make this lunch decision fun and delicious! 🎯";

        return new McpPromptGetResult
        {
            Description = "A personalized lunch decision helper based on your mood and preferences",
            Messages = new[]
            {
                new McpPromptMessage
                {
                    Role = "user",
                    Content = new McpContent
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            }
        };
    }

    private McpPromptGetResult CreateRestaurantExplorerPrompt(Dictionary<string, string>? arguments)
    {
        var cuisineType = arguments?.GetValueOrDefault("cuisine_type", "any type");

        var promptText = $@"🌍 **Restaurant Explorer**

I want to explore the lunch options available to me! I'm particularly interested in {cuisineType} of cuisine.

Please help me by:
1. Showing me all available restaurants with detailed information
2. Filtering or highlighting restaurants that match my cuisine preference
3. Providing visit statistics so I can see which places I haven't tried yet

I'm ready to discover some great lunch spots! ✨";

        return new McpPromptGetResult
        {
            Description = "Explore and discover restaurant options based on cuisine preferences",
            Messages = new[]
            {
                new McpPromptMessage
                {
                    Role = "user",
                    Content = new McpContent
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            }
        };
    }

    private McpPromptGetResult CreateLunchPlanningPrompt()
    {
        var promptText = @"📅 **Lunch Planning & Analytics**

I want to analyze and plan my lunch habits! Please help me by:

1. Showing me comprehensive visit statistics for all restaurants
2. Identifying restaurants I haven't visited yet or haven't been to in a while
3. Displaying all available restaurant options with their details
4. Suggesting a strategy for trying new places or revisiting favorites

Let's make my lunch planning data-driven and exciting! 📊";

        return new McpPromptGetResult
        {
            Description = "Plan and analyze your lunch patterns and restaurant visits",
            Messages = new[]
            {
                new McpPromptMessage
                {
                    Role = "user",
                    Content = new McpContent
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            }
        };
    }

    private McpPromptGetResult CreateAddNewRestaurantPrompt()
    {
        var promptText = @"➕ **Add New Restaurant**

I want to add a new restaurant to my lunch options! I have a place in mind but need guidance on how to add it properly.

Please help me by:
1. Explaining what information I need to provide (name, location, food type)
2. Guiding me through the process of adding the restaurant
3. Showing me examples of well-formatted restaurant entries
4. After adding, displaying the updated list of restaurants

Let's expand my lunch horizons with a new great option! 🎉";

        return new McpPromptGetResult
        {
            Description = "Get guided assistance for adding new restaurants to your lunch options",
            Messages = new[]
            {
                new McpPromptMessage
                {
                    Role = "user",
                    Content = new McpContent
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            }
        };
    }

    private JsonRpcErrorResponse CreateErrorResponse(object? id, int code, string message, object? data = null)
    {
        return new JsonRpcErrorResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            }
        };
    }

    private JsonRpcResponse HandleResourcesList(JsonRpcRequest request)
    {
        // MCP servers are required to support resources/list even if they don't provide any resources
        var result = new McpResourcesListResult
        {
            Resources = Array.Empty<McpResource>()
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    /// <summary>
    /// HTTP GET endpoint to list available resources (for direct HTTP access)
    /// </summary>
    [HttpGet("resources")]
    public IActionResult GetResources()
    {
        try
        {
            _logger.LogInformation("Received HTTP GET request for resources list");
            
            // This server doesn't provide any resources, but the endpoint must exist per MCP spec
            var result = new McpResourcesListResult
            {
                Resources = Array.Empty<McpResource>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GET resources request");
            var errorResponse = CreateErrorResponse(null, -32603, "Internal error", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }
}
