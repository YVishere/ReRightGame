# LLM Setup

## Model

Download `Llama-3.2-1B-Instruct-Q4_K_M.gguf` from HuggingFace (Unsloth):
`https://huggingface.co/unsloth/Llama-3.2-1B-Instruct-GGUF?show_file_info=Llama-3.2-1B-Instruct-Q4_K_M.gguf&library=llama-cpp-python`

After download the file will be at:
`C:\Users\<username>\.cache\huggingface\hub\models--unsloth--Llama-3.2-1B-Instruct-GGUF\snapshots\<hash>\`

Copy/move it into:
`Assets/StreamingAssets/Models/models--unsloth--Llama-3.2-1B-Instruct-GGUF/snapshots/<hash>/`

The exact path is configured in `UnityLLM.cs` → `modelPath`.

---

## Runtime Path (`constData._tcp = false`) — Default

Inference runs **in-process** inside Unity via the **LLamaSharp** plugin (llama.cpp bindings). No Python process is started.

### How per-NPC context works
1. `UnityLLM` loads the shared `LLamaWeights` once on startup (`public static model`).
2. Each AI NPC calls `UnityLLM.CreateNPCContext(npcID, systemPrompt)` in `Start()`, which creates a fresh `LLamaContext` + `InteractiveExecutor` + `ChatHistory` seeded with the NPC's personality.
3. The context is registered in `UnityLLMContextHasher` keyed by the NPC's Unity GUID.
4. On each player message, `LLM_NPCController.getDialog()` retrieves the NPC's context and calls `UnityLLM.talk2LLMWithContext()`, so conversation history is fully isolated per character.
5. On NPC destroy, `NPCController.OnDestroy()` calls `ctx.Close()` to free the `LLamaContext`.

---

## Legacy Path (`constData._tcp = true`)

Uses the Python TCP server (`ServerSocketPython.py`) with Ollama + LLaMA 3.2 via `langchain-ollama`.

### Python requirements
```
ollama
langchain
langchain-ollama
torch
```

Run `pip install -r requirements.txt` inside `Assets/Scripts/ServerFiles/`.

The Unity side spawns the Python process automatically on play. Authentication uses Windows Named Pipes (IPC) + HMAC-SHA256 request tokens.

---

## `constData._tcp` Flag

Location: `Assets/Scripts/constData.cs`

```csharp
public class constData
{
    public const bool _tcp = false; // false = llama.cpp in-process | true = Python TCP server
}
```

Because `_tcp` is a **compile-time constant**, the compiler dead-code-eliminates the inactive branch with zero runtime overhead. Change the value and recompile to switch paths.