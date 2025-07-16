using System.Text.Json.Serialization;

namespace MCP.http.Models;

// JSON-RPC 2.0 Base Classes
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object Id { get; set; }
    
    [JsonPropertyName("method")]
    public string Method { get; set; }
    
    [JsonPropertyName("params")]
    public object Params { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object Id { get; set; }
    
    [JsonPropertyName("result")]
    public object Result { get; set; }
}

public class JsonRpcErrorResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object Id { get; set; }
    
    [JsonPropertyName("error")]
    public JsonRpcError Error { get; set; } = new();
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
    
    [JsonPropertyName("data")]
    public object Data { get; set; }
}

// Streaming Support Models
public class StreamingJsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object Id { get; set; }
    
    [JsonPropertyName("result")]
    public object Result { get; set; }
    
    [JsonPropertyName("error")]
    public JsonRpcError Error { get; set; }
    
    [JsonPropertyName("streaming")]
    public StreamingInfo Streaming { get; set; }
}

public class StreamingInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "partial"; // "partial", "complete", "error"
    
    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }
    
    [JsonPropertyName("total")]
    public int? Total { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// MCP Specific Models
public class McpInitializeParams
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";
    
    [JsonPropertyName("capabilities")]
    public McpClientCapabilities Capabilities { get; set; }
    
    [JsonPropertyName("clientInfo")]
    public McpClientInfo ClientInfo { get; set; }
}

public class McpClientCapabilities
{
    [JsonPropertyName("roots")]
    public McpRootsCapability Roots { get; set; }
    
    [JsonPropertyName("sampling")]
    public object Sampling { get; set; }
    
    [JsonPropertyName("streaming")]
    public McpStreamingCapability Streaming { get; set; }
}

public class McpRootsCapability
{
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; set; }
}

public class McpStreamingCapability
{
    [JsonPropertyName("supported")]
    public bool Supported { get; set; } = true;
    
    [JsonPropertyName("protocols")]
    public string[] Protocols { get; set; } = { "chunked-json" };
}

public class McpClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}

public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";
    
    [JsonPropertyName("capabilities")]
    public McpServerCapabilities Capabilities { get; set; } = new();
    
    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

public class McpServerCapabilities
{
    [JsonPropertyName("tools")]
    public McpToolsCapability Tools { get; set; }
    
    [JsonPropertyName("resources")]
    public McpResourcesCapability Resources { get; set; }
    
    [JsonPropertyName("prompts")]
    public McpPromptsCapability Prompts { get; set; }
    
    [JsonPropertyName("streaming")]
    public McpStreamingServerCapability Streaming { get; set; }
}

public class McpToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; set; }
    
    [JsonPropertyName("streaming")]
    public bool? Streaming { get; set; } = true;
}

public class McpResourcesCapability
{
    [JsonPropertyName("subscribe")]
    public bool? Subscribe { get; set; }
    
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; set; }
}

public class McpPromptsCapability
{
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; set; }
}

public class McpStreamingServerCapability
{
    [JsonPropertyName("supported")]
    public bool Supported { get; set; } = true;
    
    [JsonPropertyName("protocols")]
    public string[] Protocols { get; set; } = { "chunked-json" };
    
    [JsonPropertyName("toolStreaming")]
    public bool ToolStreaming { get; set; } = true;
    
    [JsonPropertyName("promptStreaming")]
    public bool PromptStreaming { get; set; } = true;
}

public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "LunchTime MCP Streaming Server";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "HTTP-based MCP server with chunked streaming for lunch restaurant management";
}

// Tools
public class McpToolsListParams
{
    [JsonPropertyName("cursor")]
    public string Cursor { get; set; }
}

public class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public McpTool[] Tools { get; set; } = Array.Empty<McpTool>();
    
    [JsonPropertyName("nextCursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string NextCursor { get; set; }
}

public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };
    
    [JsonPropertyName("streaming")]
    public bool? Streaming { get; set; } = true;
}

public class McpToolCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; }
    
    [JsonPropertyName("streaming")]
    public bool? Streaming { get; set; }
}

public class McpToolCallResult
{
    [JsonPropertyName("content")]
    public McpContent[] Content { get; set; } = Array.Empty<McpContent>();
    
    [JsonPropertyName("isError")]
    public bool? IsError { get; set; }
    
    [JsonPropertyName("metadata")]
    public McpToolCallMetadata Metadata { get; set; }
}

public class McpToolCallMetadata
{
    [JsonPropertyName("executionTime")]
    public TimeSpan? ExecutionTime { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("streaming")]
    public StreamingMetadata Streaming { get; set; }
}

public class StreamingMetadata
{
    [JsonPropertyName("chunkNumber")]
    public int ChunkNumber { get; set; }
    
    [JsonPropertyName("totalChunks")]
    public int? TotalChunks { get; set; }
    
    [JsonPropertyName("isLast")]
    public bool IsLast { get; set; }
}

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

// Prompts
public class McpPromptsListParams
{
    [JsonPropertyName("cursor")]
    public string Cursor { get; set; }
}

public class McpPromptsListResult
{
    [JsonPropertyName("prompts")]
    public McpPrompt[] Prompts { get; set; } = Array.Empty<McpPrompt>();
    
    [JsonPropertyName("nextCursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string NextCursor { get; set; }
}

public class McpPrompt
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("arguments")]
    public McpPromptArgument[] Arguments { get; set; }
}

public class McpPromptArgument
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("required")]
    public bool? Required { get; set; }
}

public class McpPromptGetParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("arguments")]
    public Dictionary<string, string> Arguments { get; set; }
}

public class McpPromptGetResult
{
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("messages")]
    public McpPromptMessage[] Messages { get; set; } = Array.Empty<McpPromptMessage>();
}

public class McpPromptMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";
    
    [JsonPropertyName("content")]
    public McpContent Content { get; set; } = new();
}

// Streaming-specific response models
public class StreamingToolCallRequest
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = "";
    
    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; }
    
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("streaming")]
    public StreamingOptions Streaming { get; set; }
}

public class StreamingOptions
{
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "chunked-json";
    
    [JsonPropertyName("chunkSize")]
    public int? ChunkSize { get; set; }
    
    [JsonPropertyName("enableMetadata")]
    public bool EnableMetadata { get; set; } = true;
}

// Resources Support Models
public class McpResourcesListResult
{
    [JsonPropertyName("resources")]
    public McpResource[] Resources { get; set; } = Array.Empty<McpResource>();
}

public class McpResource
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }
}