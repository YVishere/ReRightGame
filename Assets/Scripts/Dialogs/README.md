# Dialogs - Conversation Management System

This directory contains the core conversation system that handles both scripted and AI-generated dialogs. The system provides a unified interface for managing character conversations while supporting seamless integration with LLM-powered responses.

## Architecture Overview

The dialog system operates as a centralized conversation manager that coordinates between UI presentation, input handling, and backend AI services. It maintains conversation state and context while providing smooth transitions between different dialog modes.

## Core Components

### Dialog.cs
**Data structure for conversation content** with dynamic manipulation capabilities.
- **Purpose**: Serializable container for conversation lines with runtime modification support
- **Technical Details**:
  - `List<string>` based storage for conversation lines
  - Support for prepend, append, and replace operations
  - Empty state validation for conversation flow control
  - First-line access for immediate dialog display
- **Integration**: Used by both scripted NPCs and AI-generated conversations
- **Design Pattern**: Data Transfer Object (DTO) with collection manipulation methods

### DialogManager.cs
**Central conversation controller** managing UI state and conversation flow.
- **Purpose**: Singleton manager coordinating all conversation interactions
- **Technical Details**:
  - Event-driven architecture with `OnShowDialog` and `OnCloseDialog` events
  - State management for typing animation and user input phases
  - Dual-mode operation: scripted dialog progression vs AI conversation handling
  - Integration with Unity UI components (TextMeshPro, InputField)
  - Coroutine-based typing animation with configurable speed
- **State Management**:
  - `isShowing`: Global dialog visibility state
  - `isTyping`: Prevents input during text animation
  - `currentLine`: Tracks progression through scripted dialogs
  - `isAI`: Mode flag determining interaction behavior
- **Input Handling**:
  - E key / Enter key for dialog progression
  - Return key for AI conversation submission
  - Text input field management for AI interactions

## Technical Implementation

### Conversation Flow Management
The system supports two distinct conversation modes:

#### Scripted Dialog Mode
- Linear progression through predefined conversation lines
- Automatic line advancement with user input
- Traditional RPG-style conversation experience
- Immediate dialog completion when reaching end

#### AI Conversation Mode
- Dynamic response generation through LLM integration
- User text input for natural conversation
- Context preservation across conversation turns
- Async response handling with visual feedback

### Event System Integration
The DialogManager integrates with the broader game architecture through events:
- **`OnShowDialog`**: Triggered when conversation begins, notifies GameController to switch to dialog mode
- **`OnCloseDialog`**: Triggered when conversation ends, returns control to free roam mode
- **NPC Events**: Listens to NPC interaction events for conversation initiation

### UI Component Management
- **Dialog Box**: GameObject activation/deactivation for conversation visibility
- **Text Display**: TextMeshPro component for rendered conversation text
- **Input Field**: TextMeshPro InputField for AI conversation user input
- **Typing Animation**: Character-by-character text revelation with configurable speed

### Async Operation Handling
- Coroutine-based typing animations for smooth text presentation
- Async AI response integration without blocking game loop
- State preservation during async operations
- Error handling for network communication failures

## Integration Points

### Game State Management
- Coordinates with `GameController` for game state transitions
- Manages UI layer priority during conversations
- Handles input routing between game systems

### AI System Integration
- Interfaces with `LLM_NPCController` for AI response generation
- Manages conversation context for LLM prompting
- Handles network communication errors gracefully

### Character System Integration
- Receives interaction events from NPC controllers
- Coordinates character facing direction during conversations
- Manages character animation state during dialogs

## Design Patterns

### Singleton Pattern
DialogManager implements singleton pattern for global conversation management and preventing multiple simultaneous dialogs.

### Observer Pattern
Event-driven communication with other game systems enables loose coupling and reactive state management.

### State Machine
Internal state management for conversation phases (showing, typing, waiting for input, AI processing).

### Command Pattern
Dialog operations (show, hide, advance) encapsulated as discrete operations with state validation.

This dialog system provides the foundation for rich character interactions while maintaining flexibility for both traditional scripted conversations and modern AI-powered dynamic dialogs.
