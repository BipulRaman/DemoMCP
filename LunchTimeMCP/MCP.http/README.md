# LunchTime MCP Streaming HTTP Server

A streamable Model Context Protocol (MCP) server implemented as an HTTP API for managing lunch restaurant choices. This server provides both traditional MCP functionality and modern streaming capabilities for real-time data delivery.

## Features

- **Restaurant Management**: Add and retrieve restaurants
- **Random Selection**: Pick a random restaurant for lunch
- **Visit Tracking**: Track and view statistics about restaurant visits
- **HTTP API**: RESTful endpoints for all operations
- **MCP Protocol**: Full MCP JSON-RPC 2.0 compliance
- **?? Streaming Support**: Real-time streaming via Server-Sent Events (SSE) and chunked responses
- **Progressive Loading**: Watch data load in real-time with visual progress indicators
- **Multi-Protocol Streaming**: Support for both SSE and chunked JSON streaming

## Streaming Capabilities

### Supported Streaming Protocols

1. **Server-Sent Events (SSE)**: Real-time event streaming with automatic reconnection
2. **Chunked JSON**: Progressive JSON array streaming for API clients

### Streaming Tools

- `get_restaurants_stream`: Stream restaurant data progressively with loading indicators
- `analyze_restaurants_stream`: Stream real-time analysis of restaurant data and insights
- `search_restaurants_stream`: Stream search results as they're found

## API Endpoints

### Traditional MCP Endpoints

- `POST /mcp` - JSON-RPC 2.0 MCP protocol endpoint
- `GET /mcp/initialize` - Get server initialization info
- `GET /mcp/tools` - List all available tools (including streaming tools)

### Streaming Endpoints

- `POST /mcp/stream/tools/call/sse` - Server-Sent Events streaming for tool calls
- `POST /mcp/stream/tools/call/chunked` - Chunked JSON streaming for tool calls
- `GET /mcp/stream/restaurants/sse` - Stream restaurant data via SSE
- `GET /mcp/stream/capabilities` - Get streaming capabilities and supported tools
- `GET /mcp/stream/health` - Streaming endpoints health check

### Health & Info

- `GET /health` - Server health check
- `GET /` - Server information and documentation

## Usage

### Running the Server
dotnet run
The server will start on `https://localhost:7073` (HTTPS) and `http://localhost:5073` (HTTP).

### Traditional MCP Usage

#### Initialize MCP Connectioncurl -X POST https://localhost:7073/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {"name": "test-client", "version": "1.0.0"}
    }
  }'
#### List Available Toolscurl -X POST https://localhost:7073/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list"
  }'
#### Call a Toolcurl -X POST https://localhost:7073/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "add_restaurant",
      "arguments": {
        "name": "Pizza Palace",
        "location": "123 Main St",
        "foodType": "Italian"
      }
    }
  }'
### ?? Streaming Usage

#### Server-Sent Events (SSE) - Recommended for Web Clients

Stream restaurant data in real-time:curl -N https://localhost:7073/mcp/stream/restaurants/sse
Stream tool call results:curl -X POST https://localhost:7073/mcp/stream/tools/call/sse \
  -H "Content-Type: application/json" \
  -d '{
    "toolName": "get_restaurants_stream",
    "arguments": {},
    "requestId": "unique-request-id",
    "streaming": {
      "protocol": "sse",
      "enableMetadata": true
    }
  }'
#### Chunked JSON Streaming - For API Clients
curl -X POST https://localhost:7073/mcp/stream/tools/call/chunked \
  -H "Content-Type: application/json" \
  -d '{
    "toolName": "analyze_restaurants_stream",
    "arguments": {"type": "cuisine"},
    "requestId": "analysis-123"
  }'
#### JavaScript/Browser Example
// Server-Sent Events example
const eventSource = new EventSource('/mcp/stream/restaurants/sse');

eventSource.onmessage = function(event) {
    const data = JSON.parse(event.data);
    console.log('Received:', data);
};

eventSource.addEventListener('header', function(event) {
    const headerData = JSON.parse(event.data);
    console.log('Stream started:', headerData);
});

eventSource.addEventListener('restaurant', function(event) {
    const restaurant = JSON.parse(event.data);
    console.log('New restaurant:', restaurant);
});

eventSource.addEventListener('complete', function(event) {
    console.log('Stream completed');
    eventSource.close();
});
### Advanced Streaming Examples

#### Stream Restaurant Analysiscurl -X POST https://localhost:7073/mcp/stream/tools/call/sse \
  -H "Content-Type: application/json" \
  -d '{
    "toolName": "analyze_restaurants_stream",
    "arguments": {
      "type": "general"
    },
    "requestId": "analysis-001"
  }'
#### Stream Restaurant Searchcurl -X POST https://localhost:7073/mcp/stream/tools/call/sse \
  -H "Content-Type: application/json" \
  -d '{
    "toolName": "search_restaurants_stream",
    "arguments": {
      "query": "mexican"
    },
    "requestId": "search-001"
  }'
## MCP Protocol Compliance

This server implements the full MCP (Model Context Protocol) specification with additional streaming extensions:

- **Protocol Version**: 2024-11-05
- **JSON-RPC 2.0**: Full compliance
- **Capabilities**: Tools, Prompts, Streaming
- **Streaming Extensions**: Custom streaming protocol support

### Streaming Capabilities Response
{
  "streaming": {
    "supported": true,
    "protocols": ["sse", "chunked-json"],
    "toolStreaming": true,
    "promptStreaming": true
  }
}
## Data Persistence

Restaurant data is stored in JSON format in the user's application data directory:
- Windows: `%APPDATA%\LunchTimeMCP\restaurants.json`
- macOS/Linux: `~/.config/LunchTimeMCP/restaurants.json`

## Performance & Streaming

- **Real-time Updates**: See data as it loads with progress indicators
- **Progressive Enhancement**: Works with and without streaming support
- **Cancellation Support**: Streaming operations can be cancelled mid-stream
- **Memory Efficient**: Streaming reduces memory usage for large datasets
- **Network Optimized**: Chunked responses optimize network utilization

## Initial Data

The server comes pre-loaded with trendy West Hollywood restaurants if no data exists:

1. **Guelaguetza** - Oaxacan Mexican (3014 W Olympic Blvd)
2. **Republique** - French Bistro (624 S La Brea Ave)
3. **Night + Market WeHo** - Thai Street Food (9041 Sunset Blvd)
4. **Gracias Madre** - Vegan Mexican (8905 Melrose Ave)
5. **The Ivy** - Californian (113 N Robertson Blvd)
6. **Catch LA** - Seafood (8715 Melrose Ave)
7. **Cecconi's** - Italian (8764 Melrose Ave)
8. **Earls Kitchen + Bar** - Global Comfort Food (8730 W Sunset Blvd)
9. **Pump Restaurant** - Mediterranean (8948 Santa Monica Blvd)
10. **Craig's** - American Contemporary (8826 Melrose Ave)

## Error Handling

The streaming server includes robust error handling:
- Connection failures are gracefully handled
- Partial streams can be resumed
- Error events are sent through the streaming protocol
- Fallback to traditional HTTP responses when streaming fails

## Development

Built with:
- **.NET 9.0**: Latest .NET framework
- **ASP.NET Core**: High-performance web framework
- **JSON-RPC 2.0**: Industry standard protocol
- **Server-Sent Events**: W3C standard streaming
- **Model Context Protocol**: AI tool integration standard