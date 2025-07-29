# MCP.SSE Authentication Flow

> Browser-based OAuth2 authentication implementation for MCP.SSE server with Azure Active Directory integration.

## Overview

The MCP.SSE server implements **browser-based OAuth2 authorization code flow** with Azure AD, protecting MCP tools, resources, and prompts while allowing public access to initialization endpoints.

## Architecture

```mermaid
graph TB
    A[Client Request] --> B{Request Type?}
    B -->|MCP Protocol| C[McpAuthenticationMiddleware]
    B -->|HTTP API| D[Standard Auth Pipeline]
    
    C --> E{Extract Method}
    E --> F{Method Type?}
    F -->|Public Methods| G[Allow Without Auth]
    F -->|Protected Methods| H{User Authenticated?}
    
    H -->|No| I[Return Auth Required Response]
    H -->|Yes| J[Continue to MCP Handler]
    
    I --> K[Browser Auth Flow Instructions]
    
    D --> L[JWT Bearer Token Validation]
    L --> M{Valid Token?}
    M -->|No| N[401 Unauthorized]
    M -->|Yes| O[Continue to Handler]
```

## Key Components

### 1. McpAuthenticationMiddleware
**File:** `Middleware/McpAuthenticationMiddleware.cs`

Intercepts MCP requests and enforces authentication rules.

```csharp
// Protected Method Prefixes
private static readonly string[] ProtectedMethodPrefixes = { "tools/", "resources/", "prompts/" };

// Public Methods (No Auth Required)
private static readonly string[] AllowedMethods = { "initialize", "initialized", "ping", "notifications/initialized" };
```

### 2. JWT Authentication
**File:** `Extensions/ServiceCollectionExtensions.cs`

Configures Azure AD JWT Bearer token validation with support for header and query string tokens.

### 3. Custom MCP Service
**File:** `Services/CustomMcpService.cs`

Bypasses built-in MCP library session validation for browser auth scenarios.

**Key Features:**
- **Direct HTTP Authentication:** Uses `[Authorize]` attribute for per-request JWT validation
- **No Session Dependency:** Each request is independently authenticated
- **MCP Protocol Translation:** Converts HTTP requests to MCP protocol responses
- **Bypasses Library Limitations:** Avoids "user mismatch" errors from built-in MCP session validation

## Authentication Flow Through `/mcp-custom`

The `/mcp-custom` endpoint provides a simplified authentication model designed for browser-based OAuth flows.

### Process Overview

```mermaid
sequenceDiagram
    participant VSCode as VS Code MCP
    participant Custom as /mcp-custom Endpoint
    participant Auth as [Authorize] Attribute
    participant Azure as Azure AD
    
    VSCode->>Custom: POST /mcp-custom (no token)
    Custom->>Auth: Check Authorization header
    Auth-->>Custom: 401 Unauthorized
    Custom-->>VSCode: 401 + Auth instructions
    
    Note over VSCode: User follows browser auth flow
    VSCode->>Custom: POST /mcp-custom (with Bearer token)
    Custom->>Auth: Validate JWT token
    Auth->>Azure: Verify signature & claims
    Azure-->>Auth: Token valid
    Auth-->>Custom: User authenticated
    Custom->>Custom: Process MCP request
    Custom-->>VSCode: MCP response (tools/resources/etc)
```

### Implementation Details

#### Step 1: Initial Request (No Authentication)
VS Code MCP extension sends request without token:

```json
POST /mcp-custom
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 1
}
```

**Response:**
```json
{
  "error": {
    "code": -32001,
    "message": "Authentication required",
    "data": {
      "auth_flow": "authorization_code",
      "authorize_endpoint": "http://localhost:5116/auth/authorize",
      "token_endpoint": "http://localhost:5116/auth/token"
    }
  }
}
```

#### Step 2: Browser Authentication Cycle
User manually performs OAuth flow:

1. **Get Authorization URL**
   ```bash
   POST /auth/authorize → Get authorization URL
   ```

2. **Browser Authentication**
   ```bash
   Open URL in browser → Authenticate with Azure AD
   ```

3. **Get Authorization Code**
   ```bash
   Get authorization code from callback page
   ```

4. **Exchange for Token**
   ```bash
   POST /auth/token with code → Get access token
   ```

#### Step 3: Authenticated Request
Update VS Code MCP configuration:

```json
{
  "servers": {
    "local-mcp-sse-browser-auth": {
      "type": "http",
      "url": "http://localhost:5116/mcp-custom",
      "headers": {
        "Authorization": "Bearer eyJ0eXAiOiJKV1Q..."
      }
    }
  }
}
```

**Successful Request:**
```json
POST /mcp-custom
Authorization: Bearer eyJ0eXAiOiJKV1Q...
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 1
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [
      {
        "name": "list_snippets",
        "description": "List all available code snippets"
      },
      {
        "name": "get_snippet",
        "description": "Get a specific code snippet by name"
      }
    ]
  },
  "id": 1
}
```

### Endpoint Comparison

| Aspect | `/mcp-custom` | Standard `/` (SSE) |
|--------|---------------|-------------------|
| **Authentication** | Per-request JWT validation | Session-based validation |
| **Transport** | HTTP POST requests | Server-Sent Events |
| **Session State** | Stateless | Requires consistent session |
| **Token Handling** | Authorization header only | Header or query parameter |
| **Error Handling** | Standard HTTP 401/403 | MCP error responses |
| **VS Code Compatibility** | Full compatibility | Session mismatch issues |

### Why `/mcp-custom` Was Created

**Standard Endpoint Issues:**
- Built-in MCP library enforces session validation
- Browser auth tokens obtained AFTER connection establishment
- Results in "user mismatch" errors
- Session-based validation conflicts with stateless OAuth flow

**Custom Endpoint Solution:**
- Uses ASP.NET Core `[Authorize]` attribute
- Per-request authentication without session dependency
- Direct HTTP authentication model
- No session state conflicts

### Authentication Architecture

```mermaid
graph TB
    A[VS Code Request] --> B[/mcp-custom Endpoint]
    B --> C[Authorize Attribute]
    C --> D{JWT Token Present?}
    D -->|No| E[401 Unauthorized]
    D -->|Yes| F[JWT Validation Pipeline]
    F --> G{Token Valid?}
    G -->|No| H[401/403 Error]
    G -->|Yes| I[CustomMcpService.HandleMcpRequestAsync]
    I --> J{MCP Method?}
    J -->|tools/list| K[Return Available Tools]
    J -->|tools/call| L[Execute Tool Function]
    J -->|resources/list| M[Return Available Resources]
    J -->|Other| N[Handle MCP Protocol Method]
```

## Endpoints

### Public (No Auth)
- `/health` - Server health check
- `/capabilities` - Server capabilities and auth requirements

### Authentication
- `/auth/authorize` - Generate authorization URL
- `/auth/callback` - OAuth callback (displays auth code)
- `/auth/token` - Exchange code for access token

### Protected (Auth Required)
- `/` - Standard MCP endpoint (SSE) - JWT via header or query
- `/mcp-custom` - Custom MCP endpoint (HTTP) - JWT via Authorization header

## Configuration

### Azure AD Settings
**File:** `appsettings.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

### Authentication Options
**File:** `appsettings.json`

```json
{
  "Authentication": {
    "ServerUrl": "http://localhost:5116/",
    "RequiredScopes": ["mcp:tools", "mcp:resources"],
    "RequiredRoles": ["MCP.User"]
  }
}
```

## VS Code Integration

### Basic Setup
**File:** `.vscode/mcp.json`

```json
{
  "servers": {
    "local-mcp-sse-browser-auth": {
      "type": "http",
      "url": "http://localhost:5116/mcp-custom"
    }
  }
}
```

### With Authentication Token
**Note:** Token must be manually obtained via browser flow

```json
{
  "servers": {
    "local-mcp-sse-browser-auth": {
      "type": "http",
      "url": "http://localhost:5116/mcp-custom",
      "headers": {
        "Authorization": "Bearer manually-obtained-token-here"
      }
    }
  }
}
```

**Token Setup Process:**
1. Run authentication flow manually via curl/browser
2. Copy the obtained access token
3. Update VS Code MCP configuration with token
4. Token expires after 1 hour - repeat when expired

## Quick Start

### 1. Test Without Authentication
```bash
curl -X POST http://localhost:5116/mcp-custom \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

### 2. Get Authorization URL
```bash
curl -X POST http://localhost:5116/auth/authorize \
  -H "Content-Type: application/json" -d '{}'
```

### 3. Complete Browser Authentication
1. Open authorization URL in browser
2. Authenticate with Azure AD
3. Copy authorization code from callback page

### 4. Exchange Code for Token
```bash
curl -X POST http://localhost:5116/auth/token \
  -H "Content-Type: application/json" \
  -d '{"code":"auth_code_from_browser"}'
```

### 5. Test With Authentication
```bash
curl -X POST http://localhost:5116/mcp-custom \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

## Endpoints Reference

### Public Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Server health check |
| `/capabilities` | GET | Server capabilities and auth requirements |

### Authentication Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/auth/authorize` | POST | Generate authorization URL |
| `/auth/callback` | GET | OAuth callback (displays auth code) |
| `/auth/token` | POST | Exchange code for access token |

### Protected Endpoints
| Endpoint | Method | Authentication | Description |
|----------|--------|----------------|-------------|
| `/` | POST | JWT (header/query) | Standard MCP endpoint (SSE) |
| `/mcp-custom` | POST | JWT (header only) | Custom MCP endpoint (HTTP) |

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Authentication required" | No token provided | Follow browser auth flow |
| "Invalid token" | Expired/malformed JWT | Re-authenticate |
| "User mismatch" | Using SSE endpoint | Use `/mcp-custom` endpoint |
| CORS errors | Cross-origin blocked | Check CORS configuration |

### Debug Logging
```json
{
  "Logging": {
    "LogLevel": {
      "MCP.SSE.Middleware.McpAuthenticationMiddleware": "Debug",
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  }
}
```

## Security Features

- **CSRF Protection**: State parameter validation
- **JWT Signature Verification**: Against Azure AD public keys
- **Token Expiration**: Automatic validation
- **Scope-based Access Control**: Per-token permissions
- **Secure Headers**: CORS and content-type validation

## Testing

Use the provided PowerShell test script:
```bash
.\TestScripts\test-browser-auth.ps1
```
