# Physics - Collision and Interaction Systems

This directory contains the physics and collision systems that manage character interactions, movement validation, and proximity detection. These components provide the foundation for spatial awareness and interaction mechanics throughout the game world.

## Architecture Overview

The physics system implements a component-based collision detection architecture that handles both movement validation and interaction zone management. The system uses Unity's 2D physics engine with custom logic for character-specific collision behaviors and interaction detection.

## Core Components

### CollisionDetector.cs
**Character collision detection system** managing movement validation and obstacle avoidance.
- **Purpose**: Real-time collision detection for character movement with directional awareness
- **Technical Details**:
  - Trigger-based collision detection for dynamic response
  - Integration with `CharacterAnimator` for movement direction awareness
  - BoxCast physics queries for predictive collision detection
  - Layer-based collision filtering through `GameLayers` integration
- **Collision Logic**:
  - `collided` state flag for movement blocking
  - Directional collision checking based on character facing direction
  - Predictive collision detection using BoxCast with movement vector
  - Multi-layer collision detection (SolidObjects, InteractableLayer, PlayerLayer)
- **NPC-Specific Behavior**:
  - Tag-based activation for NPC entities (`NPC`, `NPC_AI`)
  - Movement direction calculation from animator component
  - Collision state management for movement coordination
- **Technical Implementation**:
  - OnTriggerEnter2D for collision initiation
  - OnTriggerExit2D for collision resolution
  - BoxCast with configurable dimensions for accurate collision detection
  - Layer mask integration for selective collision detection

### InteractManager.cs
**Interaction zone detection system** managing player proximity and interaction availability.
- **Purpose**: Proximity-based interaction validation for player-world interactions
- **Technical Details**:
  - Trigger-based detection using Unity's 2D collision system
  - Player-specific interaction validation through tag checking
  - Boolean state management for interaction availability
  - Component-based interaction zone definition
- **Interaction Logic**:
  - `canInteract` property for interaction state queries
  - Player interaction point detection (`Player_Interaction_Point` tag)
  - Automatic interaction state management
  - Integration with NPC interaction validation systems
- **State Management**:
  - OnTriggerEnter2D for interaction zone entry
  - Default false state for interaction availability
  - Immediate state updates for responsive interaction feedback
- **Integration Features**:
  - Multiple InteractManager support per NPC for complex interaction zones
  - Boolean OR logic for multi-zone interaction validation
  - Component querying for interaction availability checks

## Technical Implementation

### Collision Detection Architecture
The collision system implements a layered detection approach:
1. **Trigger Detection**: Initial collision detection through Unity's trigger system
2. **Direction Analysis**: Movement direction calculation from animator state
3. **Predictive Validation**: BoxCast queries for movement path validation
4. **Layer Filtering**: Multi-layer collision detection with configurable filters

### Interaction Zone Management
The interaction system uses proximity-based validation:
1. **Zone Definition**: Trigger colliders define interaction boundaries
2. **Player Detection**: Tag-based validation for player interaction points
3. **State Management**: Boolean flags for interaction availability
4. **Multi-Zone Support**: Multiple interaction managers per entity

### Physics Integration
- **Unity Physics 2D**: Built on Unity's robust 2D physics engine
- **Layer System**: Integration with `GameLayers` for collision filtering
- **Trigger System**: Efficient event-driven collision detection
- **BoxCast Queries**: Predictive collision detection for movement validation

### Performance Optimization
- **Trigger-Based Detection**: Efficient collision detection without continuous polling
- **State Caching**: Collision states cached for performance
- **Layer Filtering**: Selective collision detection reduces computation overhead
- **Early Termination**: Collision validation prevents unnecessary movement calculations

## Integration Points

### Character System Integration
- **Movement Validation**: `CollisionDetector` prevents invalid character movement
- **Animation Coordination**: Movement direction from animation system drives collision detection
- **State Synchronization**: Collision states influence character movement behavior

### NPC System Integration
- **Interaction Zones**: `InteractManager` components define NPC interaction boundaries
- **Multi-Zone Validation**: Multiple interaction managers enable complex interaction areas
- **Tag-Based Filtering**: NPC-specific collision behavior through Unity tags

### Player System Integration
- **Interaction Detection**: Player interaction points trigger interaction zone validation
- **Movement Blocking**: Collision detection prevents invalid player movement
- **Proximity Awareness**: Interaction zones provide spatial awareness for user interface

### Game Layer Integration
- **Layer Management**: `GameLayers` system provides collision layer configuration
- **Selective Collision**: Layer masks enable specific collision types
- **Physics Filtering**: Efficient collision detection through layer-based filtering

## Design Patterns

### Component Pattern
Physics components can be attached to any game object, providing modular collision and interaction behavior.

### Observer Pattern
Trigger-based collision detection provides event-driven collision response without continuous polling.

### State Pattern
Collision and interaction states drive behavior changes in character and interaction systems.

### Strategy Pattern
Different collision behaviors for different entity types through component configuration.

### Facade Pattern
Simplified collision and interaction interfaces hide complex physics calculations.

## Physics Configuration

### Collision Detection Settings
- **Trigger Colliders**: Event-driven collision detection
- **Layer Masks**: Selective collision detection through layer filtering
- **BoxCast Parameters**: Configurable collision detection dimensions
- **Detection Timing**: Frame-accurate collision detection and response

### Interaction Zone Configuration
- **Proximity Bounds**: Configurable interaction zone dimensions
- **Tag Validation**: Player-specific interaction detection
- **State Management**: Automatic interaction availability tracking
- **Multi-Zone Support**: Complex interaction areas through multiple components

### Performance Tuning
- **Collision Layers**: Optimized layer assignment for efficient detection
- **Query Optimization**: Minimal physics queries for maximum performance
- **State Caching**: Collision state persistence reduces computation overhead
- **Early Exit**: Collision validation prevents unnecessary calculations

## Error Handling and Edge Cases

### Collision Edge Cases
- **Rapid Movement**: High-speed movement validation through predictive collision detection
- **Corner Cases**: BoxCast collision detection handles complex geometry
- **Multi-Entity Collision**: Layer-based filtering prevents collision conflicts

### Interaction Edge Cases
- **Zone Boundaries**: Precise trigger boundaries for consistent interaction behavior
- **Simultaneous Interactions**: Multi-zone validation for complex interaction scenarios
- **State Synchronization**: Consistent interaction state across frame boundaries

This physics system provides robust collision detection and interaction management while maintaining high performance and flexibility for diverse gameplay scenarios.
