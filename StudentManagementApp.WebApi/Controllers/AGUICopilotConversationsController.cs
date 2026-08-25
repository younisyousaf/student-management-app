using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementApp.WebApi.Services;

namespace StudentManagementApp.WebApi.Controllers;

public sealed record CopilotConversationResponse(
    string ThreadId,
    string Title,
    string? LastRunId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SaveCopilotConversationRunRequest(
    string ThreadId,
    string RunId,
    string? Title);

[ApiController]
[Route("api/ag-ui/copilot/conversations")]
[Authorize(Roles = "Admin, User")]
public sealed class AGUICopilotConversationsController
    : ControllerBase
{
    private readonly CopilotConversationStore
        _conversationStore;

    public AGUICopilotConversationsController(
        CopilotConversationStore conversationStore)
    {
        _conversationStore =
            conversationStore;
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<CopilotConversationResponse>>>
        GetConversations(
            CancellationToken cancellationToken)
    {
        var conversations =
            await _conversationStore
                .GetAllAsync(
                    cancellationToken);

        var response =
            conversations
                .Select(
                    conversation =>
                        new CopilotConversationResponse(
                            conversation.ThreadId,
                            conversation.Title,
                            conversation.LastRunId,
                            conversation.CreatedAt,
                            conversation.UpdatedAt))
                .ToList();

        return Ok(response);
    }

    [HttpPost("run")]
    public async Task<ActionResult<
        CopilotConversationResponse>>
        SaveRun(
            SaveCopilotConversationRunRequest request,
            CancellationToken cancellationToken)
    {
        if (
            string.IsNullOrWhiteSpace(
                request.ThreadId)
        )
        {
            return BadRequest(new
            {
                Message =
                    "Thread ID is required."
            });
        }

        if (
            string.IsNullOrWhiteSpace(
                request.RunId)
        )
        {
            return BadRequest(new
            {
                Message =
                    "Run ID is required."
            });
        }

        var conversation =
            await _conversationStore
                .SaveRunAsync(
                    request.ThreadId,
                    request.RunId,
                    request.Title,
                    cancellationToken);

        return Ok(
            new CopilotConversationResponse(
                conversation.ThreadId,
                conversation.Title,
                conversation.LastRunId,
                conversation.CreatedAt,
                conversation.UpdatedAt));
    }
}