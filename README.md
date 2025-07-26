# DemoMCP - Model Context Protocol Examples

Welcome to the DemoMCP repository! This collection demonstrates how to build and implement Model Context Protocol (MCP) servers using C# and .NET 9.

## 🚀 What's Inside

This repository contains examples and demonstrations of MCP server implementations, showcasing different transport mechanisms and architectural patterns for building robust, production-ready MCP servers.

### Current Projects

- **[MCP.stdio](MCPDemo/MCP.stdio/)**: A production-ready Standard I/O based MCP server implementation with restaurant management capabilities, built using Microsoft's official MCP SDK
- **[MCP.Remote](MCPDemo/MCP.Remote/)**: A cloud-native Azure Functions based MCP server with Server-Sent Events (SSE) transport and snippet management capabilities

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- VS Code (recommended)
- For MCP.stdio: Claude Desktop or other MCP-enabled AI assistant
- For MCP.Remote: Azure Functions Core Tools, Azure Storage (or Azurite for local development)

Each project includes comprehensive documentation with setup instructions, available tools, usage examples, and deployment guides.

## 🔧 Common Technologies

All projects in this repository use:
- **C# / .NET 9**: Modern, cross-platform development
- **Model Context Protocol**: Standardized protocol for AI tool integration
- **Microsoft.Extensions.Hosting**: Robust hosting framework
- **Dependency Injection**: Clean, testable architecture
- **JSON-RPC 2.0**: Standard remote procedure call protocol
- **Asynchronous Programming**: High-performance async/await patterns

## 🏗️ Architecture Overview

The repository demonstrates two primary MCP transport implementations:

- **STDIO Transport** (`MCP.stdio`): Direct integration with AI assistants using standard input/output
- **SSE Transport** (`MCP.Remote`): Server-Sent Events based implementation for cloud deployment with Azure Functions

## 📖 Project Documentation

For detailed information about each project, including setup instructions, API references, and usage examples, please refer to the individual project README files:

- [MCPDemo Overview](MCPDemo/ReadMe.md) - Comprehensive guide to MCP concepts and implementation details
- [MCP.stdio Documentation](MCPDemo/MCP.stdio/README.md) - Standard I/O transport implementation
- [MCP.Remote Documentation](MCPDemo/MCP.Remote/README.md) - Server-Sent Events transport with Azure Functions

---

*Explore the world of Model Context Protocol servers! 🚀*