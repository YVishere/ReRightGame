using UnityEngine;
using UnityEditor;
using LLama;
using LLama.Common;
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

    public static ModelParams parameters = new ModelParams(modelPath)
    {
        ContextSize = 1024, // The longest length of chat as memory.
        GpuLayerCount = 5 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
    };

    public static LLamaWeights model = LLamaWeights.LoadFromFile(parameters);
    private async void Awake()
    {
        Instance = this;

        if (constData._tcp)
        {
#pragma warning disable CS0162
            // Legacy: startup test conversation for validating the TCP/server path
            var testContext = model.CreateContext(parameters);
            var testExec = new InteractiveExecutor(testContext);
            var testHistory = new ChatHistory();
            var testParams = new InferenceParams { MaxTokens = 256, AntiPrompts = new List<string> { "User:" } };

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
            UnityEngine.Debug.Log("UnityLLM: per-NPC context mode (llama.cpp). Shared model loaded.");
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

    // Per-NPC inference — uses the NPC's own context so histories never bleed
    public async Task<string> talk2LLMWithContext(NPCContext_intf ctx, string user)
    {
        ChatSession session = new(ctx.Executor, ctx.History);
        string prompt = user.Length > 0 ? user : "Hello";
        string resp = string.Empty;
        await foreach (string text in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt), ctx.InferenceParams))
        {
            resp += text;
        }
        ctx.LastAccessed = DateTime.Now;
        return resp;
    }
}