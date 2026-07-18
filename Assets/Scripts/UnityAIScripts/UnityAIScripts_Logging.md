# UnityAIScripts System Logging File

This file is used by agentic models to log analysis, observations, and insights about the local LLM integration system and AI-powered NPC conversation management.

## Log Format
- **Timestamp**: Date and time of log entry
- **Component**: Specific script or system being analyzed
- **Observation**: What was observed or analyzed
- **Impact**: How this affects the AI system
- **Recommendations**: Suggested improvements or changes

---

## 2026-07-16 - Claude Code (Opus 4.8) - README accuracy pass vs current code

### Component: UnityAIScripts/README.md
**Observation**: Documentation had drifted from the implementation in several concrete places after the persisted-`ChatSession`, CUDA12, and static-constructor changes.

**Changes Made**:
- **Persisted `ChatSession`**: Documented the new `Session` property on `NPCContext_intf`/`NPCContext` (built once in the constructor, reused every turn to preserve the KV cache). Corrected `talk2LLMWithContext` — it reuses `ctx.Session` rather than building a fresh `ChatSession` each call. Updated the Context Creation Workflow and both Usage Examples (creation now via the `CreateNPCContext` factory; retrieval via `talk2LLMWithContext`).
- **Startup test gating**: Corrected from `constData._tcp = true` to `constData._llmDebug = true` (the actual guard in `Awake()`).
- **GPU layers**: Corrected "5 layers offloaded" → `GpuLayerCount = -1` (all layers) in Model Configuration, Performance/VRAM, and troubleshooting.
- **Static constructor + native preload**: Documented that `static UnityLLM()` runs `PreloadBackendDlls()` (`LoadLibraryEx` + `LOAD_WITH_ALTERED_SEARCH_PATH`) and `LLamaWeights.LoadFromFile()` before any `Awake()`.
- **Dependencies**: Updated LLamaSharp `0.25.0`/Cpu/AVX512 → `0.27.0` on the CUDA12 backend (Cpu backend restored but disabled via `LLamaBackendSetup.cs`); listed the actual preloaded native DLLs.
- **`Close()`**: Corrected to reflect nulling `Session`/`Executor`/`History` and setting `LastAccessed = DateTime.MinValue`.

### Component: Assets/Scripts/README.md
**Observation**: `constData._llmDebug` was undocumented.

**Changes Made**: Added the `_llmDebug` flag to the `constData.cs` entry.

**Impact**: Docs now match `UnityLLM.cs`, `NPCContext.cs`, `NPCContext_intf.cs`, and `constData.cs` as they stand on the `BundledLLM` branch. No code was changed.

**Recommendations**: The pre-existing 2026-01-18 backlog items (LRU eviction, model-load try/catch, ScriptableObject config) remain open and unaddressed by this pass.

---

## 2026-03-15 - GitHub Copilot (Claude Sonnet 4.6) - Per-NPC Context Implementation

### Component: UnityLLM.cs
**Observation**: The two recommendations from the 2026-01-18 Model Sharing Architecture entry have been fully implemented.

**Changes Made**:
- `model` and `parameters` changed from `private static` to `public static` — accessible by factory and by NPC registration code
- Removed shared class-level `context`, `executor`, `chatHistory`, `inferenceParams` fields — no class-level inference state remains
- Removed static `freshContext` / `freshExec` fields — these were the root cause of shared context bleed between NPCs
- `Awake()` changed from `async Task` to `async void` (Unity-compatible lifecycle)
- Legacy test conversation in `Awake()` wrapped in `#pragma warning disable CS0162` + `if (constData._tcp)` guard
- Added `CreateNPCContext(GUID npcId, string systemPrompt)` static factory
- Added `talk2LLMWithContext(NPCContext_intf ctx, string user)` per-NPC inference method
- `talk2LLM(string user)` body wrapped in `if (constData._tcp)` guard; returns `string.Empty` on llama.cpp path
- Added `using UnityEditor;` for `GUID` type resolution

**Impact**: Each AI NPC now has a fully isolated `LLamaContext` + `InteractiveExecutor` + `ChatHistory`. Conversation histories cannot bleed between characters. The shared model weights (`LLamaWeights`) remain loaded once for the application lifetime.

---

### Component: NPCController.cs
**Observation**: NPC registration with `UnityLLMContextHasher` wired up alongside existing TCP path.

**Changes Made**:
- Added `npcPersonality` field to capture personality string from `dialogBecomesContext()`
- `dialogBecomesContext()` now stores the personality string in `npcPersonality` before passing to `Dialog`
- `Start()` `else` branch (when `_tcp` is `false`): calls `UnityLLM.CreateNPCContext(npcID, npcPersonality)` and `UnityLLMContextHasher.Instance.HashNPC(npcID, ctx)`
- `OnDestroy()` branched: TCP path stops retrying, llama.cpp path calls `UnityLLMContextHasher.Instance.getNPCContext(npcID)?.Close()`
- TCP `establishAndStoreConnection()` call wrapped with `#pragma warning disable CS0162` to suppress dead-code warning

**Impact**: Every `NPC_AI`-tagged NPC registers its own context on startup and cleans it up on destroy.

---

### Component: LLM_NPCController.cs
**Observation**: `getDialog()` now dispatches correctly on `constData._tcp`.

**Changes Made**:
- TCP branch: restored previously-commented-out `ServerSocketC.Instance.NPCRequest()` call via `Hasher.getNPCConnection(npcID)` + `reformatDialog()`
- llama.cpp branch: retrieves `NPCContext_intf` from `UnityLLMContextHasher` by GUID, calls `UnityLLM.Instance.talk2LLMWithContext(ctx, userSpeech[^1])`
- `reformatDialog()` is only invoked in the TCP branch (it produces the `Invoke:::` wire format not needed by llama.cpp's native `ChatSession`)

**Impact**: Both paths compile and work. Switching `constData._tcp` is the only change needed to toggle between them.

---

### Component: constData.cs
**Observation**: `USING_TCP` renamed to `_tcp` for cleaner namespacing across the codebase.

**Changes Made**:
- `public const bool USING_TCP = false;` → `public const bool _tcp = false;`
- All 5 call sites updated: `ServerSocketC.cs`, `NPCController.cs`, `AuthManager.cs`, `DomainReloadHelper.cs`, `Killports.cs`

**Impact**: Consistent naming; the `const` nature means the compiler eliminates inactive branches at compile time with zero runtime cost.

---

## 2026-01-18 - Initial System Analysis

### Component: NPCContext.cs
**Observation**: NPCContext inherits from MonoBehaviour but is used as a data container class, not a Unity component attached to GameObjects.

**Impact**: 
- MonoBehaviour constructors don't work properly in Unity - they're not meant to be instantiated with `new`
- `OnDestroy()` will never be called unless the NPCContext is actually attached to a GameObject
- Unnecessary overhead from Unity's component lifecycle for what is essentially a POCO (Plain Old C# Object)
- Creates confusion about instantiation pattern (should it be AddComponent or new?)

**Recommendations**:
- Remove MonoBehaviour inheritance - NPCContext should be a plain C# class
- Remove `OnDestroy()` method as it won't execute for non-attached instances
- Keep the interface implementation for abstraction benefits
- Rely on explicit `Close()` calls from UnityLLMContextHasher for cleanup
- Consider making it a struct if immutability is desired

---

## 2026-01-18 - Model Sharing Architecture

### Component: UnityLLM.cs
**Observation**: System creates a single static `LLamaContext` from the model and uses it for test executor, but doesn't expose the model or parameters for NPC context creation.

**Impact**:
- NPCs cannot currently create their own contexts from the shared model
- Static context is created but only used for testing, wasting resources
- No public API for NPCs to access shared model for context creation
- Current design requires each NPC to load their own model (defeats single-instance purpose)

**Recommendations**:
- Expose `model` and `parameters` as public static properties
- Remove the test-specific static context creation
- Add factory method: `public static LLamaContext CreateNPCContext()` 
- Document in code comments that contexts should be created via UnityLLM
- Example: `var context = UnityLLM.CreateNPCContext(); var executor = new InteractiveExecutor(context);`

---

## 2026-01-18 - Context Access Pattern

### Component: UnityLLMContextHasher.cs
**Observation**: `getNPCContext()` returns NPCContext_intf (interface reference) which is good for abstraction but limits access to implementation-specific methods.

**Impact**:
- Calling code can only access interface-defined members
- Cannot call `updateNPC()` from NPCContext through interface reference
- `LastAccessed` is exposed in interface so it can be updated, but no update method in interface
- Inconsistency between interface contract and implementation capabilities

**Recommendations**:
- Add `void UpdateAccess();` to NPCContext_intf interface
- Implement in NPCContext as: `public void UpdateAccess() { LastAccessed = DateTime.Now; }`
- Remove standalone `updateNPC()` method or rename to match interface convention
- Consider adding `bool IsExpired(TimeSpan maxAge)` to interface for LRU checks

---

## 2026-01-18 - Memory Management Concerns

### Component: System Architecture
**Observation**: No mechanism exists for pruning idle or expired NPC contexts, despite LastAccessed timestamp tracking.

**Impact**:
- Contexts accumulate indefinitely until application quit
- Memory usage grows linearly with total NPCs encountered (even if no longer in scene)
- No way to reclaim resources for NPCs that have been destroyed or are far from player
- `LastAccessed` property exists but isn't used for any decision making

**Recommendations**:
- Implement LRU cache eviction in UnityLLMContextHasher
- Add `Update()` or coroutine to periodically check for expired contexts
- Add configuration: `public float contextTimeoutSeconds = 300f; // 5 minutes`
- Implement: `public void PruneIdleContexts(TimeSpan maxIdleTime)`
- Consider max context limit (e.g., only keep 10 most recent contexts)
- Add metrics logging: active contexts, total contexts created, contexts pruned

---

## 2026-01-18 - Error Handling Gap

### Component: UnityLLM.cs, UnityLLMContextHasher.cs
**Observation**: No try-catch blocks around model loading or context operations; failures will crash application.

**Impact**:
- Model file missing/corrupted = immediate application crash
- Insufficient memory = unhandled exception and crash
- Context operations during shutdown can throw NullReferenceException
- No graceful degradation path for AI system failure

**Recommendations**:
- Wrap `LLamaWeights.LoadFromFile()` in try-catch with fallback to disable AI
- Add `public static bool IsModelLoaded { get; private set; }` flag
- Implement null checks before model operations
- Add `HashNPC()` validation: return false if model not loaded
- Log detailed error messages for troubleshooting
- Consider fallback to scripted dialog if model unavailable

---

## 2026-01-18 - Async/Await Pattern

### Component: UnityLLM.cs
**Observation**: `Awake()` is marked as `async Task` but Unity doesn't natively support async lifecycle methods.

**Impact**:
- Unity calls Awake() synchronously and doesn't await the Task
- Test conversation may not complete before other initialization code runs
- Race condition between model loading and NPC initialization
- No guarantee Instance is set when other scripts try to access it

**Recommendations**:
- Change `Awake()` to synchronous, move async code to separate initialization method
- Use `async void Start()` for Unity-compatible async lifecycle
- Or implement: `public static async Task InitializeModel()` and call from game manager
- Add `IsInitialized` flag to track initialization completion
- Make other systems wait for model initialization before registering contexts

---

## 2026-01-18 - Unused Dependencies

### Component: UnityLLM.cs
**Observation**: Imports `Mono.Cecil.Cil` and `UnityEditor.Rendering.LookDev` which are not used in the code.

**Impact**:
- Unnecessary assembly references increase compilation time
- Editor-only namespaces (`UnityEditor`) will cause build errors for standalone builds
- Clutters code and creates confusion about actual dependencies
- May indicate copied boilerplate code not cleaned up

**Recommendations**:
- Remove unused using statements: `Mono.Cecil.Cil` and `UnityEditor.Rendering.LookDev`
- Use IDE/editor to organize and remove unused imports
- Verify build succeeds without UnityEditor dependencies
- Document actual required dependencies in code comments

---

## 2026-01-18 - Configuration Management

### Component: UnityLLM.cs
**Observation**: Model path, context size, and GPU layers are hard-coded constants.

**Impact**:
- Requires code changes to adjust configuration per deployment
- Cannot optimize for different hardware without recompilation  
- No way to A/B test different model configurations
- Path assumptions may break on different platforms or project structures

**Recommendations**:
- Create `LLMConfiguration` ScriptableObject for settings
- Expose: model path, context size, GPU layers, max tokens, temperature
- Add platform-specific configuration overrides (PC vs mobile)
- Implement model path validation with fallback search paths
- Add runtime configuration UI for testing different settings
- Use relative paths that work with StreamingAssets on all platforms

---

## 2026-01-18 - Context Retrieval Safety

### Component: UnityLLMContextHasher.cs
**Observation**: `getNPCContext()` returns null for missing contexts, requiring null checks at every call site.

**Impact**:
- Easy to forget null check and get NullReferenceException
- Repetitive null checking code in all dialog/interaction systems
- No logging when context lookup fails (silent failure)
- Difficult to distinguish between "NPC not registered" and "hasher not initialized"

**Recommendations**:
- Add `TryGetNPCContext(GUID npcId, out NPCContext_intf context)` method
- Log warning when context not found (helps debugging)
- Consider throwing exception for unexpected missing contexts vs returning null for expected cases
- Add `EnsureContext(GUID npcId)` helper that creates default context if missing
- Document expected usage pattern in XML comments

---

## 2026-01-18 - Inference Parameters Duplication

### Component: UnityLLM.cs, NPCContext.cs
**Observation**: InferenceParams defined both in UnityLLM (for testing) and per-NPC in NPCContext.

**Impact**:
- Unclear which parameters are "defaults" and which are customized
- Test parameters in UnityLLM don't represent actual NPC usage
- No shared default configuration for NPCs to start from
- Each NPC creator must know to configure all inference parameters

**Recommendations**:
- Add `public static InferenceParams DefaultInferenceParams` to UnityLLM
- NPCContext constructor should accept optional parameters, defaulting to UnityLLM defaults
- Document which parameters are safe to customize per NPC vs system-wide
- Consider parameter validation (e.g., MaxTokens must be < ContextSize)
- Add preset configurations: "verbose", "concise", "creative", "factual"

---

## 2026-01-18 - Testing and Validation

### Component: System Architecture
**Observation**: Test conversation in UnityLLM.Awake() is hard-coded and runs every application start.

**Impact**:
- Adds 5-10 seconds to every startup for test inference
- Test output clutters logs during normal gameplay
- No way to disable test without code modification
- Wastes tokens/context for no gameplay benefit in production

**Recommendations**:
- Add `public bool runStartupTest = false;` serialized field (default false)
- Guard test conversation with `if (runStartupTest)` check
- Move test to separate test component or editor-only script
- Add proper unit tests for context management using Unity Test Framework
- Consider test scene specifically for LLM functionality validation
- Add performance benchmarks: tokens/sec, first token latency, memory usage

---
