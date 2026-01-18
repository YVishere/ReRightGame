using UnityEngine;
using LLama;
using LLama.Common;
using Mono.Cecil.Cil;
using UnityEditor.Rendering.LookDev;
using System.Collections.Generic;
using System.Threading.Tasks;

// Unity Script to act as a single point of truth for LLM model and context
class UnityLLM : MonoBehaviour
{
    public static UnityLLM Instance { get; private set; }
    private static string modelPath = @"Assets\StreamingAssets\Models\models--unsloth--Llama-3.2-1B-Instruct-GGUF\snapshots\b69aef112e9f895e6f98d7ae0949f72ff09aa401\Llama-3.2-1B-Instruct-Q4_K_M.gguf";

    private static ModelParams parameters = new ModelParams(modelPath)
    {
        ContextSize = 1024, // The longest length of chat as memory.
        GpuLayerCount = 5 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
    };

    private static LLamaWeights model = LLamaWeights.LoadFromFile(parameters);

    private static LLamaContext context = model.CreateContext(parameters);

    private InteractiveExecutor executor = new InteractiveExecutor(context);

    private ChatHistory chatHistory = new ChatHistory();

    private InferenceParams inferenceParams = new InferenceParams()
    {
        MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
        AntiPrompts = new List<string> { "User:" } // Stop generation once antiprompts appear.
    };
    private async Task Awake()
    {
        Instance = this;

        //Load the model
        chatHistory.AddMessage(AuthorRole.System, "Transcript of a dialog, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.");
        chatHistory.AddMessage(AuthorRole.User, "Hello, Bob.");
        chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");

        ChatSession session = new(executor, chatHistory);
        string resp = string.Empty;
        await foreach (
            string text 
            in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, "Can you write a poem about Unity?"), inferenceParams)
        )
        {
            resp += text;
        }

        UnityEngine.Debug.Log("Response from UnityLLM----------------: " + resp);
    }

    // Add UnityLLM specific methods and properties here
}