using System.Text.Json.Serialization;

namespace MCP.http.Models;

// Domain Models
public partial class Restaurant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
}

public partial class RestaurantVisitInfo
{
    public Restaurant Restaurant { get; set; } = new();
    public int VisitCount { get; set; }
    public DateTime? LastVisited { get; set; }
}

public partial class RestaurantData
{
    public List<Restaurant> Restaurants { get; set; } = new();
    public Dictionary<string, int> VisitCounts { get; set; } = new();
}

public class FormattedRestaurantStat
{
    public string Restaurant { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FoodType { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public string TimesEaten { get; set; } = string.Empty;
}

public class FormattedRestaurantStats
{
    public string Message { get; set; } = string.Empty;
    public List<FormattedRestaurantStat> Statistics { get; set; } = new();
    public int TotalRestaurants { get; set; }
    public int TotalVisits { get; set; }
}

// Tool Response Models for HTTP-specific features
public class RestaurantSelectionResult
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("restaurant")]
    public Restaurant? Restaurant { get; set; }
}

public class StreamingResponse<T>
{
    [JsonPropertyName("streaming")]
    public bool Streaming { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("metadata")]
    public StreamingMetadata? Metadata { get; set; }
}

public class StreamingMetadata
{
    [JsonPropertyName("chunkNumber")]
    public int ChunkNumber { get; set; }

    [JsonPropertyName("totalChunks")]
    public int? TotalChunks { get; set; }

    [JsonPropertyName("isLast")]
    public bool IsLast { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class SearchResults
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<Restaurant> Results { get; set; } = new();

    [JsonPropertyName("totalFound")]
    public int TotalFound { get; set; }
}

// Analysis Models for HTTP-specific streaming features
public class CuisineAnalysis
{
    [JsonPropertyName("totalCuisines")]
    public int TotalCuisines { get; set; }

    [JsonPropertyName("cuisineGroups")]
    public CuisineGroup[] CuisineGroups { get; set; } = [];

    [JsonPropertyName("mostPopularCuisine")]
    public string MostPopularCuisine { get; set; } = string.Empty;
}

public class CuisineGroup
{
    [JsonPropertyName("cuisine")]
    public string Cuisine { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("restaurants")]
    public string[] Restaurants { get; set; } = [];
}

public class LocationAnalysis
{
    [JsonPropertyName("totalAreas")]
    public int TotalAreas { get; set; }

    [JsonPropertyName("locationGroups")]
    public LocationGroup[] LocationGroups { get; set; } = [];

    [JsonPropertyName("mostPopularArea")]
    public string MostPopularArea { get; set; } = string.Empty;
}

public class LocationGroup
{
    [JsonPropertyName("area")]
    public string Area { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("restaurants")]
    public string[] Restaurants { get; set; } = [];
}

public class PopularityAnalysis
{
    [JsonPropertyName("totalVisits")]
    public int TotalVisits { get; set; }

    [JsonPropertyName("totalRestaurants")]
    public int TotalRestaurants { get; set; }

    [JsonPropertyName("topRestaurants")]
    public PopularRestaurant[] TopRestaurants { get; set; } = [];

    [JsonPropertyName("averageVisitsPerRestaurant")]
    public double AverageVisitsPerRestaurant { get; set; }
}

public class PopularRestaurant
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("visitCount")]
    public int VisitCount { get; set; }

    [JsonPropertyName("timesEaten")]
    public string TimesEaten { get; set; } = string.Empty;
}

public class GeneralAnalysis
{
    [JsonPropertyName("totalRestaurants")]
    public int TotalRestaurants { get; set; }

    [JsonPropertyName("totalCuisines")]
    public int TotalCuisines { get; set; }

    [JsonPropertyName("totalVisits")]
    public int TotalVisits { get; set; }

    [JsonPropertyName("averageVisitsPerRestaurant")]
    public double AverageVisitsPerRestaurant { get; set; }

    [JsonPropertyName("recommendations")]
    public string[] Recommendations { get; set; } = [];
}

// JSON Source Generation Context for better performance (following SDK samples pattern)
[JsonSerializable(typeof(List<Restaurant>))]
[JsonSerializable(typeof(Restaurant))]
[JsonSerializable(typeof(RestaurantVisitInfo))]
[JsonSerializable(typeof(Dictionary<string, RestaurantVisitInfo>))]
[JsonSerializable(typeof(RestaurantData))]
[JsonSerializable(typeof(FormattedRestaurantStat))]
[JsonSerializable(typeof(FormattedRestaurantStats))]
[JsonSerializable(typeof(RestaurantSelectionResult))]
[JsonSerializable(typeof(StreamingResponse<List<Restaurant>>))]
[JsonSerializable(typeof(StreamingResponse<SearchResults>))]
[JsonSerializable(typeof(StreamingResponse<CuisineAnalysis>))]
[JsonSerializable(typeof(StreamingResponse<LocationAnalysis>))]
[JsonSerializable(typeof(StreamingResponse<PopularityAnalysis>))]
[JsonSerializable(typeof(StreamingResponse<GeneralAnalysis>))]
internal sealed partial class RestaurantContext : JsonSerializerContext
{
}