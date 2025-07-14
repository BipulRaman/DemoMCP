// MCP Chat Client - Main JavaScript Module

class MCPChatClient {
    constructor() {
        this.isConnected = false;
        this.currentConnection = null;
        this.availableTools = [];
        this.conversationHistory = [];
        
        this.initializeElements();
        this.attachEventListeners();
        this.setupAutoResize();
    }

    initializeElements() {
        // Connection elements
        this.connectBtn = document.getElementById('connect-btn');
        this.connectionStatus = document.getElementById('connection-status');
        this.httpUrlInput = document.getElementById('http-url');
        this.stdioCommandInput = document.getElementById('stdio-command');
        this.httpConfig = document.getElementById('http-config');
        this.stdioConfig = document.getElementById('stdio-config');
        
        // Chat elements
        this.chatMessages = document.getElementById('chat-messages');
        this.chatInput = document.getElementById('chat-input');
        this.sendBtn = document.getElementById('send-btn');
        this.clearChatBtn = document.getElementById('clear-chat');
        this.toolsList = document.getElementById('tools-list');
        
        // Settings
        this.autoScrollCheckbox = document.getElementById('auto-scroll');
        this.showRawCheckbox = document.getElementById('show-raw');
    }

    attachEventListeners() {
        // Connection type radio buttons
        document.querySelectorAll('input[name="connection-type"]').forEach(radio => {
            radio.addEventListener('change', (e) => {
                this.handleConnectionTypeChange(e.target.value);
            });
        });

        // Connect button
        this.connectBtn.addEventListener('click', () => {
            this.handleConnection();
        });

        // Chat input
        this.chatInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });

        this.chatInput.addEventListener('input', () => {
            this.updateSendButton();
        });

        // Send button
        this.sendBtn.addEventListener('click', () => {
            this.sendMessage();
        });

        // Clear chat
        this.clearChatBtn.addEventListener('click', () => {
            this.clearChat();
        });

        // Tool items click
        this.toolsList.addEventListener('click', (e) => {
            const toolItem = e.target.closest('.tool-item');
            if (toolItem) {
                this.insertToolSuggestion(toolItem.dataset.toolName);
            }
        });
    }

    setupAutoResize() {
        this.chatInput.addEventListener('input', () => {
            this.chatInput.style.height = 'auto';
            this.chatInput.style.height = Math.min(this.chatInput.scrollHeight, 120) + 'px';
        });
    }

    handleConnectionTypeChange(type) {
        this.httpConfig.classList.toggle('hidden', type !== 'http');
        this.stdioConfig.classList.toggle('hidden', type !== 'stdio');
    }

    async handleConnection() {
        if (this.isConnected) {
            this.disconnect();
            return;
        }

        const connectionType = document.querySelector('input[name="connection-type"]:checked').value;
        
        this.setConnectionStatus('connecting', 'Connecting...');
        this.connectBtn.disabled = true;

        try {
            if (connectionType === 'http') {
                await this.connectToHttpServer();
            } else {
                await this.connectToStdioServer();
            }
        } catch (error) {
            this.setConnectionStatus('error', `Connection failed: ${error.message}`);
            this.connectBtn.disabled = false;
        }
    }

    async connectToHttpServer() {
        const url = this.httpUrlInput.value.trim();
        if (!url) {
            throw new Error('HTTP URL is required');
        }

        try {
            // Test connection
            const response = await fetch(`${url}/health`);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            // Get server capabilities using JSON-RPC 2.0
            const capabilitiesResponse = await fetch(`${url}/mcp`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    jsonrpc: '2.0',
                    id: 1,
                    method: 'initialize',
                    params: {
                        protocolVersion: '2024-11-05',
                        capabilities: {
                            roots: {
                                listChanged: true
                            },
                            sampling: {}
                        },
                        clientInfo: {
                            name: 'MCP Chat Client',
                            version: '1.0.0'
                        }
                    }
                })
            });

            if (!capabilitiesResponse.ok) {
                throw new Error(`Failed to initialize: ${capabilitiesResponse.statusText}`);
            }

            const capabilities = await capabilitiesResponse.json();
            
            this.currentConnection = {
                type: 'http',
                url: url,
                capabilities: capabilities
            };

            await this.loadTools();
            this.setConnectionStatus('connected', 'Connected (HTTP)');
            this.connectBtn.textContent = 'Disconnect';
            this.connectBtn.disabled = false;
            this.isConnected = true;
            this.updateSendButton();

            this.addSystemMessage('Successfully connected to HTTP MCP server! 🚀');

        } catch (error) {
            throw new Error(`HTTP connection failed: ${error.message}`);
        }
    }

    async connectToStdioServer() {
        // For demonstration purposes, we'll simulate STDIO connection
        // In a real implementation, you'd need a WebSocket or similar bridge
        throw new Error('STDIO connection requires a bridge service (not implemented in this demo)');
    }

    async loadTools() {
        if (!this.isConnected || this.currentConnection.type !== 'http') {
            return;
        }

        try {
            const response = await fetch(`${this.currentConnection.url}/mcp`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    jsonrpc: '2.0',
                    id: 2,
                    method: 'tools/list'
                })
            });

            if (!response.ok) {
                throw new Error(`Failed to load tools: ${response.statusText}`);
            }

            const data = await response.json();
            if (data.error) {
                throw new Error(data.error.message);
            }
            
            this.availableTools = data.result?.tools || [];
            this.renderToolsList();

        } catch (error) {
            console.error('Error loading tools:', error);
            this.addSystemMessage(`Warning: Could not load tools - ${error.message}`);
        }
    }

    renderToolsList() {
        if (this.availableTools.length === 0) {
            this.toolsList.innerHTML = '<div class="no-tools">No tools available</div>';
            return;
        }

        this.toolsList.innerHTML = this.availableTools.map(tool => `
            <div class="tool-item" data-tool-name="${tool.name}">
                <div class="tool-name">${tool.name}</div>
                <div class="tool-description">${tool.description || 'No description available'}</div>
            </div>
        `).join('');
    }

    disconnect() {
        this.isConnected = false;
        this.currentConnection = null;
        this.availableTools = [];
        this.setConnectionStatus('disconnected', 'Disconnected');
        this.connectBtn.textContent = 'Connect';
        this.connectBtn.disabled = false;
        this.renderToolsList();
        this.updateSendButton();
        this.addSystemMessage('Disconnected from MCP server');
    }

    setConnectionStatus(status, text) {
        this.connectionStatus.className = `connection-status ${status}`;
        this.connectionStatus.innerHTML = `<i class="fas fa-circle"></i> ${text}`;
    }

    async sendMessage() {
        const message = this.chatInput.value.trim();
        if (!message || !this.isConnected) {
            return;
        }

        this.addUserMessage(message);
        this.chatInput.value = '';
        this.chatInput.style.height = 'auto';
        this.updateSendButton();

        // Show typing indicator
        this.showTypingIndicator();

        try {
            await this.processMessage(message);
        } catch (error) {
            this.addErrorMessage(`Error: ${error.message}`);
        } finally {
            this.hideTypingIndicator();
        }
    }

    async processMessage(message) {
        // Add to conversation history
        this.conversationHistory.push({ role: 'user', content: message });

        // Analyze message to determine if tools should be used
        const toolsToUse = this.analyzeMessageForTools(message);
        
        let response = '';
        let toolResults = [];

        // Execute tools if needed
        if (toolsToUse.length > 0) {
            for (const toolName of toolsToUse) {
                try {
                    const result = await this.executeTool(toolName, message);
                    toolResults.push({ tool: toolName, result });
                    this.showToolUsage(toolName, result);
                } catch (error) {
                    toolResults.push({ tool: toolName, error: error.message });
                    this.showToolError(toolName, error.message);
                }
            }
        }

        // Generate response based on results
        response = this.generateResponse(message, toolResults);
        this.addAssistantMessage(response);

        // Add to conversation history
        this.conversationHistory.push({ role: 'assistant', content: response });
    }

    analyzeMessageForTools(message) {
        const messageLower = message.toLowerCase();
        const toolsToUse = [];

        // Simple keyword-based tool detection
        if (messageLower.includes('restaurant') && (messageLower.includes('list') || messageLower.includes('show') || messageLower.includes('get') || messageLower.includes('what'))) {
            toolsToUse.push('get_restaurants');
        }
        
        if (messageLower.includes('add') && messageLower.includes('restaurant')) {
            toolsToUse.push('add_restaurant');
        }
        
        if ((messageLower.includes('pick') || messageLower.includes('choose') || messageLower.includes('random')) && messageLower.includes('restaurant')) {
            toolsToUse.push('pick_random_restaurant');
        }

        if (messageLower.includes('statistics') || messageLower.includes('stats') || messageLower.includes('visit')) {
            toolsToUse.push('get_visit_statistics');
        }

        return toolsToUse;
    }

    async executeTool(toolName, message) {
        if (!this.isConnected || this.currentConnection.type !== 'http') {
            throw new Error('Not connected to HTTP server');
        }

        let arguments_obj = {};

        // Extract arguments for add_restaurant tool
        if (toolName === 'add_restaurant') {
            arguments_obj = this.extractRestaurantInfo(message);
        }

        const response = await fetch(`${this.currentConnection.url}/mcp`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                jsonrpc: '2.0',
                id: Date.now(), // Use timestamp as unique ID
                method: 'tools/call',
                params: {
                    name: toolName,
                    arguments: arguments_obj
                }
            })
        });

        if (!response.ok) {
            throw new Error(`Tool execution failed: ${response.statusText}`);
        }

        const result = await response.json();
        if (result.error) {
            throw new Error(result.error.message);
        }
        
        return result.result;
    }

    extractRestaurantInfo(message) {
        // Simple extraction - in a real implementation, you'd use NLP
        const messageLower = message.toLowerCase();
        
        // Try to extract restaurant info from the message
        let name = '';
        let location = '';
        let foodType = '';

        // Look for patterns like "add [name] restaurant" or "add [name] at [location]"
        const addMatch = message.match(/add\s+([^,]+?)(?:\s+(?:restaurant|at|in|on)\s+([^,]+?))?(?:\s+(?:serving|for)\s+([^,]+?))?[.!?]?$/i);
        if (addMatch) {
            name = addMatch[1]?.trim() || '';
            location = addMatch[2]?.trim() || '';
            foodType = addMatch[3]?.trim() || '';
        }

        // Fallback prompts if information is missing
        if (!name) {
            name = 'New Restaurant';
        }
        if (!location) {
            location = 'Unknown Location';
        }
        if (!foodType) {
            foodType = 'Various';
        }

        return { name, location, foodType };
    }

    generateResponse(message, toolResults) {
        if (toolResults.length === 0) {
            return "I understand your message, but I'm not sure which tool to use. Try asking about restaurants, adding a restaurant, or picking a random restaurant!";
        }

        let response = '';
        
        for (const result of toolResults) {
            if (result.error) {
                response += `❌ Error with ${result.tool}: ${result.error}\n\n`;
                continue;
            }

            switch (result.tool) {
                case 'get_restaurants':
                    response += this.formatRestaurantList(result.result);
                    break;
                case 'add_restaurant':
                    response += this.formatAddRestaurantResult(result.result);
                    break;
                case 'pick_random_restaurant':
                    response += this.formatRandomRestaurantResult(result.result);
                    break;
                case 'get_visit_statistics':
                    response += this.formatVisitStatistics(result.result);
                    break;
                default:
                    response += `✅ Tool ${result.tool} executed successfully.\n\n`;
            }
        }

        return response.trim();
    }

    formatRestaurantList(result) {
        try {
            // MCP server returns result with Content array containing text
            if (result && result.content && Array.isArray(result.content)) {
                return result.content.map(c => c.text || c.Text || '').join('\n') + '\n\n';
            }
            
            // Fallback for direct data
            const data = typeof result === 'string' ? JSON.parse(result) : result;
            const restaurants = data.content || data || [];
            
            if (!Array.isArray(restaurants) || restaurants.length === 0) {
                return "🍽️ No restaurants found. Add some restaurants first!\n\n";
            }

            let response = `🍽️ **Available Restaurants (${restaurants.length})**:\n\n`;
            restaurants.forEach((restaurant, index) => {
                response += `${index + 1}. **${restaurant.name}**\n`;
                response += `   📍 ${restaurant.location}\n`;
                response += `   🍴 ${restaurant.foodType}\n`;
                if (restaurant.visitCount > 0) {
                    response += `   🔢 Visited: ${restaurant.visitCount} times\n`;
                }
                response += '\n';
            });
            
            return response;
        } catch (error) {
            return `🍽️ Restaurant list: ${JSON.stringify(result)}\n\n`;
        }
    }

    formatAddRestaurantResult(result) {
        try {
            // MCP server returns result with Content array containing text
            if (result && result.content && Array.isArray(result.content)) {
                return result.content.map(c => c.text || c.Text || '').join('\n') + '\n\n';
            }
            
            const data = typeof result === 'string' ? JSON.parse(result) : result;
            const restaurant = data.content || data;
            
            return `✅ **Restaurant Added Successfully!**\n\n` +
                   `🏪 **${restaurant.name}**\n` +
                   `📍 Location: ${restaurant.location}\n` +
                   `🍴 Food Type: ${restaurant.foodType}\n\n`;
        } catch (error) {
            return `✅ Restaurant added: ${JSON.stringify(result)}\n\n`;
        }
    }

    formatRandomRestaurantResult(result) {
        try {
            // MCP server returns result with Content array containing text
            if (result && result.content && Array.isArray(result.content)) {
                return result.content.map(c => c.text || c.Text || '').join('\n') + '\n\n';
            }
            
            const data = typeof result === 'string' ? JSON.parse(result) : result;
            const content = data.content || data;
            
            if (content.message) {
                return `🎲 ${content.message}\n\n`;
            }
            
            return `🎲 Random restaurant selected!\n\n`;
        } catch (error) {
            return `🎲 Random selection: ${JSON.stringify(result)}\n\n`;
        }
    }

    formatVisitStatistics(result) {
        try {
            // MCP server returns result with Content array containing text
            if (result && result.content && Array.isArray(result.content)) {
                return result.content.map(c => c.text || c.Text || '').join('\n') + '\n\n';
            }
            
            const data = typeof result === 'string' ? JSON.parse(result) : result;
            const stats = data.content || data || [];
            
            if (!Array.isArray(stats) || stats.length === 0) {
                return "📊 No visit statistics available.\n\n";
            }

            let response = "📊 **Visit Statistics**:\n\n";
            stats.forEach((stat, index) => {
                response += `${index + 1}. **${stat.name}**: ${stat.visitCount} visits\n`;
            });
            
            return response + '\n';
        } catch (error) {
            return `📊 Visit statistics: ${JSON.stringify(result)}\n\n`;
        }
    }

    showToolUsage(toolName, result) {
        if (this.showRawCheckbox.checked) {
            const toolDiv = document.createElement('div');
            toolDiv.className = 'tool-usage';
            toolDiv.innerHTML = `
                <div class="tool-usage-header">🔧 Used tool: ${toolName}</div>
                <div class="tool-usage-details">
                    <div class="tool-result">
                        <div class="tool-result-header">Result:</div>
                        <div class="tool-result-content">${JSON.stringify(result, null, 2)}</div>
                    </div>
                </div>
            `;
            this.chatMessages.appendChild(toolDiv);
            this.scrollToBottom();
        }
    }

    showToolError(toolName, error) {
        const toolDiv = document.createElement('div');
        toolDiv.className = 'tool-usage';
        toolDiv.style.background = '#ffebee';
        toolDiv.style.borderColor = '#f44336';
        toolDiv.innerHTML = `
            <div class="tool-usage-header" style="color: #f44336;">❌ Tool error: ${toolName}</div>
            <div class="tool-usage-details" style="color: #f44336;">${error}</div>
        `;
        this.chatMessages.appendChild(toolDiv);
        this.scrollToBottom();
    }

    showTypingIndicator() {
        const typingDiv = document.createElement('div');
        typingDiv.className = 'typing-indicator';
        typingDiv.id = 'typing-indicator';
        typingDiv.innerHTML = `
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
        `;
        this.chatMessages.appendChild(typingDiv);
        this.scrollToBottom();
    }

    hideTypingIndicator() {
        const typingIndicator = document.getElementById('typing-indicator');
        if (typingIndicator) {
            typingIndicator.remove();
        }
    }

    addUserMessage(content) {
        this.addMessage('user', content);
    }

    addAssistantMessage(content) {
        this.addMessage('assistant', content);
    }

    addSystemMessage(content) {
        this.addMessage('system', content);
    }

    addErrorMessage(content) {
        this.addMessage('error', content);
    }

    addMessage(type, content) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${type}`;
        
        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        
        // Format content with basic markdown support
        contentDiv.innerHTML = this.formatContent(content);
        
        const timestampDiv = document.createElement('div');
        timestampDiv.className = 'message-timestamp';
        timestampDiv.textContent = new Date().toLocaleTimeString();
        
        messageDiv.appendChild(contentDiv);
        messageDiv.appendChild(timestampDiv);
        
        this.chatMessages.appendChild(messageDiv);
        this.scrollToBottom();
    }

    formatContent(content) {
        // Basic markdown formatting
        return content
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/`(.*?)`/g, '<code>$1</code>')
            .replace(/\n/g, '<br>');
    }

    clearChat() {
        this.chatMessages.innerHTML = `
            <div class="welcome-message">
                <div class="message-content">
                    <h3>👋 Welcome to MCP Chat Client!</h3>
                    <p>This client allows you to test Model Context Protocol (MCP) servers.</p>
                    <ul>
                        <li>Connect to your MCP server using the sidebar</li>
                        <li>Available tools will appear once connected</li>
                        <li>Chat naturally - the client will automatically use tools when needed</li>
                        <li>Try asking: "What restaurants are available?" or "Add a new restaurant"</li>
                    </ul>
                </div>
            </div>
        `;
        this.conversationHistory = [];
    }

    insertToolSuggestion(toolName) {
        let suggestion = '';
        switch (toolName) {
            case 'GetRestaurants':
                suggestion = 'What restaurants are available?';
                break;
            case 'AddRestaurant':
                suggestion = 'Add a new pizza restaurant at Main Street serving Italian food';
                break;
            case 'PickRandomRestaurant':
                suggestion = 'Pick a random restaurant for lunch';
                break;
            case 'GetVisitStatistics':
                suggestion = 'Show me visit statistics for all restaurants';
                break;
            default:
                suggestion = `Use ${toolName} tool`;
        }
        
        this.chatInput.value = suggestion;
        this.chatInput.focus();
        this.updateSendButton();
    }

    updateSendButton() {
        const hasMessage = this.chatInput.value.trim().length > 0;
        this.sendBtn.disabled = !hasMessage || !this.isConnected;
    }

    scrollToBottom() {
        if (this.autoScrollCheckbox.checked) {
            this.chatMessages.scrollTop = this.chatMessages.scrollHeight;
        }
    }
}

// Initialize the chat client when the page loads
document.addEventListener('DOMContentLoaded', () => {
    window.mcpChatClient = new MCPChatClient();
    console.log('MCP Chat Client initialized');
});

// Export for module usage
export default MCPChatClient;
