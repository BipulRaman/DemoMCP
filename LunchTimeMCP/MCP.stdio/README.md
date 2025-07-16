# MCP.stdio - Standard I/O MCP Server

## Overview
This project implements a **Model Context Protocol (MCP) server** using **Standard I/O (stdio)** transport. It provides restaurant management functionality through the official MCP protocol, making it compatible with AI assistants like Claude Desktop, VS Code with GitHub Copilot, and other MCP-enabled applications.

## Architecture

### Standard I/O Implementation
- **MCP Protocol**: Official Model Context Protocol specification
- **JSON-RPC 2.0**: Standard protocol for communication
- **Stdio Transport**: Communication via standard input/output streams
- **Single Process**: Designed for direct integration with AI assistants

### Key Components

#### Program.cs
- **Host Configuration**: Sets up the MCP server with stdio transport
- **Dependency Injection**: Registers services and tools
- **MCP Server Setup**: Configures the official MCP library

#### RestaurantService.cs
- **Business Logic**: Core restaurant management functionality
- **Data Persistence**: JSON file storage in user's AppData directory
- **Async Operations**: All methods use async/await pattern

#### RestaurantTools.cs
- **MCP Tools**: Exposes restaurant functionality as MCP tools
- **Tool Definitions**: Implements the four core restaurant tools
- **JSON Serialization**: Handles request/response formatting

## Features

### MCP Tools Available
1. **GetRestaurants** - Retrieve all restaurants from the database
2. **AddRestaurant** - Add a new restaurant with name, location, and food type
3. **PickRandomRestaurant** - Randomly select a restaurant and track the visit
4. **GetVisitStatistics** - Get formatted statistics about restaurant visits

### Core Functionality
- **Restaurant Management**: Add, retrieve, and track restaurants
- **Visit Tracking**: Count and statistics for restaurant selections
- **Data Persistence**: JSON file storage with automatic backup
- **Seed Data**: Pre-populated with 10 trendy West Hollywood restaurants
- **Error Handling**: Comprehensive error handling and logging

## Configuration

### Required Packages
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.6" />
    <PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.1" />
    <PackageReference Include="System.Text.Json" Version="9.0.6" />
</ItemGroup>
```

### Program.cs Setup
```csharp
using LunchTimeMCP;
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
```

## Running the Server

### Command Line Execution
```bash
cd MCP.stdio
dotnet run
```

The server will start and wait for MCP protocol messages via stdin/stdout.

### Testing with MCP Inspector
```bash
# Install MCP Inspector
npm install -g @modelcontextprotocol/inspector

# Run with MCP Inspector
npx @modelcontextprotocol/inspector dotnet run
```

## Integration with AI Assistants

### VS Code with GitHub Copilot

Create a `.vscode/mcp.json` file in your workspace:

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
                "PATH_TO_YOUR_PROJECT\\MCP.stdio\\MCP.stdio.csproj"
            ],
            "env": {}
        }
    }
}
```

**Important**: Update the project path to match your actual file system location.

### Claude Desktop

Add to your Claude Desktop configuration:

```json
{
  "mcpServers": {
    "lunchroulette": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "PATH_TO_YOUR_PROJECT\\MCP.stdio\\MCP.stdio.csproj"
      ]
    }
  }
}
```

## Tool Definitions

### GetRestaurants
- **Purpose**: Retrieve all available restaurants
- **Parameters**: None
- **Returns**: JSON array of restaurant objects

### AddRestaurant
- **Purpose**: Add a new restaurant to the database
- **Parameters**:
  - `name` (string): The name of the restaurant
  - `location` (string): The location/address of the restaurant
  - `foodType` (string): The type of food served (e.g., Italian, Mexican, Thai)
- **Returns**: The newly created restaurant object

### PickRandomRestaurant
- **Purpose**: Select a random restaurant and track the visit
- **Parameters**: None
- **Returns**: Selected restaurant with a friendly message

### GetVisitStatistics
- **Purpose**: Retrieve formatted statistics about restaurant visits
- **Parameters**: None
- **Returns**: Comprehensive visit statistics with counts and totals

## Data Storage

Restaurant data is stored in JSON format in the user's application data directory:
- **Windows**: `%APPDATA%\LunchTimeMCP\restaurants.json`
- **macOS/Linux**: `~/.config/LunchTimeMCP/restaurants.json`

### Data Structure
```json
{
  "restaurants": [
    {
      "id": "guid",
      "name": "Restaurant Name",
      "location": "Address",
      "foodType": "Cuisine Type",
      "dateAdded": "2024-01-15T10:30:00Z"
    }
  ],
  "visitCounts": {
    "restaurant-id": 5
  }
}
```

## Usage Examples

### Using with GitHub Copilot in VS Code
After setting up the MCP configuration:

- **"Pick a random restaurant for lunch"**
- **"Add a new restaurant called 'Spago' in Beverly Hills serving Californian cuisine"**
- **"Show me all available restaurants"**
- **"Get statistics on restaurant visits"**

### Direct MCP Protocol Communication
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "GetRestaurants"
  }
}
```

## Architecture Benefits

### Advantages of Stdio Transport
- **Direct Integration**: Works seamlessly with AI assistants
- **Simple Protocol**: Standard input/output communication
- **Lightweight**: Minimal overhead and resource usage
- **Secure**: No network exposure or ports to manage

### MCP Library Benefits
- **Protocol Compliance**: Official MCP specification implementation
- **Automatic Handling**: Request/response parsing and validation
- **Tool Registration**: Automatic discovery and registration
- **Error Management**: Built-in error handling and responses

## Differences from MCP.sse

| Feature | MCP.stdio | MCP.sse |
|---------|-----------|---------|
| Transport | Standard I/O | HTTP/SSE |
| Protocol | JSON-RPC over stdio | JSON-RPC over HTTP |
| Real-time | Not supported | Server-Sent Events |
| Web Integration | Not supported | Full support |
| AI Assistant Support | Native | Configuration required |
| Debugging | Console logs | Web dev tools |
| Scalability | Single process | Multiple connections |
| Setup Complexity | Simple | Moderate |

## Development Notes

### MCP Tool Attributes
- **`[McpServerToolType]`**: Marks a class as containing MCP tools
- **`[McpServerTool]`**: Registers a method as an MCP tool
- **`[Description]`**: Provides tool and parameter descriptions for AI

### Dependency Injection
- Services are registered as singletons for data consistency
- Constructor injection provides access to required services
- Tools are automatically discovered and registered

### JSON Serialization
- Uses System.Text.Json with source generators
- Optimized for performance with AOT compilation
- Type-safe serialization with compile-time validation

## Troubleshooting

### Common Issues
1. **"Server not found"**: Check file paths in MCP configuration
2. **"Tool not available"**: Verify tool registration in Program.cs
3. **"Data not persisting"**: Check write permissions to AppData directory

### Debugging
- Add console logging to track MCP communication
- Use MCP Inspector for interactive testing
- Verify JSON serialization with test data

### Performance Tips
- Use async/await for all I/O operations
- Minimize file system access with in-memory caching
- Use source generators for optimal JSON performance

## Future Enhancements
- Resource support for rich content
- Prompt templates for common operations
- Enhanced error reporting and validation
- Performance monitoring and metrics
- Additional restaurant management features
