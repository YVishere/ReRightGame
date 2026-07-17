using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Mono.Cecil.Cil;
using UnityEditor.Rendering.LookDev;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using Unity.VectorGraphics.Editor;
using UnityEngine.UI;

// Unity Script to act as a single point of truth for LLM model and context
class UnityLLM : MonoBehaviour
{
    public static UnityLLM Instance { get; private set; }
    private static string modelPath = @"Assets\StreamingAssets\Models\models--unsloth--Llama-3.2-1B-Instruct-GGUF\snapshots\b69aef112e9f895e6f98d7ae0949f72ff09aa401\Llama-3.2-1B-Instruct-Q4_K_M.gguf";

    public static ModelParams parameters;
    public static LLamaWeights model;

    // Loads llama.dll and all sibling DLLs into the process using
    // LOAD_WITH_ALTERED_SEARCH_PATH so Windows resolves dependencies
    // from the DLL's own folder, not the application directory.
    // This must run before new ModelParams() triggers NativeApi..cctor()
    // and its DllImport("llama") call.
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
    private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x8;

    private static void PreloadBackendDlls()
    {
        string packagesPath = Path.Combine(Application.dataPath, "Packages");
        if (!Directory.Exists(packagesPath)) return;

        // Find llama.dll — whichever backend is installed
        string? llamaDll = null;
        foreach (string pkg in Directory.GetDirectories(packagesPath, "LLamaSharp.Backend.*"))
        {
            var found = Directory.GetFiles(pkg, "llama.dll", SearchOption.AllDirectories);
            if (found.Length > 0) { llamaDll = found[0]; break; }
        }
        if (llamaDll == null)
        {
            Debug.LogError("UnityLLM: No llama.dll found in Assets/Packages.");
            return;
        }

        // Load each DLL in dependency order from the same folder.
        // LOAD_WITH_ALTERED_SEARCH_PATH makes Windows use that folder for all deps.
        string nativeDir = Path.GetDirectoryName(llamaDll)!;
        foreach (string name in new[] { "ggml-base.dll", "ggml-cpu.dll", "ggml-cuda.dll", "ggml.dll", "llama.dll" })
        {
            string fullPath = Path.Combine(nativeDir, name);
            if (!File.Exists(fullPath)) continue;
            IntPtr h = LoadLibraryEx(fullPath, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
            if (h == IntPtr.Zero)
                Debug.LogWarning($"UnityLLM: Failed to preload {name} (error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()})");
            else
                Debug.Log($"UnityLLM: Preloaded {name}");
        }
    }

    // Static constructor gives explicit control over initialization order.
    // model/parameters must be initialised before any NPC calls CreateNPCContext().
    static UnityLLM()
    {
        PreloadBackendDlls();
        parameters = new ModelParams(modelPath)
        {
            ContextSize = 1024,
            GpuLayerCount = -1
        };
        model = LLamaWeights.LoadFromFile(parameters);
    }

    private async void Awake()
    {
        Instance = this;

        if (constData._llmDebug)
        {
#pragma warning disable CS0162
            // Legacy: startup test conversation for validating the TCP/server path
            var testContext = model.CreateContext(parameters);
            var testExec = new InteractiveExecutor(testContext);
            var testHistory = new ChatHistory();

            var samplingPipeline = new DefaultSamplingPipeline {
                                                        Temperature = 0.7f,
                                                        RepeatPenalty = 1.15f,
                                                        TopP = 0.9f,
                                                    };

            var antiPrompts = new List<string> { "<|eot_id|>", "User:", "Player:" };
            var testParams = new InferenceParams { MaxTokens = 256, AntiPrompts = antiPrompts, SamplingPipeline = samplingPipeline};

            testHistory.AddMessage(AuthorRole.System, "Transcript of a dialog, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.");
            testHistory.AddMessage(AuthorRole.User, "Hello, Bob.");
            testHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");

            ChatSession session = new(testExec, testHistory);
            string resp = string.Empty;
            await foreach (
                string text
                in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, "Can you write a poem about Unity?"), testParams)
            )
            {
                resp += text;
            }
            UnityEngine.Debug.Log("Response from UnityLLM----------------: " + resp);
#pragma warning restore CS0162
        }
        else
        {
#pragma warning disable CS0162
            UnityEngine.Debug.Log("UnityLLM: per-NPC context mode (llama.cpp). Shared model loaded.");
#pragma warning restore CS0162
        }
    }

    // Legacy: single shared context — TCP mode only
    public async Task<string> talk2LLM(string user)
    {
        if (constData._tcp)
        {
#pragma warning disable CS0162
            var freshContext = model.CreateContext(parameters);
            var freshExec = new InteractiveExecutor(freshContext);
            var cH = new ChatHistory();
            var legacyParams = new InferenceParams { MaxTokens = 256, AntiPrompts = new List<string> { "User:" } };
            cH.AddMessage(AuthorRole.System, "Give yourself a random personality and roleplay them");

            ChatSession session = new(freshExec, cH);
            string prompt = user.Length > 0 ? user : "Give yourself a random personality and roleplay them";
            string resp = string.Empty;
            await foreach (string text in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt), legacyParams))
            {
                resp += text;
            }
            return resp;
#pragma warning restore CS0162
        }
        return string.Empty;
    }

    // Per-NPC context factory — call once per NPC on Start()
    public static NPCContext CreateNPCContext(GUID npcId, string systemPrompt)
    {
        var npcLlamaContext = model.CreateContext(parameters);
        var executor = new InteractiveExecutor(npcLlamaContext);
        var history = new ChatHistory();
        history.AddMessage(AuthorRole.System, systemPrompt);
        return new NPCContext(
            npcId,
            history,
            executor,
            new InferenceParams { MaxTokens = 256, AntiPrompts = new List<string> { "User:" } },
            systemPrompt
        );
    }

    // Per-NPC inference — reuses the single ChatSession stored on the context so the
    // InteractiveExecutor KV cache is never replayed from scratch on each turn.
    public async Task<string> talk2LLMWithContext(NPCContext_intf ctx, string user)
    {
        string prompt = user.Length > 0 ? user : "Hello";
        string resp = string.Empty;
        await foreach (string text in ctx.Session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt), ctx.InferenceParams))
        {
            resp += text;
        }
        ctx.LastAccessed = DateTime.Now;
        return resp;
    }
}