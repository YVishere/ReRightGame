# UnityEngineHelper System Logging File

This file is used by agentic models to log analysis, observations, and insights about the Unity Editor integration utilities and domain reload management systems.

## Log Format
- **Timestamp**: Date and time of log entry
- **Component**: Specific script or system being analyzed
- **Observation**: What was observed or analyzed
- **Impact**: How this affects the editor workflow or resource management
- **Recommendations**: Suggested improvements or changes

---

## [2025-08-08 12:30] - GitHub Copilot - Domain Reload Helper Creation
**Component**: DomainReloadHelper.cs - Initial implementation
**Observation**: Created Unity Editor integration utility to handle proper resource cleanup during domain reload cycles
**Impact**: 
- Prevents Unity Editor from hanging during domain reloads when IPC pipes and background tasks are active
- Ensures AuthManager, Hasher, and ServerSocketC singletons are properly cleaned up before assembly reload
- Provides development tools for manual cleanup testing and debugging
- Eliminates resource leaks that can accumulate during rapid development iteration
**Recommendations**: 
- Monitor cleanup sequence timing to ensure all resources are freed before domain reload timeout
- Add more granular logging to track individual singleton cleanup success/failure
- Consider adding cleanup validation to ensure resources are actually freed
- Test with complex scenarios involving multiple AI NPCs and active connections

**Technical Details**:
- Uses [InitializeOnLoad] for automatic registration during domain initialization
- Subscribes to AssemblyReloadEvents.beforeAssemblyReload for early cleanup notification
- Implements forced cleanup through ForceCleanupAllInstances() pattern
- Provides "Tools/Force Cleanup Before Domain Reload" menu item for manual testing
- Ensures cleanup happens before Unity's normal destruction sequence

---

## [2025-08-08 12:45] - GitHub Copilot - Console Logging Persistence Analysis
**Component**: Multiple systems - Background task and process management
**Observation**: Identified potential causes for console outputs continuing after game stop, including background tasks, Python processes, and async operations
**Impact**: 
- Background authentication tasks may continue running after game stop
- Python server processes might not terminate immediately with Unity
- Async TCP operations could generate delayed log outputs
- Unity's logging system buffering may show cached messages after shutdown
**Recommendations**: 
- Implement more aggressive task cancellation with shorter timeouts
- Add explicit Python process monitoring and termination verification
- Create logging guards to prevent output after cleanup initiation
- Ensure all event handlers are properly unsubscribed during cleanup
- Add process tracking to detect orphaned Python instances

**Potential Root Causes**:
1. HandleAuthenticationRequests task continuing beyond game stop
2. Python subprocess lifecycle not synchronized with Unity shutdown
3. TCP socket operations with pending async callbacks
4. Named pipe operations hanging in WaitForConnectionAsync
5. Unity Editor vs Game Stop cleanup sequence differences
6. Delayed log buffer flushing showing old messages

---

## 2026-07-16 - GitHub Copilot (Claude Sonnet 4.6) - LLamaSharp Multi-Backend Plugin Conflict Automation

### Component: LLamaBackendSetup.cs (NEW)
**Observation**: NuGetForUnity installs `LLamaSharp.Backend.Cpu` as a transitive dependency of `LLamaSharp.Backend.Cuda12`. The Cpu package ships identical DLL names (`ggml.dll`, `llama.dll`, `ggml-base.dll`, `ggml-cpu.dll`, `mtmd.dll`) across four AVX subdirectories (`avx512/`, `avx2/`, `avx/`, `noavx/`). Unity marks all of them as Editor-compatible, raising "Multiple plugins with the same name" errors that block Play Mode. Additionally, the Cuda12 backend is missing `ggml-cpu.dll` which is required by the OS as a DLL load-time dependency even on the CUDA path.

**Changes Made**:
- Created `LLamaBackendSetup.cs` in `UnityEngineHelper/` as an `[InitializeOnLoad]` Editor-only script
- `EditorApplication.delayCall` defers execution so the AssetDatabase is fully ready before plugin importers are touched
- Step 1: iterates all DLLs under `LLamaSharp.Backend.Cpu*` package folders; calls `PluginImporter.SetCompatibleWithEditor(false)` + `SetCompatibleWithAnyPlatform(false)` + `SaveAndReimport()` on each — kills duplicate-plugin warnings without deleting files
- Step 2: locates the active backend's `llama.dll`; if `ggml-cpu.dll` is missing from the same directory, copies the highest-AVX variant (avx512 > avx2 > avx > noavx) from the Cpu backend and imports it as a new plugin asset
- `[MenuItem("Tools/Fix LLamaSharp Backend Plugins")]` exposes a manual trigger for after NuGet restores that regenerate `.meta` files
- `#if UNITY_EDITOR` guard wraps the entire class, matching `DomainReloadHelper.cs` conventions

**Impact**:
- Eliminates "Multiple plugins with same name" errors on every domain reload
- Fully automated — no manual folder deletion or DLL copying needed after team members run NuGet restore
- Fix persists across NuGet restores as long as the modified `.meta` files are committed to version control
- Properly excludes `LLamaSharp.Backend.Cuda12.Linux.*` and `LLamaSharp.Backend.Cuda12.Windows.*` meta-package folders from the active backend detection
- LLamaSharp's `DefaultNativeLibrarySelectingPolicy` continues to handle AVX-level selection at runtime through OS DLL resolution, not through Unity's plugin loader

**Recommendations**:
- Commit the `.meta` files generated by `SaveAndReimport()` to version control — this is what makes the fix survive future NuGet restores
- Run **Tools/Fix LLamaSharp Backend Plugins** manually any time NuGetForUnity regenerates the Cpu backend `.meta` files
- For multi-OS teams: extend Step 2 to detect platform via `Application.platform` and copy the appropriate `ggml-cpu` variant (`.dll` on Windows, `.so` on Linux)
- The `ggml-cpu.dll` in the Cuda12 folder is safe to exclude from git — losing it only requires re-running the menu item

**Technical Details**:
- `Application.dataPath` used as the base for absolute-to-project-relative path conversion
- AVX priority order: avx512 (3) > avx2 (2) > avx (1) > noavx (0) — selects best available source DLL
- `AssetImporter.GetAtPath` cast to `PluginImporter`; skips non-plugin assets silently
- Guard: skips DLLs already set to incompatible to avoid unnecessary `SaveAndReimport()` calls

---

<!-- Agentic models: Add your logging entries below this line -->
