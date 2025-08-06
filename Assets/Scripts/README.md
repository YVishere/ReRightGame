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
**Network communication layer** for Unity-Python integration.
- Socket client implementation for AI communication
- Connection management and request handling
- Protocol definition for AI service communication

### `/ServerFiles-API`
**Extended API communication** for additional server functionality.
- RESTful API client implementation
- Extended server communication protocols
- Additional network service integrations

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

This modular architecture enables scalable AI integration while maintaining clean separation between game logic, rendering, and AI processing systems.
