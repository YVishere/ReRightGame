# Authentication - IPC Security System

This directory contains the authentication system that secures communication between Unity and the Python LLM server using Inter-Process Communication (IPC) via Windows named pipes.

## Overview

The authentication system provides mandatory security for all AI/LLM communications, ensuring that only authenticated Unity game sessions can communicate with the Python server. It uses dynamic secret generation and session-based authentication without any file system dependencies.

## Core Components

### AuthManager.cs
**Primary authentication manager** that handles all security operations for the game session.

#### Key Responsibilities
- **Dynamic Secret Generation**: Creates unique secrets per game session using system identifiers
- **Named Pipe Server**: Manages IPC communication for secure credential exchange
- **Session Key Management**: Generates and manages authentication tokens for requests
- **Token Validation**: Provides methods for validating request and response authenticity
- **Resource Cleanup**: Properly disposes IPC resources on application termination

#### Critical Setup Requirements
- **MUST be attached to GameController GameObject**
- **Initializes before ServerSocketC starts Python server**
- **Required for ALL AI communications** - no fallback mode available

## Technical Implementation

### Dynamic Secret Generation
```csharp
// Combines multiple system identifiers for unique secrets
Process ID + Machine Name + User Name + Timestamp + Unity Instance ID
→ SHA256 Hash → Authentication Secret
```

### Named Pipe Communication
```csharp
Pipe Name: "ReRightAuth_[ProcessID]_[Timestamp]"
Direction: Bidirectional (InOut)
Mode: Message-based transmission
Security: Windows OS-level process isolation
```

### Session Authentication Flow
1. **Game Start**: AuthManager generates dynamic secret and creates named pipe
2. **Server Start**: Python server connects to pipe and retrieves authentication secret
3. **Client Connection**: Unity performs authentication handshake with server
4. **Request Processing**: All requests include session tokens for validation
5. **Response Validation**: Server responses validated before processing

## Security Features

### In-Memory Only Operation
- **No File System Usage**: Authentication secrets never written to disk
- **Process Isolation**: Named pipes provide OS-level security between processes
- **Automatic Cleanup**: All authentication data destroyed on process termination

### Dynamic Session Security
- **Unique Per Session**: New secrets generated for each game session
- **Time-Based Components**: Includes timestamp in secret generation
- **System Fingerprinting**: Uses multiple system identifiers for uniqueness

### Request Authentication
- **Token-Based Validation**: Each AI request includes generated authentication tokens
- **Session Key Integration**: Combines session keys with request data for HMAC generation
- **Hourly Token Rotation**: Tokens include hour-based components for time-limited validity

## Integration Points

### GameController Integration
```csharp
public class GameController : MonoBehaviour 
{
    // AuthManager MUST be attached as component
    // Automatically initialized in Awake()
    // Provides authentication for all AI operations
}
```

### ServerSocketC Integration
- **Automatic Pipe Detection**: Retrieves pipe name from AuthManager instance
- **Mandatory Authentication**: All connections require authentication handshake
- **Token Generation**: Automatically includes authentication tokens in AI requests
- **Response Validation**: Validates all server responses for authenticity

### Python Server Integration
- **IPC Client**: Connects to Unity's authentication pipe on startup
- **Secret Retrieval**: Downloads authentication secret via named pipe
- **Request Validation**: Validates all incoming requests against authentication tokens
- **Client Tracking**: Maintains list of authenticated clients per session

## Error Handling

### Common Issues
- **AuthManager Missing**: Server startup fails if AuthManager not attached to GameController
- **Pipe Connection Failure**: Python server cannot authenticate if pipe unavailable
- **Token Validation Failure**: Requests rejected if authentication tokens invalid

### Debugging
- **Detailed Logging**: Authentication events logged with connection details
- **Pipe Status Monitoring**: Track pipe creation and connection status
- **Token Validation Logs**: Log token generation and validation results

## Development Notes

### Testing Authentication
1. Ensure AuthManager attached to GameController
2. Verify Python server can connect to authentication pipe
3. Monitor Unity console for authentication success messages
4. Check Python server logs for secret retrieval confirmation

### Security Considerations
- Authentication system requires Windows OS (uses win32pipe)
- Named pipes are process-specific and automatically cleaned up
- Secrets are generated with sufficient entropy for session security
- No long-term credential storage or management required

## Files

- **AuthManager.cs**: Main authentication manager component
- **Authentication_Logging.md**: Detailed logging of authentication implementations and changes
