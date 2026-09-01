namespace MRBSystem.Infrasturucture.AI.Gemini;

internal class GeminiPart
{
    public string? Text { get; set; }
    public GeminiFunctionCall? FunctionCall { get; set; }
    public object? FunctionResponse { get; set; }
    public string? ThoughtSignature { get; set; }
}
