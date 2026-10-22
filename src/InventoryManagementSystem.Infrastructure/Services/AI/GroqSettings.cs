namespace InventoryManagementSystem.Infrastructure.Services.AI;

public sealed class GroqSettings
{
    public const string SectionName = "Ai:Groq";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "qwen/qwen3.8-27b";
}
