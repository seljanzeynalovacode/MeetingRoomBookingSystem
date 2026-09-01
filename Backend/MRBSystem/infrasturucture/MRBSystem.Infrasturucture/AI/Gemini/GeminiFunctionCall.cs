using System.Text.Json;

namespace MRBSystem.Infrasturucture.AI.Gemini;

internal class GeminiFunctionCall
{
    public string Name { get; set; } = null!;
    public JsonElement Args { get; set; }
}
