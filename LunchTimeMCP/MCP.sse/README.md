# LunchTime MCP Streaming Server

An HTTP-based Model Context Protocol (MCP) server with chunked JSON streaming for managing lunch restaurant choices with real-time progressive data loading.

## Features

- **Chunked HTTP Streaming**: Uses HTTP Transfer-Encoding: chunked for progressive data loading
- **MCP Protocol Compliance**: Full JSON-RPC 2.0 Model Context Protocol implementation
- **Restaurant Management**: Add, retrieve, and analyze restaurants
- **Progressive Loading**: Watch data load in real-time with visual feedback
- **Visit Tracking**: Track and analyze restaurant visit statistics
- **Unified API**: Single endpoint handles both regular and streaming requests

## HTTP Chunked Streaming Capabilities

### Supported Streaming Features

- **chunked-transfer-encoding**: Progressive response delivery
- **progressive-loading**: Real-time data streaming
- **real-time-streaming**: Live updates during processing

### Streaming Tools

1. **get_restaurants_stream**: Progressive restaurant loading with real-time feedback
2. **analyze_restaurants_stream**: Live analysis of restaurant data with step-by-step updates
3. **search_restaurants_stream**: Real-time search results as they're found

## API Endpoints

### MCP Protocol (JSON-RPC 2.0)

- `POST /mcp` - Main MCP protocol endpoint (supports both regular and streaming requests)
- `GET /mcp/initialize` - Server initialization info
- `GET /mcp/tools` - List available tools
- `GET /mcp/capabilities` - Streaming capabilities and status

### Health & Info

- `GET /health` - Server health check
- `GET /` - Server information and documentation

## Usage

### Starting the Server
cd MCP.sse
dotnet run
The server will start on `http://localhost:5227` by default.

### MCP Usage (JSON-RPC 2.0)

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
          "protocols": ["chunked-json"]
        }
      }
    }
  }'
#### List Tools
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list"
  }'
#### Execute Regular Tool
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "get_restaurants"
    }
  }'
#### Execute Streaming Tool (Method 1: Streaming Parameter)
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
      "name": "get_restaurants_stream",
      "streaming": true
    }
  }'
#### Execute Streaming Tool (Method 2: Streaming Header)
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -H "Accept-Streaming: chunked-json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 5,
    "method": "tools/call",
    "params": {
      "name": "analyze_restaurants_stream",
      "arguments": {"type": "detailed"}
    }
  }'
#### Execute Streaming Tool (Method 3: Auto-detection)
curl -X POST http://localhost:5227/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 6,
    "method": "tools/call",
    "params": {
      "name": "search_restaurants_stream",
      "arguments": {"query": "italian"}
    }
  }'
### JavaScript Fetch API Usage
async function streamMcpTool(toolName, arguments = {}) {
    const response = await fetch('http://localhost:5227/mcp', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept-Streaming': 'chunked-json'
        },
        body: JSON.stringify({
            jsonrpc: '2.0',
            id: Date.now(),
            method: 'tools/call',
            params: {
                name: toolName,
                arguments: arguments,
                streaming: true
            }
        })
    });

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    
    let buffer = '';
    while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';
        
        for (const line of lines) {
            if (line.trim()) {
                const chunk = JSON.parse(line);
                console.log('Received chunk:', chunk);
            }
        }
    }
}

// Usage examples
streamMcpTool('get_restaurants_stream');
streamMcpTool('analyze_restaurants_stream', { type: 'detailed' });
streamMcpTool('search_restaurants_stream', { query: 'pizza' });
## Available Tools

### Standard Tools (Non-streaming)

1. **get_restaurants**: Get all restaurants
2. **add_restaurant**: Add a new restaurant
3. **pick_random_restaurant**: Random restaurant selection
4. **get_visit_stats**: Restaurant visit statistics

### Streaming Tools (Chunked JSON)

1. **get_restaurants_stream**: Progressive restaurant loading
2. **analyze_restaurants_stream**: Real-time data analysis
3. **search_restaurants_stream**: Live search results

## Streaming Activation

The server automatically enables streaming when:

1. **Streaming Parameter**: `"streaming": true` in tool call params
2. **Streaming Tools**: Using tools ending with `_stream`
3. **Streaming Headers**: `Accept-Streaming` or `X-MCP-Streaming` headers
4. **Auto-detection**: Server recognizes streaming tool names

## Example Chunked JSON Response

### Restaurant Stream Chunk
{
  "jsonrpc": "2.0",
  "id": "request-123",
  "result": {
    "content": [{
      "type": "text",
      "text": "**1. Guelaguetza**\n?? Location: 3014 W Olympic Blvd\n?? Food Type: Oaxacan Mexican\n?? Added: 2024-01-15"
    }],
    "metadata": {
      "timestamp": "2024-01-15T10:30:00Z",
      "streaming": {
        "chunkNumber": 2,
        "totalChunks": 10,
        "isLast": false
      }
    }
  },
  "streaming": {
    "type": "partial",
    "sequence": 2,
    "total": 10,
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
### Final Chunk
{
  "jsonrpc": "2.0",
  "id": "request-123",
  "result": {
    "content": [{
      "type": "text",
      "text": "\n? **Streaming Complete**\n?? Total restaurants loaded: 10"
    }],
    "metadata": {
      "timestamp": "2024-01-15T10:30:00Z",
      "streaming": {
        "chunkNumber": 10,
        "totalChunks": 10,
        "isLast": true
      }
    }
  },
  "streaming": {
    "type": "complete",
    "sequence": 10,
    "total": 10,
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
## Configuration

The server uses the default ASP.NET Core configuration. JSON serialization is configured to:
- Use camelCase property naming
- Ignore null values for better client compatibility
- Use indented formatting for readability

## Data Storage

Restaurant data is stored in JSON format in the user's application data directory:
- **Windows**: `%APPDATA%\LunchTimeMCP_Streaming\restaurants.json`
- **macOS**: `~/.config/LunchTimeMCP_Streaming/restaurants.json`
- **Linux**: `~/.config/LunchTimeMCP_Streaming/restaurants.json`

## Testing

Use the included `MCP.sse.http` file with Visual Studio or VS Code REST Client extension to test all endpoints.

For streaming testing, use:
- Browser Fetch API with ReadableStream
- `curl` with streaming support
- HTTP clients with chunked transfer encoding support
- MCP-compatible clients with streaming support

## Architecture

- **Single Controller**: McpServerController handles both regular and streaming requests
- **Services**: Business logic and streaming data generation
- **Models**: Data structures for MCP protocol and streaming responses
- **Progressive Delivery**: Line-by-line JSON chunk streaming
- **Unified API**: Single `/mcp` endpoint for all operations

## Protocol Compliance

This server implements:
- MCP Protocol version 2024-11-05
- JSON-RPC 2.0 specification
- HTTP/1.1 Chunked Transfer Encoding
- CORS support for web clients
- Automatic streaming detection and activation