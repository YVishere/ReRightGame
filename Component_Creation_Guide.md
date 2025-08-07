# Component Creation Guide - ReRightGame

This guide provides comprehensive instructions for creating and configuring game objects in the ReRightGame Unity project. Follow these specifications to ensure proper integration with the game's systems.

## Unity Layers and Tags Configuration

### Layers
Configure these layers in Unity's Layer settings:
- **Player**: For the player character
- **InteractableLayer**: For NPCs and interactive objects
- **SolidObjects**: For walls, obstacles, and collision objects

### Tags
Configure these tags in Unity's Tag Manager:
- **Player**: Player character identification
- **NPC**: Standard scripted NPCs
- **NPC_AI**: AI-powered NPCs with LLM integration
- **Player_Interaction_Point**: Player's interaction detection point
- **Interactable**: General interactable objects
- **GameController**: Main game controller object

## Component Creation Specifications

### 🤖 NPC Character Setup

#### Basic Configuration
- **Tag**: `NPC` (for scripted NPCs) or `NPC_AI` (for AI-powered NPCs)
- **Layer**: `InteractableLayer`

#### Required Scripts
1. **NpcInit** - *Runtime component initialization*
   - Automatically creates Rigidbody2D (Kinematic)
   - Sets up CapsuleCollider2D for character hitbox
   - Creates interaction zones for player detection
   - Configures physics properties

2. **NPCController** - *Main NPC behavior controller*
   - Manages NPC states (Idle, Walking, Speaking)
   - Handles interaction logic and dialog triggering
   - Coordinates with movement and animation systems
   - Implements `Interactable_intf` interface

3. **CharacterAnimator** - *Sprite-based animation system*
   - Manages 4-directional movement animations
   - Requires sprite lists for each direction (up, down, left, right)
   - Coordinates with movement system for animation state

4. **CharacterMove** - *Universal movement controller*
   - Handles smooth grid-based movement
   - Integrates with collision detection system
   - Manages movement state and coordination

5. **CollisionDetector** - *Movement collision detection*
   - Prevents NPCs from walking into obstacles or other characters
   - Manages collision state for movement coordination
   - Essential for proper NPC pathfinding and movement validation
   - Works with CharacterMove to ensure smooth navigation

6. **LLM_NPCController** - *AI integration controller* (**NPC_AI only**)
   - Required only for NPCs with `NPC_AI` tag
   - Manages LLM communication and response generation
   - Not needed for standard scripted NPCs with `NPC` tag

#### NPC Setup Process
1. Create new GameObject
2. Set tag to `NPC` or `NPC_AI` based on requirements
3. Set layer to `InteractableLayer`
4. Add all required scripts listed above
5. Configure sprite animations in CharacterAnimator
6. Set up walking patterns in NPCController (optional)
7. Assign dialog content for conversations

### 👤 Player Character Setup

#### Basic Configuration
- **Tag**: `Player`
- **Layer**: `Player`

#### Required Scripts
1. **PlayerInit** - *Runtime component initialization*
   - Creates Rigidbody2D (Kinematic) for player physics
   - Sets up CapsuleCollider2D for character representation
   - Creates interaction point child object for world interaction
   - Configures collision detection and interaction zones

2. **PlayerController** - *Input handling and game interaction*
   - Processes user input (WASD/Arrow keys for movement, E for interaction)
   - Manages movement validation and execution
   - Handles interaction detection and triggering
   - Coordinates with dialog system for conversations

3. **CharacterMove** - *Shared movement system*
   - Same movement controller used by NPCs for consistency
   - Handles smooth interpolation and collision detection
   - Manages movement state and physics integration

4. **CharacterAnimator** - *Sprite-based animation*
   - Same animation system as NPCs
   - Requires directional sprite configuration
   - Provides visual feedback for movement direction

#### Player Setup Process
1. Create new GameObject
2. Set tag to `Player`
3. Set layer to `Player`
4. Add all required scripts
5. Configure sprite animations for all movement directions
6. Ensure proper input configuration in Unity Input settings

### 🎮 Game Controller Setup

#### Basic Configuration
- **Tag**: `GameController`
- **Layer**: `Default`

#### Required Scripts
1. **GameLayers** - *Layer management system*
   - Singleton providing global access to Unity layers
   - Configures layer masks for collision detection
   - Essential for physics system integration

2. **GameController** - *Main game state manager*
   - Manages game states (freeRoam vs dialogMode)
   - Coordinates between player and dialog systems
   - Handles state transitions and input routing

3. **AuthManager** - *IPC authentication system* **[CRITICAL REQUIREMENT]**
   - Manages secure communication between Unity and Python server
   - Generates dynamic secrets and authentication tokens
   - Provides IPC-based credential exchange via named pipes
   - **MUST be attached** - AI communication will fail without this component

4. **DialogManager** - *Conversation system controller*
   - Singleton managing all dialog interactions
   - Handles UI presentation and user input
   - Integrates with both scripted and AI conversations

5. **ServerSocketC** - *Network communication manager*
   - Singleton managing Python server communication
   - Handles TCP connections for AI NPCs with mandatory authentication
   - Manages server lifecycle and connection pooling
   - **Depends on AuthManager** for all AI communications

6. **Hasher** - *Connection management system*
   - Maps NPC GUIDs to network connections
   - Manages connection lifecycle and cleanup
   - Essential for multi-NPC AI communication

7. **Killports** - *Development utility*
   - Network debugging and cleanup utility
   - Prevents port conflicts during development
   - Development-specific tooling

#### Game Controller Setup Process
1. Create new GameObject named "GameController"
2. Set tag to `GameController`
3. Set layer to `Default`
4. Add all required scripts (including **AuthManager - CRITICAL**)
5. **Verify AuthManager is attached** - Check component is present before proceeding
6. Configure references in GameController script (PlayerController, Camera)
7. Set up UI references in DialogManager (dialog box, text components)
8. Configure layer masks in GameLayers component
9. **Test authentication setup** - Verify AuthManager initializes before ServerSocketC

#### ⚠️ Critical Authentication Requirements
- **AuthManager MUST be attached to GameController** before any AI operations
- Game will fail to start Python server without proper AuthManager setup
- No fallback mode available - authentication is mandatory for all AI communications
- Initialization order: AuthManager → ServerSocketC → Python server startup

## Component Descriptions and Purposes

### Core Character Components

#### CharacterMove
**Purpose**: Universal movement system shared by player and NPCs
- Grid-based movement with smooth interpolation
- Collision detection and validation
- Integration with animation and physics systems
- Movement state management for other systems

#### CharacterAnimator  
**Purpose**: Sprite-based 4-directional animation system
- Manages sprite sequences for each movement direction
- Coordinates with movement system for state synchronization
- Provides visual feedback for character direction and movement

#### CollisionDetector
**Purpose**: Movement collision detection system for NPCs
- Prevents NPCs from walking into obstacles, walls, or other characters
- Manages collision state flags for movement coordination
- Integrates with CharacterMove to validate movement paths
- Essential for proper NPC navigation and pathfinding behavior

### NPC-Specific Components

#### NPCController
**Purpose**: Main NPC behavior coordinator
- State machine managing NPC behavior (Idle/Walking/Speaking)
- Interaction detection and dialog triggering
- Integration with AI systems for dynamic conversations
- Event system for coordinating with other systems

#### LLM_NPCController (AI NPCs only)
**Purpose**: AI integration and conversation management
- Singleton service for LLM communication
- Context-aware conversation generation
- Network communication with Python LLM server
- Personality generation and dialog formatting

#### NpcInit
**Purpose**: Runtime component initialization for NPCs
- Programmatic creation of physics components
- Interaction zone setup and configuration
- Collision detection setup for proper game integration

### Player-Specific Components

#### PlayerController
**Purpose**: User input processing and world interaction
- Input handling for movement and interaction
- Physics-based movement validation
- Interaction detection and system coordination
- Integration with dialog system for conversations

#### PlayerInit
**Purpose**: Runtime component setup for player character
- Physics component creation and configuration
- Interaction point setup for world interaction
- Collision detection configuration

### System Components

#### GameController
**Purpose**: Central game state management
- Game state coordination (exploration vs conversation)
- Input routing between different systems
- State transition management

#### DialogManager
**Purpose**: Conversation system coordination
- UI management for dialog presentation
- Integration between scripted and AI conversations
- User input handling for conversation progression

#### GameLayers
**Purpose**: Unity layer management
- Centralized access to collision layers
- Physics query configuration
- Layer mask management for different systems

## Integration Guidelines

### Physics Integration
- All characters must be on appropriate layers for collision detection
- Interaction zones require proper tag configuration
- Movement validation depends on correct layer assignment

### AI Integration
- NPCs with `NPC_AI` tag automatically establish server connections
- LLM_NPCController manages conversation context and personality
- Network communication requires proper server setup

### Dialog Integration
- All interactive objects must implement `Interactable_intf`
- Dialog system coordinates with game state management
- Both scripted and AI conversations use the same interface

### Performance Considerations
- Shared components reduce memory overhead
- Singleton patterns provide efficient global access
- Event-driven architecture minimizes coupling

## Common Setup Issues

### Missing Components
- Ensure all required scripts are attached
- Verify proper tag and layer assignment
- Check script execution order dependencies

### Physics Problems
- Verify layer configuration matches collision detection
- Ensure proper collider setup and sizing
- Check that GameLayers component is configured

### AI Integration Issues
- Confirm NPC_AI tag for AI-powered characters
- Verify server connection establishment
- Check LLM_NPCController singleton initialization

This guide ensures proper component setup for seamless integration with the ReRightGame's sophisticated character and AI systems.
