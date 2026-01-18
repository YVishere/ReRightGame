using UnityEngine;
using LLama;
using LLama.Common;
using System;
using UnityEditor;

public interface NPCContext_intf
{
    GUID NpcId { get; set; }
    ChatHistory History { get; set; }
    InteractiveExecutor Executor { get; set; }
    InferenceParams InferenceParams { get; set; }
    string SystemPrompt { get; set; }
    DateTime LastAccessed { get; set; }

    public void Close();
}