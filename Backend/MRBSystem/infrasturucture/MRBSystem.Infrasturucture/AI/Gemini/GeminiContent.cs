namespace MRBSystem.Infrasturucture.AI.Gemini;

internal class GeminiContent
{
    public string? Role { get; set; }
    public List<GeminiPart> Parts { get; set; } = new();
}
