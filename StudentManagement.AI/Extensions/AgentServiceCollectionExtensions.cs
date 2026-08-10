using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using StudentManagement.AI.Agents;
using StudentManagement.AI.Configuration;
using StudentManagement.AI.Tools;
using System.ClientModel;

namespace StudentManagement.AI.Extensions;

public static class AgentServiceCollectionExtensions
{
    public static IServiceCollection AddStudentManagementAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenRouterOptions>(configuration.GetSection(OpenRouterOptions.SectionName));

        services.AddSingleton<IChatClient>(_ =>
        {
            var options = configuration.GetSection(OpenRouterOptions.SectionName).Get<OpenRouterOptions>()
                ?? new OpenRouterOptions();

            var apiKey = !string.IsNullOrWhiteSpace(options.ApiKey)
                ? options.ApiKey
                : Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "OpenRouter API key not found. Set it via User Secrets (OpenRouter:ApiKey) or the OPENROUTER_API_KEY environment variable.");
            }

            var openAiClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(options.BaseUrl) });

            return openAiClient.GetChatClient(options.Model).AsIChatClient();
        });

        services.AddScoped<StudentTools>();

        services.AddScoped<AIAgent>(sp =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var studentTools = sp.GetRequiredService<StudentTools>();

            IList<AITool> tools =
            [
                AIFunctionFactory.Create(studentTools.GetStudentByRollNumber),
            AIFunctionFactory.Create(studentTools.SearchStudentsByName)
            ];

            return StudentManagementAgent.Create(chatClient, tools);
        });

        return services;
    }
}