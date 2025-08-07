# UnityEngineHelper - Unity Editor Integration Utilities

This directory contains utility classes and systems that enhance Unity Editor functionality and provide development-time support for the ReRightGame project. These utilities focus on editor integration, domain reload management, and development workflow improvements.

## Architecture Overview

The UnityEngineHelper system provides editor-specific functionality that enhances the development experience and ensures proper resource management during Unity's domain reload cycles. These utilities are editor-only and do not affect runtime builds.

## Core Components

### DomainReloadHelper.cs
**Unity Editor domain reload management utility** ensuring proper cleanup during assembly reloads.
- **Purpose**: Manages resource cleanup during Unity's domain reload process to prevent hanging and resource leaks
- **Technical Details**:
  - `[InitializeOnLoad]` attribute for automatic registration during domain initialization
  - Static event registration for `AssemblyReloadEvents.beforeAssemblyReload`
  - Forced cleanup of singleton instances before domain reload
  - Editor menu integration for manual cleanup testing
- **Cleanup Coordination**:
  - AuthManager IPC resource cleanup via `ForceCleanupAllInstances()`
  - Hasher connection cleanup via message broadcasting
  - ServerSocketC network resource cleanup
  - Ordered cleanup sequence to prevent dependencies
- **Developer Tools**:
  - Menu item: "Tools/Force Cleanup Before Domain Reload"
  - Comprehensive logging for debugging cleanup issues
  - Manual cleanup testing capabilities

## Technical Implementation

### Domain Reload Event Handling
Unity's domain reload system can interrupt normal cleanup processes, leading to resource leaks and hanging operations. The DomainReloadHelper addresses this by:

#### Proactive Cleanup Registration
- **Early Registration**: Uses `[InitializeOnLoad]` to register cleanup handlers immediately
- **Event Subscription**: Subscribes to `beforeAssemblyReload` for earliest possible notification
- **Cleanup Coordination**: Orchestrates cleanup across multiple singleton systems

#### Resource Management
- **IPC Resources**: Named pipes and authentication tokens require explicit cleanup
- **Network Resources**: TCP connections and socket handles need proper disposal
- **Background Tasks**: Async operations and background threads must be cancelled

#### Error Prevention
- **Hanging Prevention**: Prevents domain reload from hanging on unclosed resources
- **Resource Leaks**: Ensures proper disposal of unmanaged resources
- **State Consistency**: Maintains consistent state across domain reloads

### Integration Points

#### Authentication System Integration
- Coordinates with AuthManager for IPC pipe cleanup
- Ensures background authentication tasks are cancelled
- Prevents pipe handle leaks during domain transitions

#### Network System Integration
- Triggers cleanup of all active TCP connections
- Ensures Python server processes are terminated
- Prevents socket handle accumulation

#### Development Workflow Support
- Provides manual cleanup testing via editor menu
- Enables rapid iteration without resource accumulation
- Supports debugging of cleanup sequences

## Design Patterns

### Static Initialization Pattern
DomainReloadHelper uses Unity's `[InitializeOnLoad]` pattern for automatic registration without requiring scene objects or manual setup.

### Event-Driven Cleanup
The system responds to Unity's domain reload events rather than relying on object lifecycle methods, ensuring cleanup happens even when normal destruction doesn't occur.

### Defensive Resource Management
Multiple cleanup strategies ensure resources are freed even if individual cleanup methods fail or are interrupted.

## Editor-Only Design

### Conditional Compilation
All functionality is wrapped in `#if UNITY_EDITOR` directives to ensure no editor code appears in builds:
- Zero runtime overhead in builds
- Editor-specific API usage without build errors
- Development-time functionality isolation

### Development Tools Integration
- Unity Editor menu integration for manual testing
- Comprehensive logging for development debugging
- Integration with Unity's assembly reload system

## Dependencies and Integration

### Unity Editor Dependencies
- UnityEditor assembly for editor-specific functionality
- AssemblyReloadEvents for domain reload notifications
- MenuItem attribute for editor menu integration

### Project System Dependencies
- AuthManager singleton for IPC cleanup coordination
- Hasher singleton for connection management cleanup
- ServerSocketC singleton for network resource cleanup

### Development Workflow Dependencies
- Domain reload events for automatic cleanup triggering
- Unity's logging system for development feedback
- Editor play mode for testing and validation

This utility system ensures smooth development experience by preventing the resource management issues that can cause Unity Editor to hang or accumulate leaked resources during rapid development iteration.
