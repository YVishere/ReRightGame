# ServerFiles - Network Communication Layer

This directory contains the core network communication systems that enable Unity-Python integration for AI-powered NPC conversations. The system implements TCP socket communication with connection pooling and async request handling for seamless LLM integration.

## Architecture Overview

The network communication layer provides a bridge between Unity's C# game logic and Python-based LLM services. The system manages multiple concurrent connections, handles async communication patterns, and provides error recovery mechanisms for robust AI integration.

## Core Components

### ServerSocketC.cs
**Unity-side TCP client manager** handling communication with Python LLM services.
- **Purpose**: Singleton network service manager for Unity-Python communication
- **Technical Details**:
  - Singleton pattern for global network service access
  - Python server process lifecycle management
  - TCP client connection pooling for multiple AI NPCs
  - Async communication patterns for non-blocking AI requests
- **Connection Management**:
  - Multiple concurrent TCP connections for individual NPCs
  - Connection establishment with retry logic and timeout handling
  - Process lifecycle management for Python server startup/shutdown
  - Connection validation and error recovery mechanisms
- **Communication Protocol**:
  - String-based message protocol for AI conversation requests
  - UTF-8 encoding for cross-platform text communication
  - Request-response pattern with async/await support
  - Error propagation and exception handling for network failures
- **Server Integration**:
  - Python server process spawning and management
  - Automatic server startup during game initialization
  - Graceful server shutdown on application exit
  - Retry mechanisms for server connection establishment

### ServerSocketPython.py
**Python-side TCP server** managing LLM communication and response generation.
- **Purpose**: Multi-threaded TCP server handling LLM requests from Unity clients
- **Technical Details**:
  - Multi-threaded architecture for concurrent client handling
  - Socket reuse configuration for development iteration
  - Connection pooling with active connection tracking
  - Thread-safe LLM access through mutex synchronization
- **LLM Integration**:
  - Integration with `llamaModelFile` for local LLM inference
  - Thread-safe model access through locking mechanisms
  - Request parsing and context management
  - Response generation and formatting for Unity consumption
- **Connection Handling**:
  - Individual client threads for concurrent request processing
  - Connection lifecycle management (establish, maintain, cleanup)
  - Active connection tracking for resource management
  - Graceful connection termination and cleanup
- **Threading Architecture**:
  - Main server thread for connection acceptance
  - Worker threads for individual client request processing
  - Thread-safe resource sharing through synchronization primitives
  - Error isolation between client connections

## Technical Implementation

### Communication Protocol Design
The system implements a custom text-based protocol:
1. **Connection Establishment**: TCP handshake with connection validation
2. **Request Format**: Structured text messages with conversation context
3. **Processing**: LLM inference with conversation history integration
4. **Response Delivery**: Generated text response with error handling
5. **Connection Maintenance**: Keep-alive patterns for persistent connections

### Async Communication Patterns
- **Unity Side**: Async/await patterns for non-blocking AI requests
- **Python Side**: Multi-threaded request processing for concurrent handling
- **Error Handling**: Exception propagation and retry mechanisms
- **Timeout Management**: Configurable timeouts for request processing

### Connection Pooling Architecture
- **Individual Connections**: Each AI NPC maintains dedicated TCP connection
- **Connection Hashing**: GUID-based connection mapping through `Hasher` system
- **Resource Management**: Proper connection cleanup and resource disposal
- **Scalability**: Support for multiple concurrent AI NPCs

### Process Management
- **Server Lifecycle**: Automatic Python server startup during game initialization
- **Process Monitoring**: Connection health validation and recovery
- **Graceful Shutdown**: Proper resource cleanup on application exit
- **Development Support**: Server restart capabilities for development iteration

## Integration Points

### Game System Integration
- **NPC System**: Direct integration with `NPCController` for AI conversation
- **Dialog System**: Async response integration with `DialogManager`
- **Connection Management**: `Hasher` system for NPC-connection mapping
- **Error Handling**: Graceful degradation for network communication failures

### LLM System Integration
- **Model Access**: Integration with local LLaMA model through Python
- **Context Management**: Conversation history preservation for context-aware responses
- **Response Generation**: Dynamic personality-driven conversation generation
- **Performance Optimization**: Thread-safe model access for concurrent requests

### Unity Engine Integration
- **Coroutine System**: Integration with Unity's async execution model
- **Component Lifecycle**: Network resource management aligned with Unity object lifecycle
- **Error Propagation**: Unity-compatible exception handling and logging
- **Performance Monitoring**: Network performance tracking and optimization

## Design Patterns

### Singleton Pattern
`ServerSocketC` implements singleton pattern for global network service access and connection coordination.

### Observer Pattern
Event-driven communication for connection state changes and request completion notifications.

### Factory Pattern
Connection establishment and client creation through factory methods.

### Thread Pool Pattern
Python server uses thread pooling for efficient concurrent request processing.

### Command Pattern
Network requests encapsulated as commands with async execution and error handling.

## Security and Error Handling

### Connection Security
- **Local Communication**: Localhost-only communication for security
- **Process Isolation**: Separate Python process for LLM operations
- **Resource Limits**: Connection limits and timeout enforcement
- **Error Isolation**: Connection failures isolated from other systems

### Error Recovery
- **Connection Retry**: Automatic retry mechanisms for failed connections
- **Graceful Degradation**: Fallback behavior for network communication failures
- **Resource Cleanup**: Proper cleanup for failed connections and processes
- **Exception Propagation**: Clear error reporting for debugging and monitoring

### Development Support
- **Debug Logging**: Comprehensive logging for development and troubleshooting
- **Connection Monitoring**: Real-time connection status tracking
- **Performance Metrics**: Network performance monitoring and optimization
- **Development Tools**: Utilities for network debugging and testing

## Performance Considerations

### Network Optimization
- **Connection Reuse**: Persistent connections for reduced overhead
- **Async Patterns**: Non-blocking communication for responsive gameplay
- **Connection Pooling**: Efficient resource utilization for multiple NPCs
- **Request Batching**: Optimized request patterns for reduced latency

### Resource Management
- **Memory Management**: Proper cleanup for network resources
- **Thread Management**: Efficient thread utilization for concurrent processing
- **Connection Limits**: Reasonable limits for scalable performance
- **Garbage Collection**: Minimal allocation patterns for performance

This network communication layer provides robust, scalable, and secure integration between Unity game logic and Python-based LLM services, enabling rich AI-powered NPC interactions while maintaining system performance and reliability.
