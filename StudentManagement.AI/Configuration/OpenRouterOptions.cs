namespace StudentManagement.AI.Configuration;

public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyTwo { get; set; } = string.Empty;
    public string ApiKeyThree { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string Model { get; set; } = "openrouter/free";
    public int TimeoutSeconds { get; set; } = 85;
}