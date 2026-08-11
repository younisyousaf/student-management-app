using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using StudentManagement.AI.Agents;
using StudentManagement.AI.Configuration;
using StudentManagement.AI.Services;
using StudentManagement.AI.Sessions;
using StudentManagement.AI.Context;
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

            //var apiKey = !string.IsNullOrWhiteSpace(options.ApiKey)
            //    ? options.ApiKey
            //    : Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

            var apiKey = new[] { 
                //options.ApiKey,
                //options.ApiKeyTwo, 
                options.ApiKeyThree
            }
                .FirstOrDefault(k => !string.IsNullOrWhiteSpace(k))
                ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

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
        services.AddScoped<ICopilotService, CopilotService>();
        //services.AddSingleton<ISessionStore, InMemorySessionStore>();

        services.AddScoped<StudentTools>();
        services.AddScoped<CourseTools>();
        services.AddScoped<EnrollmentTools>();
        services.AddScoped<AttendanceTools>();
        services.AddScoped<FeeTools>();
        services.AddScoped<AuthenticatedUserContextProvider>();

       
        services.AddScoped<AIAgent>(sp =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var studentTools = sp.GetRequiredService<StudentTools>();
            var courseTools = sp.GetRequiredService<CourseTools>();
            var enrollmentTools = sp.GetRequiredService<EnrollmentTools>();
            var attendanceTools = sp.GetRequiredService<AttendanceTools>();
            var feeTools = sp.GetRequiredService<FeeTools>();
            var authenticatedUserContext = sp.GetRequiredService<AuthenticatedUserContextProvider>();

            IList<AITool> tools =
            [
                AIFunctionFactory.Create(studentTools.GetStudentByRollNumber),
                AIFunctionFactory.Create(studentTools.SearchStudentsByName),
                AIFunctionFactory.Create(courseTools.GetCourseByCode),
                AIFunctionFactory.Create(courseTools.GetAllCourses),
                AIFunctionFactory.Create(enrollmentTools.GetEnrollmentsByStudent),
                AIFunctionFactory.Create(enrollmentTools.GetEnrollmentById),
                AIFunctionFactory.Create(attendanceTools.GetAttendanceForStudent),
                AIFunctionFactory.Create(attendanceTools.GetAttendanceForCourseOnDate),
                AIFunctionFactory.Create(attendanceTools.GetAttendanceById),
                AIFunctionFactory.Create(attendanceTools.GetAttendanceSummaryForStudent),
                AIFunctionFactory.Create(feeTools.GetFeeById),
                AIFunctionFactory.Create(feeTools.GetFeeStatement)
            ];

            return StudentManagementAgent.Create(chatClient, tools, authenticatedUserContext);
        });

        return services;
    }
}