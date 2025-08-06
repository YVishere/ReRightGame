# Player - Player Character System

This directory contains the player character controller and initialization systems that handle user input, movement, and world interaction. The player system serves as the primary interface between user input and the game world.

## Architecture Overview

The player system implements a component-based architecture where the player character shares common movement and animation systems with NPCs while providing unique input handling and interaction capabilities. The system processes user input, validates movement through the physics system, and initiates interactions with the game world.

## Core Components

### PlayerController.cs
**Primary player input and behavior controller** managing user input translation to game actions.
- **Purpose**: Central coordinator for all player actions and world interactions
- **Input Processing**:
  - Raw input capture using Unity's Input system
  - Horizontal/Vertical axis input with digital movement
  - Diagonal movement prevention for grid-based movement
  - E key interaction trigger
- **Technical Details**:
  - Integration with shared `CharacterMove` component
  - Movement validation through physics system
  - Interaction detection using Unity's collision system
  - Component dependency management through `PlayerInit`
- **Movement Mechanics**:
  - Digital movement with normalized input vectors
  - Coroutine-based smooth movement execution
  - Movement blocking during active movement
  - Physics-based collision detection and response
- **Interaction System**:
  - Facing direction calculation for interaction targeting
  - Circular overlap detection for interaction range
  - Interface-based interaction with game objects
  - Layer-based filtering for interaction validation

### PlayerInit.cs
**Runtime component initialization** for dynamic player setup.
- **Purpose**: Programmatic component addition and configuration for player character
- **Technical Details**:
  - Rigidbody2D configuration for kinematic physics
  - Collider setup with standardized dimensions
  - Interaction point creation for world interaction
  - Component initialization order management
- **Physics Configuration**:
  - Kinematic rigidbody for controlled movement
  - CapsuleCollider2D for character representation
  - Trigger-based collision detection
  - Collision detection mode optimization
- **Interaction Point Setup**:
  - Child GameObject creation for interaction targeting
  - BoxCollider2D configuration for interaction detection
  - Proper tag assignment for interaction system integration
  - Layer assignment for collision filtering

## Technical Implementation

### Input Processing Pipeline
1. **Raw Input Capture**: Unity Input system integration for cross-platform input
2. **Movement Vector Construction**: Axis input combination with diagonal prevention
3. **Movement Validation**: Physics system consultation for valid movement
4. **Movement Execution**: Coroutine-based smooth movement with collision response
5. **Interaction Processing**: E key detection with facing direction calculation

### Movement System Integration
- **Shared Components**: Uses common `CharacterMove` system with NPCs
- **Movement Validation**: Physics-based collision detection before movement execution
- **Animation Coordination**: Movement state synchronization with animation system
- **State Management**: Movement blocking during active movement or interactions

### Interaction Mechanics
- **Facing Direction Calculation**: Uses animator state to determine interaction direction
- **Interaction Range**: Circular overlap detection with configurable radius
- **Target Validation**: Layer mask filtering for valid interaction targets
- **Interface Invocation**: Polymorphic interaction through `Interactable_intf`

### Physics Integration
- **Layer Utilization**: Integration with `GameLayers` for proper collision filtering
- **Collision Detection**: OverlapCircle for interaction range detection
- **Movement Validation**: Physics queries before movement execution
- **Trigger Systems**: Interaction point collision detection for proximity awareness

## Integration Points

### Character System Integration
- Shares `CharacterMove` and `CharacterAnimator` components with NPC system
- Common movement mechanics ensure consistent physics behavior across characters
- Unified animation state management for directional movement

### Input System Integration
- Unity Input System integration for cross-platform input handling
- Configurable input mapping through Unity's input action system
- Real-time input processing with frame-rate independent movement

### Physics System Integration
- `GameLayers` integration for collision layer management
- Collision detection for both movement validation and interaction targeting
- Physics-based interaction range detection with layer filtering

### Dialog System Integration
- Interaction initiation triggers dialog system activation
- State coordination during conversation mode
- Input routing between movement and dialog systems

### World Interaction Integration
- Interface-based interaction with NPCs, objects, and other interactive elements
- Position-based interaction targeting with facing direction awareness
- Movement state management during interactions

## Design Patterns

### Component Pattern
Unity's component architecture enables modular player construction with shared systems and clear separation of concerns.

### Strategy Pattern
Input processing strategies can be swapped for different input methods or control schemes.

### Command Pattern
Player actions (movement, interaction) are encapsulated as discrete operations with validation.

### Observer Pattern
Component communication through Unity's event system and component references.

### Factory Pattern
`PlayerInit` acts as a factory for runtime component creation and configuration.

## Input Handling Architecture

### Movement Input
- **Digital Movement**: Binary input processing for grid-based movement
- **Diagonal Prevention**: Movement constraint logic for 4-directional movement
- **Input Buffering**: Coroutine-based movement prevents input spam
- **Physics Validation**: Movement validation through collision detection

### Interaction Input
- **Key Binding**: E key for interaction trigger
- **Context Sensitivity**: Interaction availability based on proximity and facing direction
- **Target Validation**: Interface checking for valid interaction targets
- **State Coordination**: Interaction state management with other systems

This player system provides responsive and intuitive character control while maintaining consistency with the broader character system architecture and enabling rich world interaction capabilities.
