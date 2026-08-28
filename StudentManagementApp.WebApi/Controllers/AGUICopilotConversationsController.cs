using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI.Hosting;
using StudentManagement.Core.Models;
using StudentManagementApp.WebApi.AGUI;
using StudentManagementApp.WebApi.DTOs;
using Microsoft.AspNetCore.Authorization;
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

public sealed record RenameCopilotConversationRequest(
    string Title);

public sealed record PrepareCopilotTurnRequest(
    string ThreadId,
    string MessageId,
    string Message);

public sealed record CopilotTurnActivityRequest(
    string Id,
    string ToolName,
    string Status);

public sealed record StopCopilotTurnRequest(
    string UserMessageId,
    IReadOnlyList<CopilotTurnActivityRequest> Activities);

public sealed record CopilotTurnActivityResponse(
    string Id,
    string ToolName,
    string Status);

public sealed record CopilotTurnResponse(
    string UserMessageId,
    string Status,
    IReadOnlyList<CopilotTurnActivityResponse> Activities,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CompleteCopilotTurnRequest(
    string UserMessageId);

public sealed record RetryCopilotTurnRequest(
    string UserMessageId);

public sealed record EditCopilotTurnRequest(
    string UserMessageId,
    string Message);


[ApiController]
[Route("api/ag-ui/copilot/conversations")]
[Authorize(Roles = "Admin, User")]
public sealed class AGUICopilotConversationsController
    : ControllerBase
{
    private readonly CopilotConversationStore
        _conversationStore;
    private const string HostedCopilotAgentName = "student-management-copilot";
    private readonly AIAgent _agent;
    private readonly AgentSessionStore _sessionStore;
    private readonly CopilotTurnStore _turnStore;
    public AGUICopilotConversationsController(
    CopilotConversationStore conversationStore, CopilotTurnStore turnStore,

    [FromKeyedServices(
        HostedCopilotAgentName)]
    AIAgent agent,

    [FromKeyedServices(
        HostedCopilotAgentName)]
    AgentSessionStore sessionStore)
    {
        _conversationStore = conversationStore;
        _turnStore = turnStore;
        _agent = agent;
        _sessionStore = sessionStore;
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

    [HttpPost("prepare-turn")]
    public async Task<ActionResult<
    CopilotConversationResponse>>
    PrepareTurn(
        PrepareCopilotTurnRequest request,
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
                request.MessageId)
        )
        {
            return BadRequest(new
            {
                Message =
                    "Message ID is required."
            });
        }

        if (
            string.IsNullOrWhiteSpace(
                request.Message)
        )
        {
            return BadRequest(new
            {
                Message =
                    "Message is required."
            });
        }

        /*
         * Load the persisted MAF session.
         *
         * The keyed AgentSessionStore already
         * applies claims-based user isolation.
         */
        var session =
            await _sessionStore
                .GetSessionAsync(
                    _agent,
                    request.ThreadId,
                    cancellationToken);

        var historyProvider =
            _agent.GetService<
                InMemoryChatHistoryProvider>();

        if (historyProvider is null)
        {
            return Problem(
                title:
                    "Copilot history is unavailable.",
                detail:
                    "The hosted Copilot does not expose an InMemoryChatHistoryProvider.");
        }

        var storedMessages =
            historyProvider.GetMessages(
                session);

        /*
         * Make prepare-turn idempotent.
         *
         * If the browser retries the same request,
         * don't persist the same user message twice.
         */
        var existingMessage =
            storedMessages
                .FirstOrDefault(
                    message =>
                        string.Equals(
                            message.MessageId,
                            request.MessageId,
                            StringComparison.Ordinal));

        if (existingMessage is null)
        {
            var userMessage =
                new ChatMessage(
                    ChatRole.User,
                    request.Message)
                {
                    MessageId =
                        request.MessageId,

                    CreatedAt =
                        DateTimeOffset.UtcNow
                };

            storedMessages.Add(
                userMessage);

            /*
             * Persist BEFORE starting AG-UI.
             *
             * Therefore Stop Generation cannot
             * erase this user turn.
             */
            await _sessionStore
                .SaveSessionAsync(
                    _agent,
                    request.ThreadId,
                    session,
                    cancellationToken);
        }
        else if (
            !string.Equals(
                existingMessage.Text,
                request.Message,
                StringComparison.Ordinal)
        )
        {
            return Conflict(new
            {
                Message =
                    "The message ID already belongs to a different message."
            });
        }

        /*
         * Create/update the sidebar index only
         * after the MAF session has been saved.
         */
        var conversation =
            await _conversationStore
                .EnsureConversationAsync(
                    request.ThreadId,
                    request.Message,
                    cancellationToken);

        await _turnStore.EnsurePreparedAsync(
            request.ThreadId,
            request.MessageId,
            cancellationToken);

        return Ok(
            new CopilotConversationResponse(
                conversation.ThreadId,
                conversation.Title,
                conversation.LastRunId,
                conversation.CreatedAt,
                conversation.UpdatedAt));
    }

    [HttpPost("{threadId}/stop-turn")]
    public async Task<ActionResult<CopilotTurnResponse>> StopTurn(
    string threadId,
    StopCopilotTurnRequest request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message = "Thread ID is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.UserMessageId))
        {
            return BadRequest(new
            {
                Message = "User message ID is required."
            });
        }

        var activities = request.Activities
            .Select(activity =>
                new CopilotTurnActivityResponse(
                    activity.Id,
                    activity.ToolName,
                    activity.Status == "running"
                        ? "stopped"
                        : activity.Status))
            .ToList();

        string activitiesJson = JsonSerializer.Serialize(activities);

        /*
         * Load the authoritative persisted MAF session.
         */
        var session = await _sessionStore.GetSessionAsync(
            _agent,
            threadId,
            cancellationToken);

        var historyProvider = _agent.GetService<InMemoryChatHistoryProvider>();

        if (historyProvider is null)
        {
            return Problem(
                title: "Copilot history is unavailable.",
                detail: "The hosted Copilot does not expose an InMemoryChatHistoryProvider.");
        }

        var storedMessages = historyProvider.GetMessages(session);

        /*
         * Close the stopped user turn semantically.
         *
         * Without this marker MAF sees:
         *
         * User: previous request
         * User: next request
         *
         * and may try to finish the unanswered request.
         */
        string stopMarkerId = CopilotSessionMarkers.CreateStoppedMessageId(
            request.UserMessageId);

        bool markerExists = storedMessages.Any(message =>
            string.Equals(
                message.MessageId,
                stopMarkerId,
                StringComparison.Ordinal));

        if (!markerExists)
        {
            var stoppedMessage = new ChatMessage(
                ChatRole.Assistant,
                CopilotSessionMarkers.StoppedMessageText)
            {
                MessageId = stopMarkerId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            storedMessages.Add(stoppedMessage);

            await _sessionStore.SaveSessionAsync(
                _agent,
                threadId,
                session,
                cancellationToken);
        }

        /*
         * Persist the UI/runtime representation separately.
         */
        var turn = await _turnStore.MarkStoppedAsync(
            threadId,
            request.UserMessageId,
            activitiesJson,
            cancellationToken);

        if (turn is null)
        {
            return NotFound(new
            {
                Message = "Copilot turn was not found."
            });
        }

        return Ok(
            new CopilotTurnResponse(
                turn.UserMessageId,
                turn.Status.ToString(),
                activities,
                turn.CreatedAt,
                turn.UpdatedAt));
    }

    [HttpPost("{threadId}/retry-turn")]
    public async Task<ActionResult<CopilotTurnResponse>> RetryTurn(
    string threadId,
    RetryCopilotTurnRequest request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message = "Thread ID is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.UserMessageId))
        {
            return BadRequest(new
            {
                Message = "User message ID is required."
            });
        }

        var session = await _sessionStore.GetSessionAsync(
            _agent,
            threadId,
            cancellationToken);

        var historyProvider = _agent.GetService<InMemoryChatHistoryProvider>();

        if (historyProvider is null)
        {
            return Problem(
                title: "Copilot history is unavailable.",
                detail: "The hosted Copilot does not expose an InMemoryChatHistoryProvider.");
        }

        var storedMessages = historyProvider.GetMessages(session);

        int userMessageIndex = storedMessages.FindIndex(message =>
            string.Equals(
                message.MessageId,
                request.UserMessageId,
                StringComparison.Ordinal));

        if (userMessageIndex < 0)
        {
            return NotFound(new
            {
                Message = "The original user message was not found."
            });
        }

        var userMessage = storedMessages[userMessageIndex];

        if (userMessage.Role != ChatRole.User)
        {
            return Conflict(new
            {
                Message = "The retry target is not a user message."
            });
        }

        bool hasNewerUserMessage = storedMessages
            .Skip(userMessageIndex + 1)
            .Any(message => message.Role == ChatRole.User);

        if (hasNewerUserMessage)
        {
            return Conflict(new
            {
                Message = "Only the latest stopped request can be retried."
            });
        }

        string stopMarkerId = CopilotSessionMarkers.CreateStoppedMessageId(
            request.UserMessageId);

        int removedMarkers = storedMessages.RemoveAll(message =>
            string.Equals(
                message.MessageId,
                stopMarkerId,
                StringComparison.Ordinal));

        if (removedMarkers == 0)
        {
            return Conflict(new
            {
                Message = "The stopped marker for this request was not found."
            });
        }

        var turn = await _turnStore.MarkPreparedForRerunAsync(
            threadId,
            request.UserMessageId,
            cancellationToken);

        if (turn is null)
        {
            return Conflict(new
            {
                Message = "Only the latest stopped Copilot turn can be retried."
            });
        }

        /*
         * The original user message remains in history.
         * We remove only the internal Assistant stopped marker.
         *
         * The next AG-UI run therefore sees the session ending with
         * the original User message and can answer it again.
         */
        historyProvider.SetMessages(
            session,
            storedMessages);

        await _sessionStore.SaveSessionAsync(
            _agent,
            threadId,
            session,
            cancellationToken);

        return Ok(
            new CopilotTurnResponse(
                turn.UserMessageId,
                turn.Status.ToString(),
                [],
                turn.CreatedAt,
                turn.UpdatedAt));
    }

    [HttpPost("{threadId}/edit-turn")]
    public async Task<ActionResult<CopilotTurnResponse>> EditTurn(
    string threadId,
    EditCopilotTurnRequest request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message = "Thread ID is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.UserMessageId))
        {
            return BadRequest(new
            {
                Message = "User message ID is required."
            });
        }

        string editedMessage = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(editedMessage))
        {
            return BadRequest(new
            {
                Message = "Edited message is required."
            });
        }

        var session = await _sessionStore.GetSessionAsync(
            _agent,
            threadId,
            cancellationToken);

        var historyProvider = _agent.GetService<InMemoryChatHistoryProvider>();

        if (historyProvider is null)
        {
            return Problem(
                title: "Copilot history is unavailable.",
                detail: "The hosted Copilot does not expose an InMemoryChatHistoryProvider.");
        }

        var storedMessages = historyProvider.GetMessages(session);

        int userMessageIndex = storedMessages.FindIndex(message =>
            string.Equals(
                message.MessageId,
                request.UserMessageId,
                StringComparison.Ordinal));

        if (userMessageIndex < 0)
        {
            return NotFound(new
            {
                Message = "The original user message was not found."
            });
        }

        var userMessage = storedMessages[userMessageIndex];

        if (userMessage.Role != ChatRole.User)
        {
            return Conflict(new
            {
                Message = "The edit target is not a user message."
            });
        }

        bool hasNewerUserMessage = storedMessages
            .Skip(userMessageIndex + 1)
            .Any(message => message.Role == ChatRole.User);

        if (hasNewerUserMessage)
        {
            return Conflict(new
            {
                Message = "Only the latest interrupted request can be edited."
            });
        }

        string stopMarkerId = CopilotSessionMarkers.CreateStoppedMessageId(
            request.UserMessageId);

        int stopMarkerIndex = storedMessages.FindIndex(message =>
            string.Equals(
                message.MessageId,
                stopMarkerId,
                StringComparison.Ordinal));

        if (stopMarkerIndex < 0)
        {
            return Conflict(new
            {
                Message = "The stopped marker for this request was not found."
            });
        }

        var turn = await _turnStore.MarkPreparedForRerunAsync(
            threadId,
            request.UserMessageId,
            cancellationToken);

        if (turn is null)
        {
            return Conflict(new
            {
                Message = "Only the latest interrupted Copilot turn can be edited."
            });
        }

        /*
         * Keep the same user MessageId because this is an edit
         * of the existing turn, not a new conversation turn.
         */
        userMessage.Contents =
        [
            new TextContent(editedMessage)
        ];

        /*
         * The stopped request is being reopened, so remove
         * the internal assistant marker that closed it.
         */
        storedMessages.RemoveAt(stopMarkerIndex);

        historyProvider.SetMessages(
            session,
            storedMessages);

        await _sessionStore.SaveSessionAsync(
            _agent,
            threadId,
            session,
            cancellationToken);

        return Ok(
            new CopilotTurnResponse(
                turn.UserMessageId,
                turn.Status.ToString(),
                [],
                turn.CreatedAt,
                turn.UpdatedAt));
    }

    [HttpPost("{threadId}/complete-turn")]
    public async Task<IActionResult> CompleteTurn(
    string threadId,
    CompleteCopilotTurnRequest request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new
            {
                Message = "Thread ID is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.UserMessageId))
        {
            return BadRequest(new
            {
                Message = "User message ID is required."
            });
        }

        var turn = await _turnStore.MarkCompletedAsync(
            threadId,
            request.UserMessageId,
            cancellationToken);

        if (turn is null)
        {
            return NotFound(new
            {
                Message = "Copilot turn was not found."
            });
        }

        return NoContent();
    }

    [HttpGet("{threadId}/turns")]
    public async Task<ActionResult<IReadOnlyList<CopilotTurnResponse>>> GetTurns(
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

        var turns = await _turnStore.GetByThreadAsync(
            threadId,
            cancellationToken);

        var response = turns
            .Select(turn =>
            {
                var activities =
                    JsonSerializer.Deserialize<List<CopilotTurnActivityResponse>>(
                        turn.ActivitiesJson)
                    ?? [];

                return new CopilotTurnResponse(
                    turn.UserMessageId,
                    turn.Status.ToString(),
                    activities,
                    turn.CreatedAt,
                    turn.UpdatedAt);
            })
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
        await _sessionStore.DeleteSessionAsync(
            _agent,
            threadId,
            cancellationToken);

        await _turnStore.DeleteByThreadAsync(
            threadId,
            cancellationToken);

        await _conversationStore.DeleteAsync(
            threadId,
            cancellationToken);


        return NoContent();
    }
}