# LunchTime MCP SSE Server

A Server-Sent Events (SSE) based Model Context Protocol (MCP) server for managing lunch restaurant choices with real-time streaming capabilities.

## Features

- **Real-time Streaming**: Server-Sent Events for live data updates
- **MCP Protocol Compliance**: Full JSON-RPC 2.0 Model Context Protocol implementation
- **Restaurant Management**: Add, retrieve, and analyze restaurants
- **Progressive Loading**: Watch data load in real-time with visual feedback
- **Auto-reconnection**: SSE connections automatically handle reconnection
- **Visit Tracking**: Track and analyze restaurant visit statistics

## Recent Fixes

### ZodError for nextCursor (Fixed)

**Issue**: Clients using Zod schema validation were receiving errors when `nextCursor` was null:ZodError: Expected string, received null at path ["nextCursor"]
**Solution**: 
- Added `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to `nextCursor` properties
- Configured global JSON serialization to ignore null values
- Now null `nextCursor` values are omitted from the JSON response instead of being serialized as `null`

**Before Fix**:{
  "prompts": [...],
  "nextCursor": null
}
**After Fix**:{
  "prompts": [...]
}
## Server-Sent Events Capabilities

### Supported Event Types

- **tool-result**: Real-time tool execution results
- **restaurant-update**: Live updates when restaurants are added or selected
- **error**: Error notifications
- **heartbeat**: Connection keep-alive events
- **connected**: Initial connection confirmation

### Streaming Tools

1. **get_restaurants_stream**: Progressive restaurant loading with real-time feedback
2. **analyze_restaurants_stream**: Live analysis of restaurant data with step-by-step updates
3. **search_restaurants_stream**: Real-time search results as they're found

## API Endpoints

### MCP Protocol (JSON-RPC 2.0)

- `POST /mcp` - Main MCP protocol endpoint
- `GET /mcp/initialize` - Server initialization info
- `GET /mcp/tools` - List available tools

### Server-Sent Events

- `GET /sse/stream` - Main SSE connection for real-time updates
- `GET /sse/tools/{toolName}` - Stream specific tool execution
- `POST /sse/tools/{toolName}` - Stream tool with request body
- `GET /sse/capabilities` - SSE capabilities and status

### Health & Info

- `GET /health` - Server health check
- `GET /` - Server information and documentation

## Usage

### Starting the Server
cd MCP.sse
dotnet run
The server will start on `http://localhost:5227` by default.

### Traditional MCP Usage (JSON-RPC 2.0)

#### Initialize Connection
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {
        "streaming": {
          "supported": true,
          "protocol": "sse"
        }
      }
    }
  }'
#### List Prompts (Fixed - No more ZodError!)
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 6,
    "method": "prompts/list"
  }'
#### Execute Tool
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "get_restaurants"
    }
  }'
### Server-Sent Events Usage

#### Connect to Real-time Stream
const eventSource = new EventSource('http://localhost:5227/sse/stream');

eventSource.onmessage = function(event) {
    console.log('Received:', JSON.parse(event.data));
};

eventSource.addEventListener('restaurant-update', function(event) {
    console.log('Restaurant update:', JSON.parse(event.data));
});

eventSource.addEventListener('tool-result', function(event) {
    console.log('Tool result:', JSON.parse(event.data));
});
#### Stream Tool Execution
const toolStream = new EventSource('http://localhost:5227/sse/tools/get_restaurants_stream');

toolStream.onmessage = function(event) {
    const data = JSON.parse(event.data);
    console.log(`Progress: ${data.progress}%`, data);
};

toolStream.addEventListener('restaurant-data', function(event) {
    const restaurant = JSON.parse(event.data);
    console.log('New restaurant loaded:', restaurant);
});
#### Stream with Arguments
const searchArgs = encodeURIComponent(JSON.stringify({query: "italian"}));
const searchStream = new EventSource(`http://localhost:5227/sse/tools/search_restaurants_stream?arguments=${searchArgs}`);

searchStream.addEventListener('search-result', function(event) {
    const match = JSON.parse(event.data);
    console.log('Found match:', match);
});
## Available Tools

### Standard Tools (Non-streaming)

1. **get_restaurants**: Get all restaurants
2. **add_restaurant**: Add a new restaurant
3. **pick_random_restaurant**: Random restaurant selection
4. **get_visit_stats**: Restaurant visit statistics

### Streaming Tools (SSE)

1. **get_restaurants_stream**: Progressive restaurant loading
2. **analyze_restaurants_stream**: Real-time data analysis
3. **search_restaurants_stream**: Live search results

## Example SSE Events

### Restaurant Stream Event
{
  "event": "restaurant-data",
  "data": {
    "index": 1,
    "total": 10,
    "restaurant": {
      "name": "Guelaguetza",
      "location": "3014 W Olympic Blvd",
      "foodType": "Oaxacan Mexican"
    },
    "progress": 10.0
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
### Analysis Progress Event
{
  "event": "analysis-progress",
  "data": {
    "step": "?? Analyzing restaurant data...",
    "progress": 25.0,
    "isComplete": false
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
### Restaurant Update Event
{
  "event": "restaurant-update",
  "data": {
    "action": "added",
    "restaurant": {
      "name": "New Restaurant",
      "location": "123 Main St",
      "foodType": "Italian"
    }
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
## Configuration

The server uses the default ASP.NET Core configuration. JSON serialization is configured to:
- Use camelCase property naming
- Ignore null values (preventing ZodError issues)
- Use indented formatting for readability

## Data Storage

Restaurant data is stored in JSON format in the user's application data directory:
- Windows: `%APPDATA%\LunchTimeMCP_SSE\restaurants.json`
- macOS: `~/.config/LunchTimeMCP_SSE/restaurants.json`
- Linux: `~/.config/LunchTimeMCP_SSE/restaurants.json`

## Testing

Use the included `MCP.sse.http` file with Visual Studio or VS Code REST Client extension to test all endpoints.

For SSE testing, use:
- Browser Developer Tools
- `curl` with streaming support
- SSE client libraries
- EventSource API in browsers
- Interactive `demo.html` file

## Troubleshooting

### ZodError: Expected string, received null

This error has been fixed in the latest version. If you're still experiencing issues:

1. Ensure you're using the latest server version
2. Check that your client properly handles missing `nextCursor` fields
3. Verify JSON response doesn't contain `"nextCursor": null`

### SSE Connection Issues

- Check CORS configuration if connecting from browser
- Verify server is running on expected port
- Check firewall settings for SSE connections

## Architecture

- **Controllers**: Handle HTTP requests and SSE connections
- **Services**: Business logic and streaming management
- **Models**: Data structures for MCP protocol and SSE events
- **Real-time Broadcasting**: Live updates to all connected SSE clients

## Protocol Compliance

This server implements:
- MCP Protocol version 2024-11-05
- JSON-RPC 2.0 specification
- Server-Sent Events (W3C EventSource specification)
- CORS support for web clients
- Proper null value handling to prevent Zod validation errors