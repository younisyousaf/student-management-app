using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace StudentManagementApp.WebApi.Controllers;

public sealed record CopilotHistoryMessageResponse(
    string Id,
    string Role,
    string Content,
    DateTimeOffset? CreatedAt);

public sealed record CopilotPendingApprovalResponse(
    string InterruptId,
    string ToolCallId,
    string ToolName,
    string Arguments,
    string Message);


[ApiController]
[Route("api/ag-ui/copilot")]
[Authorize(Roles = "Admin, User")]
public sealed class AGUICopilotHistoryController
    : ControllerBase
{
    private const string HostedCopilotAgentName =
        "student-management-copilot";

    private readonly IServiceProvider _serviceProvider;

    public AGUICopilotHistoryController(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpGet("history/{threadId}")]
    public async Task<ActionResult<
        IReadOnlyList<CopilotHistoryMessageResponse>>>
        GetHistory(
            string threadId,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message = "Thread ID is required."
            });
        }

        var agent =
            _serviceProvider
                .GetRequiredKeyedService<AIAgent>(
                    HostedCopilotAgentName);

        var sessionStore =
            _serviceProvider
                .GetRequiredKeyedService<AgentSessionStore>(
                    HostedCopilotAgentName);

        var session =
            await sessionStore.GetSessionAsync(
                agent,
                threadId,
                cancellationToken);

        var historyProvider =
            agent.GetService<InMemoryChatHistoryProvider>();

        if (historyProvider is null)
        {
            return Problem(
                title: "Copilot history is unavailable.",
                detail:
                    "The hosted Copilot does not expose an InMemoryChatHistoryProvider.");
        }

        var storedMessages =
            historyProvider.GetMessages(session);

        var response =
            storedMessages
                .Where(message =>
                    message.Role == ChatRole.User ||
                    message.Role == ChatRole.Assistant)
                .Select(
                    (message, index) =>
                        new CopilotHistoryMessageResponse(
                            message.MessageId
                                ?? $"history-{index}",

                            message.Role == ChatRole.User
                                ? "user"
                                : "assistant",

                            message.Text ?? string.Empty,

                            message.CreatedAt))
                .Where(message =>
                    !string.IsNullOrWhiteSpace(
                        message.Content))
                .ToList();

        return Ok(response);
    }

    [HttpGet("pending-approval/{threadId}")]
    public async Task<ActionResult<CopilotPendingApprovalResponse?>>
    GetPendingApproval(
        string threadId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message = "Thread ID is required."
            });
        }

        var agent =
            _serviceProvider
                .GetRequiredKeyedService<AIAgent>(
                    HostedCopilotAgentName);

        var sessionStore =
            _serviceProvider
                .GetRequiredKeyedService<AgentSessionStore>(
                    HostedCopilotAgentName);

        var session =
            await sessionStore.GetSessionAsync(
                agent,
                threadId,
                cancellationToken);

        var historyProvider =
            agent.GetService<
                InMemoryChatHistoryProvider>();

        if (historyProvider is null)
        {
            return Problem(
                title:
                    "Copilot approval state is unavailable.",
                detail:
                    "The hosted Copilot does not expose an InMemoryChatHistoryProvider.");
        }

        var storedMessages =
            historyProvider.GetMessages(
                session);

        var respondedRequestIds =
            storedMessages
                .SelectMany(
                    message =>
                        message.Contents)
                .OfType<
                    ToolApprovalResponseContent>()
                .Select(
                    response =>
                        response.RequestId)
                .ToHashSet(
                    StringComparer.Ordinal);

        var pendingRequest =
            storedMessages
                .SelectMany(
                    message =>
                        message.Contents)
                .OfType<
                    ToolApprovalRequestContent>()
                .LastOrDefault(
                    request =>
                        !respondedRequestIds.Contains(
                            request.RequestId));

        if (pendingRequest is null)
        {
            return Ok(
                (CopilotPendingApprovalResponse?)null);
        }

        if (
            pendingRequest.ToolCall
                is not FunctionCallContent functionCall)
        {
            return Problem(
                title:
                    "Invalid Copilot approval state.",
                detail:
                    "The pending approval does not contain a function call.");
        }

        var arguments =
            functionCall.Arguments is null
                ? "{}"
                : JsonSerializer.Serialize(
                    functionCall.Arguments);

        var response =
            new CopilotPendingApprovalResponse(
                InterruptId:
                    pendingRequest.RequestId,

                ToolCallId:
                    functionCall.CallId,

                ToolName:
                    functionCall.Name,

                Arguments:
                    arguments,

                Message:
                    $"Approval required for tool call: {functionCall.Name}");

        return Ok(response);
    }
}
