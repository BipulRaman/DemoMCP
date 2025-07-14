# LunchTime MCP HTTP Server

A Model Context Protocol (MCP) server implemented as an HTTP API for managing lunch restaurant choices. This server provides the same functionality as the console MCP server but exposes it through HTTP endpoints.

## Features

- **Restaurant Management**: Add and retrieve restaurants
- **Random Selection**: Pick a random restaurant for lunch
- **Visit Tracking**: Track and view statistics about restaurant visits
- **HTTP API**: RESTful endpoints for all operations
- **MCP Protocol**: Dedicated MCP endpoints for tool discovery and execution
- **Swagger Documentation**: Interactive API documentation

## API Endpoints

### REST API Endpoints

- `GET /api/restaurant/restaurants` - Get all restaurants
- `POST /api/restaurant/restaurants` - Add a new restaurant
- `POST /api/restaurant/restaurants/random` - Pick a random restaurant
- `GET /api/restaurant/restaurants/statistics` - Get visit statistics

### MCP Protocol Endpoints

- `GET /api/mcp/info` - Get server information and available tools
- `POST /api/mcp/tools/call` - Execute MCP tool calls

## Usage

### Running the Server

```bash
dotnet run
```

The server will start on `https://localhost:7073` (HTTPS) and `http://localhost:5073` (HTTP).

### Swagger UI

Visit the root URL when running in development mode to access the Swagger UI for interactive API testing.

### Example API Calls

#### Get All Restaurants
```bash
curl https://localhost:7073/api/restaurant/restaurants
```

#### Add a Restaurant
```bash
curl -X POST https://localhost:7073/api/restaurant/restaurants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Pizza Palace",
    "location": "123 Main St",
    "foodType": "Italian"
  }'
```

#### Pick Random Restaurant
```bash
curl -X POST https://localhost:7073/api/restaurant/restaurants/random
```

#### Get Visit Statistics
```bash
curl https://localhost:7073/api/restaurant/restaurants/statistics
```

### MCP Tool Calls

#### Get Server Info
```bash
curl https://localhost:7073/api/mcp/info
```

#### Execute Tool Call
```bash
curl -X POST https://localhost:7073/api/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "add_restaurant",
    "parameters": {
      "name": "Pizza Palace",
      "location": "123 Main St",
      "foodType": "Italian"
    }
  }'
```

## Data Persistence

Restaurant data is stored in JSON format in the user's application data directory:
- Windows: `%APPDATA%\LunchTimeMCP\restaurants.json`
- macOS/Linux: `~/.config/LunchTimeMCP/restaurants.json`

## Initial Data

The server comes pre-loaded with trendy West Hollywood restaurants if no data exists:
- Guelaguetza (Oaxacan Mexican)
- Republique (French Bistro)
- Night + Market WeHo (Thai Street Food)
- Gracias Madre (Vegan Mexican)
- The Ivy (Californian)
- Catch LA (Seafood)
- Cecconi's (Italian)
- Earls Kitchen + Bar (Global Comfort Food)
- Pump Restaurant (Mediterranean)
- Craig's (American Contemporary)

## Technology Stack

- .NET 8
- ASP.NET Core Web API
- System.Text.Json for serialization
- Swagger/OpenAPI for documentation
- Dependency Injection for service management