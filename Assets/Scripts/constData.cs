using UnityEngine;

public class constData
{
    public const bool _tcp = false;
    public const bool _llmDebug = true;

    public const int _USER_LLM_INPUT_CHAR_LIM = 240;
    public const int _USER_LLM_OUTPUT_TOKEN_LIM = 80;
    public const float _NPC_LLM_TEMPERATURE = 0.7f;
    public const float _NPC_LLM_REPEAT_PENALTY = 1.15f;
    public const float _NPC_LLM_NUCLEAS_SAMPLING = 0.9f;
    public const int _LLM_CONTEXT_SIZE_TOKENS = 1024; 
    public const int _LLM_NUM_GPU_LAYERS = -1;
    public const int _LLM_FILTER_BUFFER_NUM_CHARS = 8;
    public const int _LLM_MAX_TOKENS_GENERATED = 256;
}
