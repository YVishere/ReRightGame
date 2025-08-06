# Simple test version of llamaModelFile without external dependencies
class llamaModel:
    def __init__(self):
        self.device = "cpu"  # Simple fallback
        print("LlamaModel initialized (test version)")

    def invoke(self, sendText, context=None):
        # Simple echo response for testing
        if context is None:
            response = f"Echo: {sendText}"
        else:
            response = f"Echo with context [{context}]: {sendText}"
        
        return response
