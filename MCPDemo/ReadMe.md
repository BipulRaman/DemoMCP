# Model Context Protocol (MCP) Demo Projects

## 📋 Table of Contents

- [Overview](#overview)
- [What is MCP?](#what-is-mcp)
- [Key Benefits of MCP](#key-benefits-of-mcp)
- [Projects Structure](#projects-structure)
- [Core Components](#core-components)
- [Transport Types](#transport-types)
- [Getting Started](#getting-started)
- [Testing and Development](#testing-and-development)
- [Integration Examples](#integration-examples)
- [Best Practices](#best-practices)

## Overview

This repository contains **production-ready** demonstration projects showcasing the Model Context Protocol (MCP) implementation using C# and .NET 9.0. The projects illustrate different transport mechanisms and deployment scenarios, providing comprehensive examples for building, configuring, and integrating MCP servers with AI assistants like GitHub Copilot and Claude Desktop.

Each project demonstrates the same core MCP functionality (code snippet management) through different transport layers and deployment targets, allowing you to choose the implementation that best fits your architecture requirements.

## What is MCP?

The Model Context Protocol (MCP) is a standardized protocol that enables AI assistants to securely connect to external data sources and tools. It provides a bridge between AI models and real-world applications, allowing for enhanced functionality and context-aware interactions.

## Key Benefits of MCP

- **Standardized Communication**: Consistent protocol for AI-tool integration
- **Security**: Controlled access to external resources
- **Extensibility**: Easy to add new tools and capabilities
- **Interoperability**: Works across different AI platforms and assistants
- **Flexible Deployment**: Multiple transport options for different scenarios

## Projects Structure

This repository contains four distinct MCP server implementations:

### 🖥️ MCP.STDIO
**Standard Input/Output Server** - Process-based communication for local development and command-line integration.
- **Target Framework**: .NET 9.0 Console Application
- **Transport**: Standard I/O (stdio)
- **Use Case**: Local development, CLI tools, single-user applications
- **Location**: `./MCP.STDIO/`

### 🌐 MCP.SSE  
**Web API Server** - HTTP-based server with Server-Sent Events for real-time communication.
- **Target Framework**: .NET 9.0 ASP.NET Core Web API
- **Transport**: HTTP with Server-Sent Events (SSE)
- **Use Case**: Web applications, real-time streaming, containerized deployments
- **Location**: `./MCP.SSE/`

### ☁️ MCP.Remote
**Azure Functions Server** - Serverless implementation for cloud deployment.
- **Target Framework**: .NET 9.0 Azure Functions Worker
- **Transport**: HTTP with Server-Sent Events (SSE)
- **Use Case**: Serverless deployments, Azure cloud integration, scalable solutions
- **Location**: `./MCP.Remote/`

### 📚 MCP.Common
**Shared Library** - Common services, models, and utilities shared across all implementations.
- **Target Framework**: .NET 9.0 Class Library
- **Components**: Services, Tools, Models, Extensions
- **Location**: `./MCP.Common/`

## Core Components

### MCP Server Architecture
MCP servers typically consist of:
- **Tools**: Exposed functions that AI assistants can call
- **Resources**: Data sources that can be read by AI assistants
- **Transport Layer**: Communication mechanism (STDIO, TCP, etc.)
- **Protocol Handler**: Manages MCP message formatting and routing

## Transport Types

This repository demonstrates three primary MCP transport implementations, each optimized for different deployment scenarios:

### Standard Input/Output (STDIO)
Process-based communication ideal for local development and command-line integration.

### Server-Sent Events (SSE) 
Event-driven HTTP communication for cloud deployment and real-time streaming capabilities.

### Azure Functions
Serverless implementation combining SSE transport with cloud-native scalability.

### Transport Selection Guidelines

**Choose STDIO when:**
- Developing locally
- Building command-line tools
- Single-user applications
- Simple deployment scenarios
- Direct process integration needed

**Choose SSE (Web API) when:**
- Building web-hosted applications
- Need real-time streaming capabilities
- Containerized deployments (Docker, Kubernetes)
- Traditional web server infrastructure
- Load balancing requirements

**Choose Azure Functions when:**
- Building cloud-hosted applications
- Serverless/consumption-based billing preferred
- Automatic scaling requirements
- Azure ecosystem integration
- Event-driven architectures

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code
- Azure Functions Core Tools (for MCP.Remote)
- Node.js (for MCP Inspector testing)

### Quick Start

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd MCPDemo
   ```

2. **Build the solution:**
   ```bash
   dotnet build MCPDemo.slnx
   ```

3. **Run a specific project:**
   ```bash
   # STDIO Server
   dotnet run --project MCP.STDIO

   # Web API Server
   dotnet run --project MCP.SSE

   # Azure Functions (local)
   cd MCP.Remote
   func start
   ```

### Project-Specific Setup

Each project contains its own detailed README with specific setup instructions:
- [MCP.STDIO Setup Guide](./MCP.STDIO/README.md)
- [MCP.SSE Setup Guide](./MCP.SSE/README.md)  
- [MCP.Remote Setup Guide](./MCP.Remote/README.md)

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
All projects support seamless integration with VS Code and GitHub Copilot. Create a `.vscode/mcp.json` file in your workspace:

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
                "./MCP.STDIO/MCP.STDIO.csproj"
            ]
        },
        "demo-mcp-sse": {
            "type": "sse",
            "url": "http://localhost:5296/sse"
        },
        "demo-mcp-remote": {
            "type": "sse",
            "url": "http://localhost:7234/runtime/webhooks/mcp/sse"
        }
    }
}
```

**Note:** Ensure the project paths and URLs match your actual configuration.

## Integration Examples

### Claude Desktop Integration

Add to your Claude Desktop configuration file (`%APPDATA%\Claude\claude_desktop_config.json` on Windows):

```json
{
  "mcpServers": {
    "demo-snippet-manager": {
      "command": "dotnet",
      "args": [
        "run", 
        "--project", 
        "C:\\Path\\To\\MCPDemo\\MCP.STDIO\\MCP.STDIO.csproj"
      ]
    }
  }
}
```

### GitHub Copilot Integration

The projects work seamlessly with GitHub Copilot when configured in VS Code. The MCP servers provide additional context and tools that enhance Copilot's capabilities for code snippet management.

### Available Tools

All implementations provide the following MCP tools:

- **`hello`**: Simple greeting tool for testing connectivity
- **`get_snippets`**: Retrieve saved code snippets by name
- **`save_snippet`**: Save new code snippets with names and content

## Best Practices

### Tool Design
- Use descriptive `[Description]` attributes for tools and parameters
- Return JSON-serialized responses for consistency
- Handle error cases gracefully and provide meaningful error messages
- Keep tools focused and single-purpose for better composability
- Implement proper input validation and sanitization

### Service Architecture
- Use dependency injection for service registration and management
- Separate business logic from MCP tool implementations
- Implement proper error handling and structured logging
- Use async/await patterns consistently for better performance
- Follow SOLID principles for maintainable code

### Security & Deployment
- Validate all input parameters to prevent injection attacks
- Implement appropriate access controls and authentication
- Sanitize data before processing and storage
- Use secure communication channels (HTTPS for SSE transport)
- Consider rate limiting and request throttling for production deployments
- Implement proper secrets management for cloud deployments

### Performance Optimization
- Use efficient data structures and algorithms
- Implement caching strategies where appropriate
- Consider connection pooling for database operations
- Monitor memory usage and implement proper disposal patterns
- Use streaming for large data operations

### Testing Strategy
- Write comprehensive unit tests for business logic
- Implement integration tests for MCP protocol compliance
- Use the MCP Inspector for development and debugging
- Test all transport mechanisms in realistic scenarios
- Validate error handling and edge cases

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For questions, issues, or contributions:
- Create an issue in the GitHub repository
- Review the project-specific README files for detailed documentation
- Check the MCP official documentation for protocol specifications
