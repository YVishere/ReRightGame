using UnityEngine;
using LLama;
using LLama.Common;
using LLama.Transformers;
using System;
using UnityEditor;

public class NPCContext : NPCContext_intf
{
    public GUID NpcId { get; set; }
    public ChatHistory History { get; set; }
    public InteractiveExecutor Executor { get; set; }
    public ChatSession Session { get; set; }
    public InferenceParams InferenceParams { get; set; }
    public string SystemPrompt { get; set; }
    public DateTime LastAccessed { get; set; }

    public NPCContext(GUID npcId, ChatHistory history, InteractiveExecutor executor, InferenceParams inferenceParams, string systemPrompt)
    {
        NpcId = npcId;
        History = history;
        Executor = executor;
        Session = new ChatSession(executor, history); // created once; reused every turn
        Session.WithHistoryTransform(new PromptTemplateTransformer(UnityLLM.model, withAssistant: true));
        Session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
                                                new[] { "User:", "Assistant:", "Player:", "Server:", "<|eot_id|>", "<|start_header_id|>" },
                                                redundancyLength: 8));
        InferenceParams = inferenceParams;
        SystemPrompt = systemPrompt;
        LastAccessed = DateTime.Now;
    }

    public void updateNPC()
    {
        LastAccessed = DateTime.Now;
    }

    private void OnDestroy()
    {
        Close();
    }

    public void Close()
    {
        Session = null;
        Executor = null;
        History = null;
        Debug.Log("NPCContext closed for NPC ID: " + NpcId);
        LastAccessed = DateTime.MinValue;
    }
}