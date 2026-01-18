# UnityAIScripts - Local LLM Integration System

This directory contains the local Large Language Model (LLM) integration system using LLamaSharp for AI-powered NPC conversations. The system implements a memory-efficient architecture with a single shared model instance and per-NPC context management through GUID-based hashing.

## Architecture Overview

The UnityAIScripts system provides a complete solution for integrating local LLM inference into Unity, replacing or complementing the network-based approach from ServerFiles. The architecture is designed around three key principles:

1. **Single Model Instance**: One `LLamaWeights` instance loaded in memory (1-5GB) shared across all NPCs
2. **Per-NPC Context Management**: Individual conversation histories and executors for each AI NPC
3. **GUID-Based Context Hashing**: Efficient context lookup and lifecycle management through Unity GUIDs

This design minimizes memory overhead while maintaining independent conversation contexts for multiple NPCs simultaneously, enabling rich AI interactions without network latency or external dependencies.

## Core Components

### UnityLLM.cs
**Singleton model manager and single point of truth for LLM resources**
- **Purpose**: Loads and manages the shared LLamaSharp model instance for all AI NPCs
- **Model Configuration**:
  - Model path: `Llama-3.2-1B-Instruct-Q4_K_M.gguf` (quantized 4-bit model)
  - Context size: 1024 tokens for conversation memory
  - GPU acceleration: 5 layers offloaded to GPU (configurable based on VRAM)
  - Model format: GGUF format from Unsloth optimized for inference
- **Technical Details**:
  - Static model instance (`LLamaWeights`) loaded once at initialization
  - Singleton pattern for global LLM service access
  - Async initialization in `Awake()` for non-blocking model loading
  - Default context creation for testing/demonstration purposes
- **Initialization Process**:
  - Model file loaded from StreamingAssets at startup
  - Model parameters configured (context size, GPU layers)
  - Test conversation executed to validate model functionality
  - Instance reference stored for global access
- **Memory Management**:
  - Single model instance reduces RAM usage (vs per-NPC models)
  - Model remains loaded for application lifetime
  - Context creation on-demand for each NPC
  - Shared model weights across all inference operations

### UnityLLMContextHasher.cs
**Context lifecycle manager with GUID-based NPC context hashing**
- **Purpose**: Manages the mapping between NPC GUIDs and their conversation contexts
- **Technical Details**:
  - Dictionary-based context storage: `Dictionary<GUID, NPCContext_intf>`
  - Singleton pattern for centralized context management
  - Application lifecycle integration for cleanup
  - Interface-based context abstraction for flexibility
- **Context Management**:
  - `HashNPC()`: Registers new NPC with conversation context
  - `containsNPC()`: Checks if NPC has existing context
  - `getNPCContext()`: Retrieves existing context by GUID
  - Context validation during application lifecycle events
- **Lifecycle Handling**:
  - Automatic cleanup on `OnApplicationQuit()`
  - Context disposal through `Close()` interface method
  - Application quit detection prevents invalid operations
  - Safety checks for destroyed GameObjects
- **Hash Management**:
  - GUID-based unique identification per NPC
  - Prevents duplicate context creation for same NPC
  - Debug logging for context registration and system prompt tracking
  - Display functionality for debugging active contexts

### NPCContext_intf.cs
**Interface contract defining per-NPC conversation context structure**
- **Purpose**: Abstracts the NPC context structure for implementation flexibility
- **Required Properties**:
  - `NpcId`: GUID identifier linking context to specific NPC
  - `History`: `ChatHistory` object maintaining conversation flow
  - `Executor`: `InteractiveExecutor` for streaming LLM inference
  - `InferenceParams`: Per-NPC inference configuration (temperature, tokens, etc.)
  - `SystemPrompt`: NPC personality and behavior instructions
  - `LastAccessed`: Timestamp for LRU caching and idle context cleanup
- **Required Methods**:
  - `Close()`: Resource cleanup and context disposal
- **Design Benefits**:
  - Enables multiple context implementation strategies
  - Facilitates testing through mock implementations
  - Supports future context pooling or caching strategies
  - Decouples hasher from concrete context implementation

### NPCContext.cs
**Concrete implementation of NPC conversation context**
- **Purpose**: Data container holding all state for individual NPC conversations
- **Context State**:
  - Unique NPC identifier for context-NPC mapping
  - Complete conversation history with role-based messages
  - Interactive executor instance for streaming responses
  - Configurable inference parameters per NPC
  - System prompt defining NPC personality and constraints
  - Activity timestamp for cache management
- **Initialization**:
  - Constructor-based initialization with all required context components
  - Timestamp set to current time on context creation
  - All properties passed explicitly for clear dependency tracking
- **Lifecycle Management**:
  - `updateNPC()`: Updates last accessed timestamp for activity tracking
  - `OnDestroy()`: Unity lifecycle hook for automatic cleanup
  - `Close()`: Explicit resource disposal with null assignment
  - Debug logging on context closure for monitoring
- **Technical Details**:
  - Plain interface implementation (no MonoBehaviour dependencies in current design)
  - Explicit resource cleanup to enable GC
  - Timestamp tracking enables LRU cache eviction strategies
  - Null assignment prevents dangling references to heavy objects

## Technical Implementation

### Model Loading and Initialization
The system loads the LLM model once during application startup:
1. **Path Resolution**: Model file located in StreamingAssets with full snapshot path
2. **Parameter Configuration**: Context size and GPU layer allocation specified
3. **Model Loading**: `LLamaWeights.LoadFromFile()` loads quantized GGUF model into memory
4. **Context Creation**: Default context created from model for testing
5. **Validation**: Test conversation executed to ensure model functionality

### Context Creation Workflow
When a new AI NPC needs LLM capabilities:
1. **Context Initialization**: Create `NPCContext` with NPC-specific configuration
2. **Executor Assignment**: `InteractiveExecutor` created from shared model context
3. **History Setup**: `ChatHistory` initialized with system prompt for personality
4. **Parameter Configuration**: `InferenceParams` set with token limits and stop sequences
5. **Context Registration**: Context hashed in `UnityLLMContextHasher` by NPC GUID
6. **Retrieval**: NPC controller retrieves context via GUID for conversation execution

### Context Switching and Management
The system supports multiple concurrent NPC conversations:
- **Context Retrieval**: O(1) dictionary lookup by NPC GUID
- **Context Isolation**: Each NPC maintains independent conversation history
- **Memory Sharing**: All contexts share single model weights instance
- **Concurrent Inference**: Multiple NPCs can process responses simultaneously
- **Context Updates**: `LastAccessed` timestamp updated on each interaction

### Memory Optimization Strategy
- **Shared Model Weights**: Single `LLamaWeights` instance (~1-5GB depending on quantization)
- **Minimal Per-Context Overhead**: Each context stores only conversation history and executor
- **Quantized Model**: Q4_K_M quantization reduces model size with minimal quality loss
- **GPU Offloading**: GPU layers reduce CPU memory pressure and improve inference speed
- **Context Cleanup**: Explicit `Close()` calls enable resource reclamation
- **LRU Cache Potential**: `LastAccessed` timestamp enables idle context eviction

### Integration with NPC System
The AI system integrates with the existing NPC framework:
- **NPC Identification**: NPCs use Unity GUIDs for unique identification
- **Context Association**: Each AI-enabled NPC registers context on initialization
- **Dialog System Integration**: Dialog system retrieves context for conversation execution
- **Interaction Coordination**: Player interactions trigger context retrieval and inference
- **State Management**: Context maintains conversation state between interactions

## Usage Example

### Basic NPC Context Creation
```csharp
// In NPC initialization (e.g., NPCController or NPCInit)
GUID npcId = gameObject.GetComponent<GUID_Generator>().GetGUID();

// Create context with NPC-specific configuration
var history = new ChatHistory();
history.AddMessage(AuthorRole.System, "You are a friendly merchant in a medieval fantasy world.");

var inferenceParams = new InferenceParams()
{
    MaxTokens = 150,
    AntiPrompts = new List<string> { "Player:" }
};

var executor = new InteractiveExecutor(UnityLLM.model.CreateContext(UnityLLM.parameters));

var npcContext = new NPCContext(
    npcId, 
    history, 
    executor, 
    inferenceParams, 
    "You are a friendly merchant..."
);

// Register context with hasher
UnityLLMContextHasher.Instance.HashNPC(npcId, npcContext);
```

### Context Retrieval and Usage
```csharp
// In dialog system or interaction handler
GUID npcId = GetNPCGuid();
var context = UnityLLMContextHasher.Instance.getNPCContext(npcId);

if (context != null)
{
    // Add player message to history
    context.History.AddMessage(AuthorRole.User, playerInput);
    
    // Create chat session and get response
    var session = new ChatSession(context.Executor, context.History);
    string response = await GetLLMResponse(session, context.InferenceParams);
    
    // Update access timestamp
    context.LastAccessed = DateTime.Now;
}
```

## Performance Considerations

### Memory Usage
- **Model Size**: ~1.2GB for Q4_K_M quantized Llama-3.2-1B
- **Per-Context Overhead**: ~1-5MB per NPC (conversation history + executor)
- **GPU VRAM**: 5 layers * ~240MB = ~1.2GB GPU memory allocation
- **Total Footprint**: Base model + (NPCs * context overhead)

### Inference Performance
- **First Token Latency**: 100-500ms depending on GPU/CPU
- **Token Generation Speed**: 10-50 tokens/second with GPU acceleration
- **Context Switching**: Near-instant (dictionary lookup only)
- **Concurrent NPCs**: Limited by inference queue, not context switching

### Optimization Opportunities
- **Context Pooling**: Reuse executor instances instead of per-NPC creation
- **LRU Eviction**: Unload contexts for NPCs not recently accessed
- **Batch Inference**: Process multiple NPC responses in single inference call
- **Dynamic GPU Layers**: Adjust GPU offloading based on available VRAM
- **Prompt Caching**: Cache common system prompts to reduce token processing

## Comparison with ServerFiles Network Approach

### UnityAIScripts (Local LLM)
**Advantages**:
- Zero network latency - immediate response generation
- No external dependencies or server management
- Offline functionality for single-player experiences
- Lower ongoing operational costs (no server hosting)
- Better privacy - all inference happens locally

**Disadvantages**:
- Higher client system requirements (GPU recommended)
- Larger application size (model bundled with game)
- Limited to smaller models (1-3B parameters feasible)
- Player hardware determines inference quality/speed

### ServerFiles (Network LLM)
**Advantages**:
- Access to larger, more capable models (7B-70B parameters)
- Consistent inference quality across all clients
- Lower client system requirements
- Centralized model updates without client patches

**Disadvantages**:
- Network latency (100-1000ms+ response times)
- Server infrastructure and operational costs
- Requires internet connectivity for AI features
- Scalability concerns with many concurrent players

## Future Enhancements

### Planned Improvements
- **Context Pooling**: Implement executor reuse to reduce per-NPC memory overhead
- **LRU Cache**: Automatic eviction of idle NPC contexts after configurable timeout
- **Streaming Response UI**: Real-time token-by-token display in dialog boxes
- **Dynamic Model Loading**: Support for multiple models with runtime switching
- **Inference Queue**: Priority queue for managing multiple concurrent NPC responses
- **Response Caching**: Cache responses for common questions to improve performance
- **System Prompt Library**: Predefined personality templates for different NPC types

### Integration Possibilities
- **Emotion Detection**: Parse LLM responses for NPC emotional state transitions
- **Quest Generation**: Use LLM to dynamically generate side quests from conversations
- **Dynamic Dialog Trees**: Blend scripted dialog with LLM-generated responses
- **Voice Synthesis**: Integrate with TTS for voiced AI NPC conversations
- **Player Profiling**: Adapt NPC personality based on player conversation history

## Dependencies

### LLamaSharp Package
- **Version**: 0.25.0 (LLamaSharp.Backend.Cpu)
- **Purpose**: .NET bindings for llama.cpp inference engine
- **Native Libraries**: ggml.dll, llama.dll (AVX512 optimized)
- **Model Format**: GGUF (standardized quantized model format)

### Unity Packages
- **Unity.VisualScripting**: GUID generation and component integration
- **UnityEngine**: Core Unity functionality and MonoBehaviour lifecycle

### Model Files
- **Model**: Llama-3.2-1B-Instruct (Unsloth GGUF)
- **Quantization**: Q4_K_M (4-bit quantization, medium quality)
- **Size**: ~1.2GB on disk
- **Location**: Assets/StreamingAssets/Models/

## Troubleshooting

### Common Issues

**Model Load Failure**
- Verify model file exists at specified path in StreamingAssets
- Check model file isn't corrupted (redownload if necessary)
- Ensure sufficient RAM available (minimum 4GB free recommended)

**GPU Acceleration Not Working**
- Verify AVX512 DLL plugins are enabled in Unity plugin settings
- Check GPU compatibility (CUDA for NVIDIA, ROCm for AMD)
- Reduce `GpuLayerCount` if VRAM insufficient

**Context Not Found**
- Ensure `HashNPC()` called during NPC initialization before first interaction
- Verify GUID consistency between registration and retrieval
- Check `UnityLLMContextHasher` instance exists in scene

**Slow Inference**
- Increase `GpuLayerCount` if VRAM available
- Reduce `MaxTokens` in `InferenceParams` for faster responses
- Consider more aggressive quantization (Q3 or Q2)
- Verify CPU isn't thermal throttling during inference

**Memory Leaks**
- Ensure `Close()` called on contexts when NPCs destroyed
- Verify `OnApplicationQuit()` executes during shutdown
- Check for circular references preventing context garbage collection

## Best Practices

### Context Lifecycle
- Create contexts during NPC initialization, not on first interaction
- Register contexts immediately after creation to prevent orphaned executors
- Update `LastAccessed` timestamp on each interaction for LRU tracking
- Call `Close()` explicitly when removing NPCs from scene

### System Prompt Design
- Keep system prompts concise (50-200 tokens) to preserve context space
- Include clear personality traits and behavioral constraints
- Specify response format expectations (length, style, perspective)
- Test prompts with various player inputs to ensure consistent behavior

### Performance Optimization
- Limit maximum active NPC contexts based on target hardware
- Implement conversation timeout to prevent infinite generation
- Use antiprompts to control response length naturally
- Monitor inference time and adjust `GpuLayerCount` dynamically if needed

### Error Handling
- Wrap LLM inference calls in try-catch for graceful failure
- Implement fallback dialog for inference errors
- Log model loading failures with detailed error information
- Validate context exists before attempting inference operations
