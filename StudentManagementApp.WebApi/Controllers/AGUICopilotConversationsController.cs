using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementApp.WebApi.DTOs;
using StudentManagementApp.WebApi.Services;
using StudentManagement.Core.Models;

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

public sealed record RenameCopilotConversationRequest(
    string Title);

[ApiController]
[Route("api/ag-ui/copilot/conversations")]
[Authorize(Roles = "Admin, User")]
public sealed class AGUICopilotConversationsController
    : ControllerBase
{
    private readonly CopilotConversationStore
        _conversationStore;
    private const string HostedCopilotAgentName =
    "student-management-copilot";

    private readonly AIAgent _agent;

    private readonly AgentSessionStore
        _sessionStore;

    public AGUICopilotConversationsController(
    CopilotConversationStore conversationStore,

    [FromKeyedServices(
        HostedCopilotAgentName)]
    AIAgent agent,

    [FromKeyedServices(
        HostedCopilotAgentName)]
    AgentSessionStore sessionStore)
    {
        _conversationStore =
            conversationStore;

        _agent =
            agent;

        _sessionStore =
            sessionStore;
    }

    [HttpGet]
    public async Task<ActionResult<
    PaginatedResult<CopilotConversationResponse>>>
    GetConversations(
        [FromQuery]
        PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var result =
            await _conversationStore
                .GetPageAsync(
                    pagination.PageNumber,
                    pagination.PageSize,
                    cancellationToken);

        var conversations =
            result.Items
                .Select(
                    conversation =>
                        new CopilotConversationResponse(
                            conversation.ThreadId,
                            conversation.Title,
                            conversation.LastRunId,
                            conversation.CreatedAt,
                            conversation.UpdatedAt))
                .ToList();

        var response =
            new PaginatedResult<
                CopilotConversationResponse>(
                    conversations,
                    result.PageNumber,
                    result.PageSize,
                    result.TotalCount);

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

    [HttpPatch("{threadId}/title")]
    public async Task<ActionResult<
    CopilotConversationResponse>>
    RenameConversation(
        string threadId,
        RenameCopilotConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message =
                    "Thread ID is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
            request.Title))
        {
            return BadRequest(new
            {
                Message =
                    "Conversation title is required."
            });
        }

        var conversation =
            await _conversationStore
                .RenameAsync(
                    threadId,
                    request.Title,
                    cancellationToken);

        if (conversation is null)
        {
            return NotFound(new
            {
                Message =
                    "Conversation was not found."
            });
        }

        return Ok(
            new CopilotConversationResponse(
                conversation.ThreadId,
                conversation.Title,
                conversation.LastRunId,
                conversation.CreatedAt,
                conversation.UpdatedAt));
    }

    [HttpDelete("{threadId}")]
    public async Task<IActionResult>
    DeleteConversation(
        string threadId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message =
                    "Thread ID is required."
            });
        }

        var conversation =
            await _conversationStore
                .GetByThreadIdAsync(
                    threadId,
                    cancellationToken);

        if (conversation is null)
        {
            return NotFound(new
            {
                Message =
                    "Conversation was not found."
            });
        }

        /*
         * Delete the actual persisted MAF session
         * first.
         *
         * The keyed AgentSessionStore already
         * applies claims-based user isolation.
         */
        await _sessionStore
            .DeleteSessionAsync(
                _agent,
                threadId,
                cancellationToken);

        await _conversationStore
            .DeleteAsync(
                threadId,
                cancellationToken);

        return NoContent();
    }
}