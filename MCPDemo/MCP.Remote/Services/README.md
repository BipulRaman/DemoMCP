# Azure Blob Service Architecture

## Overview

This project has been refactored to implement a layered architecture that separates Azure Blob Storage operations from the MCP Tools. This provides better separation of concerns, testability, and maintainability.

## Architecture Layers

### 1. Service Layer (`Services/`)

#### `IAzBlobService` & `AzBlobService`
- **Purpose**: Generic Azure Blob Storage operations
- **Responsibilities**: 
  - Low-level blob operations (get, save, delete, list, exists)
  - Container management
  - Error handling and logging
- **Features**:
  - Async operations with cancellation support
  - String and Stream-based operations
  - Comprehensive error handling and logging

#### `ISnippetService` & `SnippetService`
- **Purpose**: Business logic for snippet management
- **Responsibilities**:
  - Snippet-specific operations
  - Validation of snippet names and content
  - Abstraction over blob storage for snippet use cases
- **Features**:
  - Snippet name validation
  - Content validation
  - Blob naming conventions (`.json` extension)
  - Business-specific error messages

### 2. Tool Layer (`Tools/`)

#### `SnippetsTool` (Refactored)
- **Purpose**: MCP Tool endpoints for snippet operations
- **Changes**:
  - Removed direct Azure Functions blob bindings
  - Uses `ISnippetService` for all operations
  - Added new operations: `ListSnippets`, `DeleteSnippet`
  - Improved error handling with structured responses
  - Returns JSON objects with success/error status

## Key Benefits

### 1. **Separation of Concerns**
- Azure Blob operations are isolated in `AzBlobService`
- Business logic is contained in `SnippetService`
- MCP Tools focus only on request/response handling

### 2. **Testability**
- Each layer can be unit tested independently
- Interfaces allow for easy mocking
- Service layer can be tested without Azure Functions runtime

### 3. **Reusability**
- `IAzBlobService` can be reused for other blob storage needs
- `ISnippetService` can be used by other tools or endpoints
- Clear contracts through interfaces

### 4. **Maintainability**
- Changes to blob storage logic don't affect tools
- Business logic changes are isolated to service layer
- Easy to add new snippet operations

### 5. **Error Handling**
- Structured error responses
- Comprehensive logging at each layer
- Graceful handling of missing resources

## Configuration

### Dependency Injection (Program.cs)
```csharp
// Register Azure Blob Service
builder.Services.AddSingleton<BlobServiceClient>(provider =>
{
    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    return new BlobServiceClient(connectionString);
});

builder.Services.AddScoped<IAzBlobService, AzBlobService>();
builder.Services.AddScoped<ISnippetService, SnippetService>();
```

### Required Environment Variables
- `AzureWebJobsStorage`: Azure Storage connection string

## Available Tools

### 1. `get_snippets`
- **Description**: Retrieves a snippet by name
- **Parameters**: `snippetname` (string)
- **Returns**: JSON with content and metadata

### 2. `save_snippet`
- **Description**: Saves a code snippet
- **Parameters**: 
  - `snippetname` (string): Name of the snippet
  - `snippet` (string): Code content
- **Returns**: JSON with success status

### 3. `list_snippets`
- **Description**: Lists all available snippets
- **Parameters**: None
- **Returns**: JSON with array of snippet names

### 4. `delete_snippet`
- **Description**: Deletes a snippet by name
- **Parameters**: `snippetname` (string)
- **Returns**: JSON with success status

## Migration Notes

### From Direct Blob Bindings to Service Layer

**Before:**
```csharp
[Function(nameof(GetSnippet))]
public object GetSnippet(
    [BlobInput(BlobPath)] string snippetContent
)
```

**After:**
```csharp
[Function(nameof(GetSnippet))]
public async Task<object> GetSnippet(
    [McpToolTrigger(...)] ToolInvocationContext context
)
{
    var snippetName = context.Arguments?.GetValueOrDefault("snippetname")?.ToString();
    var content = await _snippetService.GetSnippetAsync(snippetName);
    return new { success = true, content, snippetName };
}
```

### Benefits of Migration
- More control over error handling
- Structured response format
- Ability to add validation
- Better logging and monitoring
- Easier testing and debugging

## Future Enhancements

1. **Caching Layer**: Add Redis cache for frequently accessed snippets
2. **Versioning**: Implement snippet versioning
3. **Categories**: Add support for snippet categories/tags
4. **Search**: Implement snippet search functionality
5. **Backup**: Add automatic backup functionality
6. **Security**: Implement user-based access control

## Dependencies

- `Azure.Storage.Blobs` v12.22.1
- `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` v6.6.1
- `Microsoft.Azure.Functions.Worker.Extensions.Mcp` v1.0.0-preview.5
