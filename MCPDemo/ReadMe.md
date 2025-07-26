# Model Context Protocol (MCP) Demo Projects

## 📋 Table of Contents

- [Overview](#overview)
- [What is MCP?](#what-is-mcp)
- [Key Benefits of MCP](#key-benefits-of-mcp)
- [Core Components](#core-components)
- [Transport Types](#transport-types)
- [Testing and Development](#testing-and-development)
- [Best Practices](#best-practices)

## Overview

This repository contains **production-ready** demonstration projects showcasing the Model Context Protocol (MCP) implementation using C# and .NET 9.0. The projects illustrate different transport mechanisms and provide comprehensive examples for building, configuring, and integrating MCP servers with AI assistants like GitHub Copilot and Claude Desktop.

Each project demonstrates the same core MCP functionality through different transport layers, allowing you to choose the implementation that best fits your deployment scenario and architecture requirements.

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

This repository demonstrates two primary MCP transport implementations, each optimized for different deployment scenarios:

**Standard Input/Output (STDIO)** - Process-based communication for local development and command-line integration.

**Server-Sent Events (SSE)** - Event-driven communication for cloud deployment and real-time streaming capabilities.

### Transport Selection Guidelines

**Choose STDIO when:**
- Developing locally
- Building command-line tools
- Single-user applications
- Simple deployment scenarios

**Choose SSE when:**
- Building cloud-hosted applications
- Need real-time streaming capabilities
- Deploying to serverless platforms (Azure Functions)
- Integrating with cloud infrastructure

## Testing and Development

### MCP Inspector
The MCP Inspector is a powerful tool for testing and debugging MCP servers during development:

```bash
# Install MCP Inspector globally
npm install -g @modelcontextprotocol/inspector

# Launch inspector
npx @modelcontextprotocol/inspector
```

### VS Code Integration
Both projects support seamless integration with VS Code and GitHub Copilot. Create a `.vscode/mcp.json` file in your workspace:

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
                "PATH_TO_YOUR_PROJECT\\MCP.stdio\\MCP.stdio.csproj"
            ]
        },
        "demo-mcp-remote": {
            "type": "sse",
            "url": "http://localhost:7234/runtime/webhooks/mcp/sse"
        }
    }
}
```

**Note:** Update the project path to match your actual file system location.

## Best Practices

### Tool Design
- Use descriptive `[Description]` attributes for tools and parameters
- Return JSON-serialized responses for consistency
- Handle error cases gracefully and provide meaningful error messages
- Keep tools focused and single-purpose for better composability

### Service Architecture
- Use dependency injection for service registration and management
- Separate business logic from MCP tool implementations
- Implement proper error handling and structured logging
- Use async/await patterns consistently for better performance

### Security & Deployment
- Validate all input parameters to prevent injection attacks
- Implement appropriate access controls and authentication
- Sanitize data before processing and storage
- Use secure communication channels (HTTPS for SSE transport)
- Consider rate limiting and request throttling for production deployments
