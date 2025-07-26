using System.Text.Json.Serialization;

namespace MCP.Shared;

public partial class Snippet
{
    public string? Name { get; set; }
    public string? Content { get; set; }
    public string? Language { get; set; }
}

public partial class SnippetResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public Snippet? Snippet { get; set; }
    public string? SnippetName { get; set; }
}

public partial class SnippetListResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string>? Snippets { get; set; }
    public int Count { get; set; }
}

[JsonSerializable(typeof(Snippet))]
[JsonSerializable(typeof(List<Snippet>))]
[JsonSerializable(typeof(SnippetResponse))]
[JsonSerializable(typeof(SnippetListResponse))]
[JsonSerializable(typeof(List<string>))]
public partial class SnippetContext : JsonSerializerContext
{
}
