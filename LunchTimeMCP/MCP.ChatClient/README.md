# MCP Chat Client

A modern web-based chat client for testing Model Context Protocol (MCP) servers, similar to Claude Desktop.

## Features

- 🎨 **Modern UI**: Beautiful, responsive interface similar to Claude Desktop
- 🔌 **Multiple Connection Types**: Support for HTTP and STDIO MCP servers
- 🛠️ **Tool Integration**: Automatic tool discovery and intelligent usage
- 💬 **Natural Chat**: Chat naturally and let the client determine when to use tools
- 📱 **Responsive Design**: Works on desktop and mobile devices
- ⚙️ **Configurable**: Various settings for customization

## Quick Start

1. **Install dependencies**:
   ```bash
   npm install
   ```

2. **Start the development server**:
   ```bash
   npm run dev
   ```

3. **Open your browser** to `http://localhost:3000`

4. **Connect to your MCP server**:
   - For HTTP: Enter your server URL (default: `http://localhost:5000`)
   - For STDIO: Enter the command to run your server

## Usage

### Connecting to HTTP MCP Server

1. Start your MCP HTTP server (e.g., the `MCP.http` project)
2. In the chat client, select "HTTP Server"
3. Enter the server URL (default: `http://localhost:5000`)
4. Click "Connect"

### Connecting to STDIO MCP Server

Note: STDIO connection requires a bridge service (not implemented in this demo).
For a full implementation, you'd need a WebSocket bridge or similar.

### Chat Examples

Once connected, try these example messages:

- **"What restaurants are available?"** - Lists all restaurants
- **"Add a new pizza place at Main Street"** - Adds a restaurant
- **"Pick a random restaurant for lunch"** - Selects a random restaurant
- **"Show me visit statistics"** - Displays visit statistics

## Architecture

### Components

- **MCPChatClient**: Main application class
- **Connection Manager**: Handles HTTP/STDIO connections
- **Tool Analyzer**: Determines which tools to use based on user messages
- **Response Formatter**: Formats tool results into readable responses

### Tool Detection

The client uses keyword-based analysis to determine when to use tools:

- Restaurant listings: "list", "show", "get", "what" + "restaurant"
- Adding restaurants: "add" + "restaurant"
- Random selection: "pick", "choose", "random" + "restaurant"
- Statistics: "statistics", "stats", "visit"

### Message Flow

1. User sends message
2. Client analyzes message for tool usage
3. Tools are executed if needed
4. Results are formatted and displayed
5. Conversation continues naturally

## Development

### File Structure

```
MCP.ChatClient/
├── index.html          # Main HTML file
├── styles.css          # CSS styles
├── main.js            # Main JavaScript application
├── package.json       # NPM configuration
├── vite.config.js     # Vite configuration
└── README.md          # This file
```

### Building for Production

```bash
npm run build
```

The built files will be in the `dist/` directory.

### Serving Production Build

```bash
npm run preview
```

## Customization

### Adding New Tools

1. Update the `analyzeMessageForTools()` method to detect your tool
2. Add formatting logic in `generateResponse()` method
3. Update tool suggestion logic in `insertToolSuggestion()`

### Styling

Modify `styles.css` to customize the appearance. The design uses:
- CSS Grid and Flexbox for layout
- CSS Variables for theming
- Smooth transitions and animations
- Responsive design principles

## Troubleshooting

### Connection Issues

- Ensure your MCP server is running
- Check the server URL is correct
- Verify CORS is enabled on your MCP server
- Check browser console for error messages

### Tool Detection Issues

- Use specific keywords that the analyzer recognizes
- Check the `analyzeMessageForTools()` method for supported patterns
- Try rephrasing your message

## Browser Compatibility

- Chrome/Edge 88+
- Firefox 85+
- Safari 14+

## License

MIT License - feel free to use and modify as needed.
