# NPC - Non-Player Character System

This directory contains the comprehensive NPC system that powers both traditional scripted NPCs and AI-enhanced characters with dynamic conversation capabilities. The system integrates movement patterns, interaction mechanics, and LLM-powered personality generation.

## Architecture Overview

The NPC system implements a modular character architecture where NPCs can operate in two modes: traditional scripted behavior or AI-enhanced dynamic personalities. AI NPCs register a per-NPC `LLamaContext` with `UnityLLMContextHasher` on startup (llama.cpp path), or establish individual TCP connections to the Python LLM server (legacy TCP path). The active path is controlled by `constData._tcp`.

## Core Components

### NPCController.cs
**Primary NPC behavior controller** managing state, movement, and interactions.
- **Purpose**: Central coordinator for all NPC behaviors and AI integration
- **State Management**:
  - `NPCState` enum: `Idle`, `Walking`, `Speaking`
  - State-driven behavior with smooth transitions
  - Timer-based pattern cycling for natural movement
- **Technical Details**:
  - GUID-based unique identification for AI connection management
  - Event system for interaction notifications (`OnNPCInteractStart`, `OnNPCInteractEnd`)
  - Integration with `CharacterMove` for shared movement mechanics
  - Collision-aware interaction validation through `InteractManager` components
- **AI Integration**:
  - Automatic AI detection via Unity tags (`NPC_AI`)
  - `npcPersonality` field captures personality string from `LLM_NPCController.generatePersonality()`
  - **llama.cpp path** (`_tcp = false`): calls `UnityLLM.CreateNPCContext(npcID, npcPersonality)` and registers with `UnityLLMContextHasher` on `Start()`; calls `ctx.Close()` on `OnDestroy()`
  - **TCP path** (`_tcp = true`): async TCP connection establishment via `ServerSocketC`; connection stored in `Hasher` by GUID
  - Dynamic dialog generation through `LLM_NPCController`
- **Movement Patterns**:
  - Configurable waypoint-based walking patterns
  - Timer-controlled movement intervals for natural behavior
  - Coroutine-based smooth movement execution
  - State preservation during interactions

### LLM_NPCController.cs
**AI integration controller** managing LLM communication and response generation.
- **Purpose**: Singleton service coordinating AI personality and conversation generation
- **Technical Details**:
  - Dispatches on `constData._tcp` to select inference path
  - Async communication with correct backend
  - Error handling and connection validation
  - Dialog history management for context-aware responses
- **Conversation Management**:
  - **llama.cpp path**: retrieves `NPCContext_intf` from `UnityLLMContextHasher` by NPC GUID; calls `UnityLLM.Instance.talk2LLMWithContext()` — history is natively maintained in the NPC's `ChatSession`
  - **TCP path**: formats request as `Invoke:::prompt:::Context:::history` via `reformatDialog()`; sends via `ServerSocketC.NPCRequest()` using the NPC's `TcpClient` from `Hasher`
  - Personality generation for unique NPC characteristics

### Interactable_intf.cs
**Interaction interface** defining the contract for interactive game objects.
- **Purpose**: Standard interface ensuring consistent interaction behavior across different object types
- **Method Signature**: `Interact(Transform initiator)` provides interaction context
- **Design Pattern**: Interface segregation for clean component interaction
- **Integration**: Implemented by NPCs, items, and other interactive elements

### NpcInit.cs
**Runtime component initialization** for dynamic NPC setup.
- **Purpose**: Programmatic component addition and configuration for NPCs
- **Technical Details**:
  - Rigidbody2D configuration for kinematic physics
  - Collider setup with appropriate dimensions and offsets
  - Interaction zone creation for player proximity detection
  - Multi-directional interaction support (4-directional boxes)
- **Physics Configuration**:
  - Kinematic rigidbody for controlled movement
  - Trigger colliders for interaction detection
  - CapsuleCollider2D for character representation
  - BoxCollider2D arrays for interaction zones
- **Interaction Mechanics**:
  - Child GameObject creation for interaction zones
  - Component attachment and configuration
  - Layer assignment for proper collision detection

## Technical Implementation

### AI NPC Lifecycle
1. **Initialization**: GUID generation and component setup in `Awake()`
2. **AI Detection**: Tag-based AI capability detection (`NPC_AI`)
3. **Personality Generation**: `LLM_NPCController.generatePersonality()` called; result stored in `npcPersonality` and used as system prompt
4. **Context Registration**:
   - `_tcp = false`: `UnityLLM.CreateNPCContext(npcID, npcPersonality)` → `UnityLLMContextHasher.HashNPC()`
   - `_tcp = true`: async TCP connection → `Hasher.HashNPC()`
5. **Conversation Management**: Context-aware dialog generation via `LLM_NPCController.getDialog()`
6. **Cleanup**:
   - `_tcp = false`: `ctx.Close()` in `OnDestroy()`
   - `_tcp = true`: `stopRetrying = true` in `OnDestroy()`

### Interaction System
- **Proximity Detection**: InteractManager components detect player presence
- **Validation Logic**: Multiple interaction zones with logical OR validation
- **Position Adjustment**: Automatic character alignment for conversations
- **State Preservation**: Movement state restoration after interactions

### Movement Patterns
- **Waypoint System**: Vector2 array defining movement patterns
- **Cyclical Behavior**: Pattern repetition with index wrapping
- **Timer-Based Activation**: Configurable delays between movement phases
- **Collision Awareness**: Integration with physics system for obstacle avoidance

### Event-Driven Architecture
- **Interaction Events**: Notify other systems of conversation start/end
- **State Change Notifications**: Enable reactive behavior in other components
- **Loose Coupling**: Event-based communication reduces direct dependencies

## Integration Points

### Character System Integration
- Shares `CharacterMove` and `CharacterAnimator` components with player
- Common movement mechanics ensure consistent physics behavior
- Animation state synchronization during conversations

### Dialog System Integration
- Seamless handoff to `DialogManager` for conversation rendering
- Context preservation for AI conversation continuity
- State coordination between movement and conversation systems

### Network Architecture Integration
- Individual connections per AI NPC through `ServerSocketC`
- Connection pooling and management via `Hasher` system
- Async communication patterns for non-blocking AI responses

### Physics System Integration
- `InteractManager` components for interaction zone detection
- `CollisionDetector` integration for movement validation
- Unity layer system integration for proper collision filtering

## Design Patterns

### State Machine Pattern
NPCs implement explicit state machines with clear state transitions and state-specific behaviors.

### Observer Pattern
Event-driven communication enables reactive behavior without tight coupling between systems.

### Singleton Pattern
`LLM_NPCController` provides global access to AI services while ensuring single instance coordination.

### Component Pattern
Unity's component architecture enables modular NPC construction with shared systems.

### Factory Pattern
`NpcInit` acts as a factory for runtime component creation and configuration.

This NPC system provides a robust foundation for both traditional game characters and modern AI-enhanced NPCs, enabling rich interactive experiences while maintaining clean architecture and extensibility.
