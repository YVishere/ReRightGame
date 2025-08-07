# Scripts - Core Game Systems

This directory contains the main C# scripts that power the ReRightGame, a 2D Unity game featuring AI-powered NPCs with LLM integration. The architecture follows a modular design with clear separation of concerns between different game systems.

## Architecture Overview

The game operates on a client-server architecture where Unity (C#) handles game logic and rendering, while a Python server manages AI/LLM operations. Communication occurs through TCP socket connections with connection pooling for multiple AI NPCs.

## Core Files

### GameController.cs
**Primary game state manager** that orchestrates the main game loop and state transitions.
- Manages `GameState` enum: `freeRoam` and `dialogMode`
- Delegates update calls to appropriate controllers based on current state
- Handles transitions between free exploration and conversation modes
- Integrates with DialogManager for seamless UI state management
- **Required Component**: Must have AuthManager component attached for IPC authentication
- Coordinates with AuthManager for secure AI server communication

### GameLayers.cs
**Unity layer management system** providing centralized access to collision layers.
- Singleton pattern for global layer access
- Defines layer masks: `SolidObjects`, `InteractableLayer`, `PlayerLayer`, `DefaultLayer`
- Enables efficient physics queries and collision detection
- Critical for movement validation and interaction detection

### Hasher.cs
**Connection management system** for AI NPC network connections.
- Maintains hash table mapping NPC GUIDs to TCP connections (`Dictionary<GUID, ConnectionInfo>`)
- Singleton pattern for global connection access
- Handles connection lifecycle management and cleanup
- Provides connection validation and retrieval methods
- Essential for multi-NPC AI communication architecture

### ConnectionInfo.cs
**Network connection wrapper** encapsulating TCP client and stream management.
- Wraps `TcpClient` and `NetworkStream` with lifecycle management
- Provides connection state validation
- Handles proper resource cleanup to prevent memory leaks
- Thread-safe connection management for concurrent AI operations

### Killports.cs
**Development utility** for network debugging and cleanup.
- Handles port management and socket cleanup during development
- Prevents "address already in use" errors during rapid iteration
- Development-specific tooling for network troubleshooting

## Directory Structure

### `/Authentication`
**IPC-based authentication system** securing AI server communication.
- AuthManager.cs: Manages dynamic secret generation and named pipe communication
- Provides session-based authentication for Unity-Python server communication
- Uses Windows named pipes for secure, in-memory credential exchange
- **Setup Required**: AuthManager must be attached to GameController object
- Generates unique authentication secrets per game session using system identifiers

### `/Dialogs`
**Conversation system** managing both scripted and AI-generated conversations.
- Dialog data structures and conversation management
- UI rendering and user input handling
- Integration layer between game logic and AI responses

### `/NPC`
**Non-Player Character system** with AI integration capabilities.
- NPC behavior controllers and state management
- LLM integration for dynamic conversation generation
- Interaction interfaces and event systems

### `/Player`
**Player character system** handling user input and character control.
- Input processing and movement validation
- Interaction initiation and world state awareness
- Character initialization and component setup

### `/PlayerNPCcommon`
**Shared character systems** used by both player and NPCs.
- Movement mechanics and animation controllers
- Common character behaviors and utilities
- Collision detection and physics integration

### `/Physics`
**Collision and interaction systems** managing world physics.
- Collision detection and response systems
- Interaction zone management and validation
- Physics-based game mechanics

### `/ServerFiles`
**Network communication layer** for Unity-Python integration with mandatory IPC authentication.
- Socket client implementation for AI communication with token-based authentication
- Connection management and request handling with authentication handshakes
- Protocol definition for AI service communication with session validation
- **Authentication Required**: All connections must authenticate via AuthManager IPC system
- Supports secure multi-NPC concurrent connections with individual session tokens

### `/ServerFiles-API`
**Extended API communication** for additional server functionality.
- RESTful API client implementation
- Extended server communication protocols
- Additional network service integrations

### `/UnityEngineHelper`
**Unity Editor integration utilities** for development workflow enhancement.
- Domain reload management and resource cleanup
- Editor-specific development tools and utilities
- Development-time workflow support and debugging tools
- **DomainReloadHelper**: Prevents editor hanging during assembly reloads by managing IPC and network resource cleanup

## Technical Design Patterns

### Singleton Pattern
Multiple systems use singleton pattern for global access:
- `GameLayers` for collision layer management
- `Hasher` for connection management
- `DialogManager` for conversation control
- `ServerSocketC` for network communication

### Event-Driven Architecture
Components communicate through C# events to maintain loose coupling:
- NPC interaction events (`OnNPCInteractStart`, `OnNPCInteractEnd`)
- Dialog system events (`OnShowDialog`, `OnCloseDialog`)
- State change notifications across systems

### State Machine Pattern
Multiple systems implement state machines:
- Game states (free roam vs dialog mode)
- NPC states (idle, walking, speaking)
- Character animation states (directional movement)

### Component-Based Design
Unity's component system enables modular character construction:
- Shared components between player and NPCs
- Initialization scripts for runtime component setup
- Interface-based interaction system (`Interactable_intf`)

## Dependencies and Integration

### Unity Dependencies
- Unity 2D rendering and physics systems
- Input system for player control
- Coroutine system for async operations
- Component architecture for modular design

### External Dependencies
- Python server for LLM processing
- Ollama for local LLM inference
- TCP socket communication layer
- GUID system for unique entity identification
- **Windows Named Pipes**: For secure IPC authentication between Unity and Python
- **Python win32pipe libraries**: Required for authentication pipe communication

## Authentication Setup Requirements

### GameController Configuration
1. **AuthManager Component**: Must be attached to the GameController GameObject
2. **Initialization Order**: AuthManager initializes before ServerSocketC starts Python server
3. **Pipe Name Generation**: Creates unique named pipe per game session
4. **Session Management**: Generates dynamic secrets and session keys

### Security Architecture
- **Dynamic Secret Generation**: Uses Process ID, Machine Name, User Name, Timestamp, Unity Instance ID
- **Session-Based Authentication**: Each game session generates unique authentication credentials
- **Token Validation**: All AI requests include generated tokens for validation
- **IPC Communication**: Secrets exchanged via Windows named pipes (in-memory only)
- **No File System Usage**: Authentication data never written to disk

This modular architecture enables scalable AI integration while maintaining clean separation between game logic, rendering, and AI processing systems.
