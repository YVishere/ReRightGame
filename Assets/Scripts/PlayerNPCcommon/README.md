# PlayerNPCcommon - Shared Character Systems

This directory contains the core character systems shared between both player and NPC entities. These components provide the fundamental movement, animation, and behavior mechanics that ensure consistent character representation and physics throughout the game.

## Architecture Overview

The shared character system implements reusable components that abstract common character behaviors from specific entity types. This architecture enables consistent physics, animation, and movement mechanics while allowing entity-specific controllers to focus on their unique logic.

## Core Components

### CharacterMove.cs
**Universal movement controller** providing physics-based character movement for all entities.
- **Purpose**: Centralized movement system with collision detection and smooth interpolation
- **Technical Details**:
  - Coroutine-based movement execution for frame-rate independent motion
  - Integration with Unity's physics system for collision validation
  - Movement state management with blocking/non-blocking modes
  - Event-driven movement pause/resume for interaction handling
- **Movement Mechanics**:
  - Vector2-based directional movement with configurable speed
  - Target position calculation with smooth interpolation
  - Collision detection integration with `CollisionDetector`
  - Path validation for player-specific movement rules
- **State Management**:
  - `moving` flag for animation and input coordination
  - `stopMoveFlag` for interaction-based movement pausing
  - Old movement state preservation for seamless resume
  - Direction tracking for animation state synchronization
- **Integration Features**:
  - NPC event subscription for interaction-based movement control
  - Player-specific path validation for collision avoidance
  - Animation system coordination through movement state updates

### CharacterAnimator.cs
**Sprite-based animation controller** managing directional character animations.
- **Purpose**: 4-directional sprite animation system with state-driven animation selection
- **Technical Details**:
  - Sprite list management for each movement direction (up, down, left, right)
  - `SpriteAnimator` wrapper for individual animation sequences
  - Movement parameter tracking (`MoveX`, `MoveY`, `IsMoving`)
  - Animation state transition handling with previous animation preservation
- **Animation System**:
  - Direction-based animation selection using movement vectors
  - Smooth animation transitions between movement states
  - Frame-based sprite animation with configurable timing
  - Idle state handling with last movement direction preservation
- **State Coordination**:
  - Movement vector tracking for animation direction selection
  - Moving state integration for animation activation/deactivation
  - Previous animation state tracking for smooth transitions
  - SpriteRenderer integration for visual output

### SpriteAnimator.cs
**Individual sprite sequence animator** handling frame-by-frame animation playback.
- **Purpose**: Low-level sprite animation engine for smooth frame transitions
- **Technical Details**:
  - Sprite list iteration with configurable frame timing
  - `SpriteRenderer` integration for visual output
  - Frame advance logic with automatic looping
  - Animation state management (playing/stopped)
- **Animation Features**:
  - Frame-by-frame sprite sequence playback
  - Automatic animation looping for continuous movement
  - Configurable animation speed and timing
  - Direct sprite renderer manipulation for performance

## Technical Implementation

### Movement System Architecture
The movement system implements a layered approach:
1. **Input Layer**: Movement vectors from controllers (player input or NPC AI)
2. **Validation Layer**: Collision detection and path validation
3. **Execution Layer**: Smooth interpolation to target positions
4. **State Layer**: Movement state coordination with other systems

### Animation State Machine
The animation system operates as a state machine:
- **Direction States**: Up, Down, Left, Right movement animations
- **Movement States**: Moving vs Idle animation selection
- **Transition Handling**: Smooth animation changes between states
- **State Persistence**: Last direction preservation for idle states

### Physics Integration
- **Collision Detection**: Integration with Unity's Physics2D system
- **Layer Filtering**: Collision layer management through `GameLayers`
- **Path Validation**: Movement validation before execution
- **Collision Response**: Movement blocking and alternative path calculation

### Event-Driven Architecture
- **NPC Integration**: Event subscription for interaction-based movement control
- **State Notifications**: Movement state changes notify dependent systems
- **Loose Coupling**: Event-based communication reduces direct dependencies

## Integration Points

### Controller Integration
- **Player Controller**: Input-driven movement with collision validation
- **NPC Controller**: AI-driven movement with pattern-based behavior
- **Shared Interface**: Consistent movement API across entity types

### Physics System Integration
- **Collision Detection**: `CollisionDetector` component integration
- **Layer Management**: `GameLayers` system for collision filtering
- **Movement Validation**: Physics queries for path validation

### Animation System Integration
- **Sprite Management**: `SpriteRenderer` component coordination
- **State Synchronization**: Movement state drives animation selection
- **Visual Feedback**: Animation provides visual representation of movement state

### Interaction System Integration
- **Movement Pausing**: Interaction events pause/resume movement
- **State Preservation**: Movement state maintained during interactions
- **Smooth Transitions**: Seamless movement resume after interactions

## Design Patterns

### Component Pattern
Reusable components that can be attached to any character entity, providing consistent behavior across different character types.

### State Machine Pattern
Animation system implements explicit state management with clear state transitions based on movement parameters.

### Observer Pattern
Event-driven communication enables reactive behavior without tight coupling between movement and other systems.

### Template Method Pattern
`CharacterMove` provides movement template while allowing entity-specific behavior through parameter configuration.

### Facade Pattern
`CharacterAnimator` provides simplified interface to complex sprite animation system.

## Performance Considerations

### Movement Optimization
- **Coroutine-Based**: Efficient frame-rate independent movement
- **Early Termination**: Movement validation prevents unnecessary physics calculations
- **State Caching**: Movement state preservation reduces computation overhead

### Animation Optimization
- **Sprite Reuse**: Shared sprite resources across multiple characters
- **State-Driven Updates**: Animation updates only when state changes
- **Direct Renderer Access**: Efficient sprite updates through direct component access

### Memory Management
- **Component Reuse**: Shared components reduce memory footprint
- **State Preservation**: Minimal state storage for movement and animation
- **Resource Sharing**: Sprite lists shared between character instances

This shared character system provides the foundation for consistent and efficient character behavior while maintaining the flexibility needed for diverse entity types in the game world.
