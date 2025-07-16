# Model Context Protocol (MCP) Demo Projects

## 📋 Table of Contents

- [Overview](#overview)
- [What is MCP?](#what-is-mcp)
- [Key Benefits of MCP](#key-benefits-of-mcp)
- [Core Components](#core-components)
- [Transport Types](#transport-types)
  - [STDIO Transport](#stdio-transport)
  - [HTTP Transport](#http-transport)
  - [Server-Sent Events (SSE) Transport](#server-sent-events-sse-transport)
  - [WebSocket Transport](#websocket-transport)
  - [Transport Selection Guidelines](#transport-selection-guidelines)
  - [Security Considerations by Transport](#security-considerations-by-transport)
- [Testing and Development](#testing-and-development)
  - [MCP Inspector](#mcp-inspector)
  - [VS Code Integration](#vs-code-integration)
- [Best Practices](#best-practices)
  - [Tool Design](#tool-design)
  - [Service Architecture](#service-architecture)
  - [Security Considerations](#security-considerations)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Resources](#resources)

## Overview
This repository contains demonstration projects showcasing the Model Context Protocol (MCP) implementation using C#. The projects illustrate how to build, configure, and integrate MCP servers with AI assistants.

## What is MCP?
The Model Context Protocol (MCP) is a standardized protocol that enables AI assistants to securely connect to external data sources and tools. It provides a bridge between AI models and real-world applications, allowing for enhanced functionality and context-aware interactions.

## Key Benefits of MCP
- **Standardized Communication**: Consistent protocol for AI-tool integration
- **Security**: Controlled access to external resources
- **Extensibility**: Easy to add new tools and capabilities
- **Interoperability**: Works across different AI platforms and assistants

## Core Components

### MCP Server Architecture
MCP servers typically consist of:
- **Tools**: Exposed functions that AI assistants can call
- **Resources**: Data sources that can be read by AI assistants
- **Transport Layer**: Communication mechanism (STDIO, TCP, etc.)
- **Protocol Handler**: Manages MCP message formatting and routing

## Transport Types

MCP supports multiple transport mechanisms to enable communication between AI assistants and MCP servers. Each transport type has specific use cases and configuration requirements.

### STDIO Transport
**Standard Input/Output** - The most common transport for local development and simple deployments.

**Characteristics:**
- Process-based communication through stdin/stdout
- Ideal for local development and testing
- No network configuration required
- Automatic process lifecycle management

**Use Cases:**
- Local development and debugging
- Simple command-line tools
- Single-user applications
- CI/CD pipeline integration

**Configuration Example:**
```json
{
    "type": "stdio",
    "command": "dotnet",
    "args": ["run", "--project", "path/to/project.csproj"]
}
```

**C# Implementation:**
```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport();
```

### HTTP Transport
**HTTP/HTTPS** - Network-based communication for web-accessible MCP servers.

**Characteristics:**
- RESTful API endpoints
- Supports CORS for web applications
- Can be deployed behind load balancers
- Stateless communication model

**Use Cases:**
- Multi-user applications
- Cloud deployments
- Web-based integrations
- Microservices architectures

**Configuration Example:**
```json
{
    "type": "http",
    "url": "http://localhost:5227/mcp",
    "headers": {
        "Authorization": "Bearer your-token"
    }
}
```

**C# Implementation:**
```csharp
builder.Services
    .AddMcpServer()
    .WithHttpServerTransport();

// Configure HTTP endpoint
app.MapMcpServer("/mcp");
```

### Server-Sent Events (SSE) Transport
**Event-driven communication** - Real-time streaming for live updates and notifications.

**Characteristics:**
- Unidirectional server-to-client streaming
- Built on HTTP with persistent connections
- Automatic reconnection support
- Low latency for real-time updates

**Use Cases:**
- Live data feeds
- Real-time monitoring
- Progress notifications
- Event-driven architectures

**Configuration Example:**
```json
{
    "type": "sse",
    "url": "http://localhost:5227/mcp/events",
    "reconnectInterval": 5000
}
```

**C# Implementation:**
```csharp
builder.Services
    .AddMcpServer()
    .WithSseServerTransport();

// Configure SSE endpoint
app.MapMcpServerSse("/mcp/events");
```

### WebSocket Transport
**Bidirectional real-time communication** - Full-duplex communication for interactive applications.

**Characteristics:**
- Persistent bidirectional connections
- Low overhead after initial handshake
- Real-time interactive capabilities
- Built-in heartbeat and reconnection

**Use Cases:**
- Interactive applications
- Real-time collaboration
- Gaming or simulation tools
- High-frequency data exchange

**Configuration Example:**
```json
{
    "type": "websocket",
    "url": "ws://localhost:5227/mcp/ws",
    "protocols": ["mcp-v1"]
}
```

**C# Implementation:**
```csharp
builder.Services
    .AddMcpServer()
    .WithWebSocketServerTransport();

// Configure WebSocket endpoint
app.MapMcpServerWebSocket("/mcp/ws");
```

### Transport Selection Guidelines

**Choose STDIO when:**
- Developing locally
- Building command-line tools
- Single-user applications
- Simple deployment scenarios

**Choose HTTP when:**
- Building web applications
- Need stateless communication
- Deploying to cloud platforms
- Integrating with existing REST APIs

**Choose SSE when:**
- Need real-time data streaming
- One-way server updates
- Building dashboards or monitoring tools
- Live notification systems

**Choose WebSocket when:**
- Need bidirectional real-time communication
- Building interactive applications
- High-frequency data exchange
- Real-time collaboration features

### Security Considerations by Transport

**STDIO:**
- Process-level isolation
- No network exposure
- File system permissions apply

**HTTP:**
- HTTPS for encryption
- Authentication headers
- CORS configuration
- Rate limiting

**SSE:**
- Same security as HTTP
- Connection persistence considerations
- Stream authorization

**WebSocket:**
- WSS for secure connections
- Origin validation
- Subprotocol negotiation
- Connection limits

## Testing and Development

### MCP Inspector
Use the MCP Inspector for local testing and development:

```bash
# Install MCP Inspector
npm install -g @modelcontextprotocol/inspector
```

### VS Code Integration
Configure MCP servers in VS Code by creating a `.vscode/mcp.json` file:

```json
{
    "inputs": [],
    "servers": {
        "demo-mcp-stdio": {
            "type": "stdio",
            "command": "dotnet",
            "args": [
                "run",
                "--project",
                "C:\\Code\\GitHub\\DemoMCP\\MCPDemo\\MCP.stdio\\MCP.stdio.csproj"
            ],
            "env": {}
        },
        "demo-mcp-http": {
            "type": "http",
            "url": "http://localhost:5227/mcp"
        }
    }
}
```

## Best Practices

### Tool Design
- Use descriptive `[Description]` attributes for tools and parameters
- Return JSON-serialized responses for consistency
- Handle error cases gracefully
- Keep tools focused and single-purpose

### Service Architecture
- Use dependency injection for service registration
- Separate business logic from MCP tool implementations
- Implement proper error handling and logging
- Use async/await patterns consistently

### Security Considerations
- Validate all input parameters
- Implement appropriate access controls
- Sanitize data before processing
- Use secure communication channels

## Project Structure
This repository contains multiple MCP demonstration projects:
- **MCP.stdio**: Console-based MCP server with STDIO transport
- **MCP.http**: HTTP-based MCP server with RESTful endpoints

Each project demonstrates different aspects of MCP server implementation and can serve as reference implementations for your own MCP servers.

## Getting Started
1. Clone this repository
2. Navigate to the desired project folder
3. Install dependencies with `dotnet restore`
4. Build with `dotnet build`
5. Run with `dotnet run`
6. Test using MCP Inspector or configure in VS Code

## Resources
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [MCP C# Library Documentation](https://www.nuget.org/packages/ModelContextProtocol/)
- [VS Code MCP Integration Guide](https://code.visualstudio.com/docs/copilot/mcp)
