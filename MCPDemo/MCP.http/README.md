# MCP.http - Restaurant Management HTTP Server

This is an HTTP-based Model Context Protocol (MCP) server that manages restaurant choices for lunch decisions. It follows the pattern established in the samples directory, providing a clean HTTP transport layer with JSON-RPC 2.0 protocol support.

## Features

The server provides several tools for managing and discovering restaurants:

### Core Restaurant Management
- **GetRestaurants**: Get a list of all available restaurants
- **AddRestaurant**: Add a new restaurant with name, location, and food type
- **PickRandomRestaurant**: Randomly select a restaurant for lunch
- **GetVisitStatistics**: View statistics about restaurant visits

### HTTP-Enhanced Features
Following the pattern from the samples directory, this implementation includes:

- **GetRestaurantsStream**: Stream restaurant data with progressive loading
- **AnalyzeRestaurantsStream**: Real-time analysis of restaurant data (cuisine, location, popularity, general)
- **SearchRestaurantsStream**: Stream search results as they're found
- **GetNearbyRestaurants**: Simulated external API integration for nearby restaurants
- **GetRestaurantReviews**: Simulated external API for restaurant reviews

## Testing with VS Code and GitHub Copilot

This section demonstrates how to test the MCP server directly within VS Code using MCP JSON configuration and GitHub Copilot integration.

### Prerequisites

1. **VS Code** with GitHub Copilot extension installed
2. **MCP for VS Code Extension** (if available)
3. **REST Client Extension** for VS Code (alternative approach)

### Method 1: Using MCP JSON Configuration

#### 1. Create MCP Configuration File

Create a `.vscode/mcp-config.json` file in your workspace:

```json
{
  "mcpServers": {
    "restaurant-server": {
      "command": "dotnet",
      "args": ["run", "--project", "MCP.http"],
      "transport": "http",
      "url": "http://localhost:7072/mcp",
      "name": "Restaurant Management Server",
      "description": "Local restaurant management MCP server for testing"
    }
  }
}
```

#### 2. VS Code Settings Configuration

Add to your `.vscode/settings.json`:

```json
{
  "mcp.servers": {
    "restaurant-server": {
      "transport": "http",
      "url": "http://localhost:7072/mcp",
      "autoStart": true
    }
  },
  "github.copilot.enable": {
    "*": true,
    "mcp": true
  }
}
```

#### 3. Testing with GitHub Copilot Chat

Once configured, you can test the MCP server through GitHub Copilot Chat:

1. **Open Copilot Chat** (`Ctrl+Shift+I` or `Cmd+Shift+I`)
2. **Use MCP Tools** through natural language:

```
@workspace Can you help me add a new restaurant called "Taco Bell" in location "Food Court" with food type "Mexican" using the restaurant server?
```

```
@workspace Show me all available restaurants from the restaurant management server
```

```
@workspace Pick a random restaurant for lunch today
```

#### 4. Advanced Copilot Integration

GitHub Copilot can understand and interact with your MCP tools:

```
@workspace I'm looking for Italian restaurants. Can you:
1. Show me current Italian restaurants
2. If none exist, add "Mario's Pizza" as an Italian restaurant downtown
3. Then pick between the Italian options
```

### Method 2: Using REST Client Extension

#### 1. Install REST Client Extension

Install the "REST Client" extension by Huachao Mao from the VS Code marketplace.

#### 2. Create Test File

Create a `test-mcp-server.http` file in your workspace:

```http
### Variables
@baseUrl = http://localhost:7072
@contentType = application/json

### Health Check
GET {{baseUrl}}/health

### List Available Tools
POST {{baseUrl}}
Content-Type: {{contentType}}

{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list"
}

### Add Restaurant - Pizza Palace
POST {{baseUrl}}
Content-Type: {{contentType}}

{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "AddRestaurant",
    "arguments": {
      "name": "Pizza Palace",
      "location": "Downtown",
      "foodType": "Italian"
    }
  }
}

### Get All Restaurants
POST {{baseUrl}}
Content-Type: {{contentType}}

{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "GetRestaurants",
    "arguments": {}
  }
}

### Pick Random Restaurant
POST {{baseUrl}}
Content-Type: {{contentType}}

{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "PickRandomRestaurant",
    "arguments": {}
  }
}

### Get Visit Statistics
POST {{baseUrl}}
Content-Type: {{contentType}}

{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "GetVisitStatistics",
    "arguments": {}
  }
}

### Test Streaming - Get Restaurants Stream
POST {{baseUrl}}
Content-Type: {{contentType}}

{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "tools/call",
  "params": {
    "name": "GetRestaurantsStream",
    "arguments": {}
  }
}

### Test External API - Get Nearby Restaurants
POST {{baseUrl}}
Content-Type: {{contentType}}

{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "GetNearbyRestaurants",
    "arguments": {
      "latitude": 40.7128,
      "longitude": -74.0060,
      "radius": 1000
    }
  }
}
```

#### 3. Interactive Testing

1. Click "Send Request" above each HTTP request
2. View responses in the right panel
3. Use GitHub Copilot to generate additional test cases:

```
// Ask Copilot: "Generate more test cases for the restaurant MCP server"
// Copilot will suggest additional HTTP requests
```

### Method 3: Copilot-Assisted Test Generation

#### 1. Generate Test Data with Copilot

Ask GitHub Copilot to help create test data:

```
// In VS Code, select this comment and ask Copilot:
// "Generate 10 diverse restaurant test cases with different cuisines and locations"
```

#### 2. Copilot-Generated Test Script

Create a `test-restaurants.js` file and let Copilot help:

```javascript
// Ask Copilot to generate a comprehensive test script for the MCP restaurant server
// Include tests for: adding restaurants, listing, random selection, error handling

const baseUrl = 'http://localhost:7072/mcp';
const testRestaurants = [
  // Copilot will generate test data here
];

async function runTests() {
  // Copilot will generate test functions here
}
```

### Method 4: Debugging with VS Code

#### 1. Launch Configuration

Add to `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug MCP Restaurant Server",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/MCP.http/bin/Debug/net9.0/MCP.http.dll",
      "args": [],
      "cwd": "${workspaceFolder}/MCP.http",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

#### 2. Debugging Workflow

1. Set breakpoints in your MCP tools (`RestaurantTools.cs`)
2. Start debugging (`F5`)
3. Use REST Client or Copilot Chat to trigger requests
4. Step through code execution

### Method 5: Copilot Chat Integration Testing

#### 1. Natural Language Testing

Use Copilot Chat with natural language commands:

```
@workspace Test the restaurant server by:
1. Adding 3 different restaurants
2. Listing all restaurants
3. Picking a random one
4. Getting visit statistics
5. Testing error cases
```

#### 2. Copilot-Assisted Debugging

When issues occur, ask Copilot for help:

```
@workspace The restaurant server returned an error "Method not found". 
Can you help me debug this? Here's the request I sent: [paste JSON]
```

#### 3. Code Generation with Context

Ask Copilot to generate additional tools:

```
@workspace Based on the existing RestaurantTools pattern, 
can you create a new tool called "GetRestaurantsByFoodType" 
that filters restaurants by cuisine type?
```

### Best Practices for VS Code + Copilot Testing

1. **Use Descriptive Comments**: Help Copilot understand your testing intent
2. **Leverage Workspace Context**: Use `@workspace` to include project context
3. **Iterative Testing**: Build tests incrementally with Copilot assistance
4. **Document Results**: Use Copilot to help document test outcomes
5. **Error Analysis**: Ask Copilot to help analyze and fix errors

### Expected Benefits

- **Faster Testing**: Natural language to JSON-RPC conversion
- **Better Coverage**: Copilot suggests edge cases you might miss
- **Documentation**: Copilot helps document test scenarios
- **Debugging**: AI-assisted error analysis and resolution
- **Integration**: Seamless workflow within VS Code environment

This approach combines the power of GitHub Copilot's AI assistance with VS Code's integrated development environment to create a comprehensive testing workflow for your MCP restaurant server.


