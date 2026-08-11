using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using StudentManagement.AI.Sessions;
using System.Text.Json;

namespace StudentManagement.AI.Services;

public class CopilotService : ICopilotService
{
    private readonly AIAgent _agent;
    private readonly ISessionStore _sessionStore;

    public CopilotService(
        AIAgent agent,
        ISessionStore sessionStore)
    {
        _agent = agent;
        _sessionStore = sessionStore;
    }

    public async Task<CopilotChatResult> SendMessageAsync(
        string message,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        AgentSession session;
        string resolvedSessionId;

        JsonElement? existingSession = null;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            existingSession = await _sessionStore.GetAsync(
                sessionId,
                cancellationToken);
        }

        if (existingSession is { } existing)
        {
            session = await _agent.DeserializeSessionAsync(
                existing,
                cancellationToken: cancellationToken);

            resolvedSessionId = sessionId!;
        }
        else
        {
            session = await _agent.CreateSessionAsync(
                cancellationToken);

            resolvedSessionId = Guid.NewGuid().ToString();
        }

        AgentResponse result = await _agent.RunAsync(
            message,
            session,
            cancellationToken: cancellationToken);

        // Look for a pending approval request.
        ToolApprovalRequestContent? approvalRequest =
        result.Messages
            .SelectMany(message => message.Contents)
            .OfType<ToolApprovalRequestContent>()
            .FirstOrDefault();

        // Always persist the AgentSession,
        // even when execution pauses for approval.
        JsonElement serialized =
            await _agent.SerializeSessionAsync(
                session,
                cancellationToken: cancellationToken);

        await _sessionStore.SaveAsync(
            resolvedSessionId,
            serialized,
            cancellationToken);

        if (approvalRequest is not null)
        {
            if (approvalRequest.ToolCall is not FunctionCallContent functionCall)
            {
                throw new InvalidOperationException(
                    "The approval request did not contain a function call.");
            }

            IReadOnlyDictionary<string, object?> arguments =
             functionCall.Arguments is not null
                 ? new Dictionary<string, object?>(
                     functionCall.Arguments)
                 : new Dictionary<string, object?>();

            await _sessionStore.SavePendingApprovalAsync(
                resolvedSessionId,
                new PendingToolApproval(
                    approvalRequest.RequestId,
                    functionCall.CallId,
                    functionCall.Name,
                    arguments),
                cancellationToken);

            return new CopilotChatResult(
                Response: null,
                SessionId: resolvedSessionId,
                RequiresApproval: true,
                Approval: new CopilotApprovalRequest(
                    RequestId: approvalRequest.RequestId,
                    FunctionName: functionCall.Name,
                    Arguments: arguments));
        }

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            return new CopilotChatResult(
                Response:
                    "The AI provider returned an empty response. " +
                    "Please retry the request.",
                SessionId: resolvedSessionId,
                RequiresApproval: false,
                Approval: null);
        }

        return new CopilotChatResult(
            Response: result.Text,
            SessionId: resolvedSessionId,
            RequiresApproval: false,
            Approval: null);
    }

    public async Task<CopilotApprovalResult> RespondToApprovalAsync(
    string sessionId,
    string requestId,
    bool approved,
    string? reason = null,
    CancellationToken cancellationToken = default)
    {
        // 1. Load the persisted MAF AgentSession
        JsonElement? serializedSession =
            await _sessionStore.GetAsync(
                sessionId,
                cancellationToken);

        if (serializedSession is null)
        {
            throw new KeyNotFoundException(
                "The requested agent session was not found.");
        }

        AgentSession session =
            await _agent.DeserializeSessionAsync(
                serializedSession.Value,
                cancellationToken: cancellationToken);

        // 2. Load the server-side pending approval
        PendingToolApproval? pendingApproval =
            await _sessionStore.GetPendingApprovalAsync(
                sessionId,
                cancellationToken);

        if (pendingApproval is null)
        {
            throw new InvalidOperationException(
                "This session has no pending tool approval.");
        }

        // 3. Make sure the client is responding to the correct request
        if (!string.Equals(
            pendingApproval.RequestId,
            requestId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The approval request does not match the pending approval.");
        }

        // 4. Reconstruct the exact function call
        var functionCall = new FunctionCallContent(
            pendingApproval.CallId,
            pendingApproval.FunctionName,
            new Dictionary<string, object?>(
                pendingApproval.Arguments));

        // 5. Create the correlated approval response
        var approvalResponse =
            new ToolApprovalResponseContent(
                pendingApproval.RequestId,
                approved,
                functionCall);

        // 6. Send only the approval response back to MAF
        var approvalMessage =
            new ChatMessage(
                ChatRole.User,
                [
                    approvalResponse
                ]);

        AgentResponse result =
            await _agent.RunAsync(
                approvalMessage,
                session,
                cancellationToken: cancellationToken);

        // 7. Persist the updated session
        JsonElement updatedSerializedSession =
            await _agent.SerializeSessionAsync(
                session,
                cancellationToken: cancellationToken);

        await _sessionStore.SaveAsync(
            sessionId,
            updatedSerializedSession,
            cancellationToken);

        // 8. Approval is now consumed
        await _sessionStore.ClearPendingApprovalAsync(
            sessionId,
            cancellationToken);

        return new CopilotApprovalResult(
            Response: result.Text,
            SessionId: sessionId,
            Approved: approved);
    }
}