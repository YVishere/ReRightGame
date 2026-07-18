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
- **Purpose**: Loads and manages the shared LLamaSharp model instance for all AI NPCs; exposes factory and per-NPC inference methods
- **Model Configuration**:
  - Model path: `Llama-3.2-1B-Instruct-Q4_K_M.gguf` (quantized 4-bit model)
  - Context size: 1024 tokens for conversation memory
  - GPU acceleration: `GpuLayerCount = -1` — offloads **all** model layers to the GPU (CUDA12 backend)
  - Model format: GGUF format from Unsloth optimized for inference
- **Technical Details**:
  - `model` and `parameters` are **public static** — shared across all NPCs and accessible by the factory
  - No class-level context, executor, or chatHistory fields — all state is per-NPC
  - Singleton pattern for global LLM service access (`Instance` set in `Awake()`)
  - `Awake()` is `async void` (Unity-compatible)
  - **Static constructor** `static UnityLLM()` runs `PreloadBackendDlls()` then loads the model — this fixes native init order (see below)
  - `PreloadBackendDlls()` uses `LoadLibraryEx` with `LOAD_WITH_ALTERED_SEARCH_PATH` to load `llama.dll` and its `ggml-*.dll` siblings from the backend's own folder, so Windows resolves native dependencies correctly **before** LLamaSharp's `NativeApi` static constructor fires
- **Initialization Process**:
  - Native backend DLLs preloaded and `LLamaWeights.LoadFromFile()` run in the **static constructor**, before any `Awake()` — guarantees `model`/`parameters` are ready when NPCs call `CreateNPCContext()`
  - In `Awake()`, if `constData._llmDebug = true`: runs a startup test conversation (Bob prompt) to validate inference
  - In `Awake()`, if `constData._llmDebug = false`: logs that per-NPC context mode is active
- **Public API**:
  - `CreateNPCContext(GUID npcId, string systemPrompt)` — **static factory**: creates a fresh `LLamaContext`, `InteractiveExecutor`, and `ChatHistory` seeded with `systemPrompt`; returns `NPCContext`
  - `talk2LLMWithContext(NPCContext_intf ctx, string user)` — **per-NPC inference**: reuses the persisted `ctx.Session` (built once at context creation) so the `InteractiveExecutor` KV cache is never replayed from scratch; streams the response and updates `LastAccessed`
  - `talk2LLM(string user)` — **legacy, `_tcp` path only**: creates a fresh shared context per call; returns `string.Empty` when `_tcp = false`
- **Memory Management**:
  - Single model instance reduces RAM usage (vs per-NPC models)
  - Model remains loaded for application lifetime
  - Each NPC gets its own `LLamaContext` via factory — no shared state between NPCs

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
  - `Session`: `ChatSession` built once over `Executor` + `History` and reused every turn — preserves the KV cache across messages
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
  - `ChatSession` created once in the constructor (`new ChatSession(executor, history)`) and reused every turn
  - Configurable inference parameters per NPC
  - System prompt defining NPC personality and constraints
  - Activity timestamp for cache management
- **Initialization**:
  - Constructor-based initialization with all required context components
  - `Session` is instantiated inside the constructor from the passed `executor` + `history`
  - Timestamp set to current time on context creation
  - All properties passed explicitly for clear dependency tracking
- **Lifecycle Management**:
  - `updateNPC()`: Updates last accessed timestamp for activity tracking
  - `OnDestroy()`: Unity lifecycle hook that calls `Close()`
  - `Close()`: Nulls `Session`, `Executor`, and `History`, sets `LastAccessed` to `DateTime.MinValue`, and logs closure
  - Debug logging on context closure for monitoring
- **Technical Details**:
  - Plain interface implementation (no MonoBehaviour dependencies in current design)
  - Explicit resource cleanup to enable GC
  - Timestamp tracking enables LRU cache eviction strategies
  - Null assignment prevents dangling references to heavy objects

### Technical Implementation

### Model Loading and Initialization
The system loads the LLM model once in the **static constructor** (`static UnityLLM()`), before any Unity lifecycle method runs:
1. **Native Preload**: `PreloadBackendDlls()` loads `llama.dll` and its `ggml-*.dll` siblings via `LoadLibraryEx(LOAD_WITH_ALTERED_SEARCH_PATH)` so native dependencies resolve from the backend folder before LLamaSharp's `NativeApi` initializes
2. **Path Resolution**: Model file located in StreamingAssets with full snapshot path
3. **Parameter Configuration**: `ContextSize = 1024`, `GpuLayerCount = -1` (offload all layers)
4. **Model Loading**: `LLamaWeights.LoadFromFile()` loads the quantized GGUF model into memory
5. **Validation** (debug only): In `Awake()`, a test conversation runs if `constData._llmDebug = true`

### Context Creation Workflow
When a new AI NPC starts (`NPCController.Start()`):
1. `NPCController` calls `UnityLLM.CreateNPCContext(npcID, personalityPrompt)`
2. Factory creates fresh `LLamaContext` from shared `model`, new `InteractiveExecutor`, and `ChatHistory` seeded with system prompt
3. The `NPCContext` constructor builds the `ChatSession` **once** from that executor + history
4. `NPCContext` returned and registered in `UnityLLMContextHasher` keyed by NPC GUID
5. On player message, `LLM_NPCController.getDialog()` retrieves context by GUID and calls `talk2LLMWithContext()`
6. Inference streams over the NPC's **persisted** `ctx.Session` — history stays isolated and the KV cache is preserved between turns
7. On NPC destroy, `NPCController.OnDestroy()` calls `ctx.Close()` to release the `LLamaContext`

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
// In NPC initialization (e.g., NPCController.Start())
GUID npcId = gameObject.GetComponent<GUID_Generator>().GetGUID();

// Use the factory — it creates the LLamaContext, InteractiveExecutor,
// ChatHistory (seeded with the system prompt), and the reusable ChatSession.
NPCContext npcContext = UnityLLM.CreateNPCContext(
    npcId,
    "You are a friendly merchant in a medieval fantasy world.");

// Register context with hasher
UnityLLMContextHasher.Instance.HashNPC(npcId, npcContext);
```

### Context Retrieval and Usage
```csharp
// In dialog system or interaction handler
GUID npcId = GetNPCGuid();
NPCContext_intf context = UnityLLMContextHasher.Instance.getNPCContext(npcId);

if (context != null)
{
    // Streams over the persisted ctx.Session and updates LastAccessed internally.
    string response = await UnityLLM.Instance.talk2LLMWithContext(context, playerInput);
}
```

## Performance Considerations

### Memory Usage
- **Model Size**: ~1.2GB for Q4_K_M quantized Llama-3.2-1B
- **Per-Context Overhead**: ~1-5MB per NPC (conversation history + executor)
- **GPU VRAM**: `GpuLayerCount = -1` offloads all layers — expect roughly the full ~1.2GB model in VRAM plus KV cache
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
- **Version**: 0.27.0
- **Active Backend**: `LLamaSharp.Backend.Cuda12` (Windows). `LLamaSharp.Backend.Cpu` is restored as a transitive dependency but its plugins are disabled — see `UnityEngineHelper/LLamaBackendSetup.cs`
- **Purpose**: .NET bindings for llama.cpp inference engine
- **Native Libraries**: `llama.dll`, `ggml.dll`, `ggml-base.dll`, `ggml-cpu.dll`, `ggml-cuda.dll` (preloaded via `LoadLibraryEx` in `UnityLLM.PreloadBackendDlls()`)
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
- Verify the CUDA12 backend plugins are enabled and the Cpu backend is disabled (run **Tools/Fix LLamaSharp Backend Plugins** — see `LLamaBackendSetup.cs`)
- Confirm `ggml-cuda.dll` is present in the active backend's native folder and was preloaded (check for the `UnityLLM: Preloaded ggml-cuda.dll` log line)
- Check GPU compatibility (CUDA 12 for NVIDIA)
- Reduce `GpuLayerCount` from `-1` to a fixed count if VRAM is insufficient

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
