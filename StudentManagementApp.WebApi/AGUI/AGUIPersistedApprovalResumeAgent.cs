using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace StudentManagementApp.WebApi.AGUI;

public sealed class AGUIPersistedApprovalResumeAgent : DelegatingAIAgent
{
    public AGUIPersistedApprovalResumeAgent(AIAgent innerAgent)
        : base(innerAgent)
    {
    }

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var filteredMessages =
            RemoveSyntheticApprovalRequests(messages);

        return InnerAgent.RunAsync(
            filteredMessages,
            session,
            options,
            cancellationToken);
    }

    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var filteredMessages =
            RemoveSyntheticApprovalRequests(messages);

        return InnerAgent.RunStreamingAsync(
            filteredMessages,
            session,
            options,
            cancellationToken);
    }

    private static IReadOnlyList<ChatMessage> RemoveSyntheticApprovalRequests(
        IEnumerable<ChatMessage> messages)
    {
        var messageList = messages.ToList();

        var approvalResponses = messageList
            .SelectMany(message => message.Contents)
            .OfType<ToolApprovalResponseContent>()
            .Where(response => response.ToolCall is FunctionCallContent)
            .ToDictionary(
                response => response.RequestId,
                response => (FunctionCallContent)response.ToolCall,
                StringComparer.Ordinal);

        if (approvalResponses.Count == 0)
        {
            return messageList;
        }

        var filteredMessages = new List<ChatMessage>();

        foreach (var message in messageList)
        {
            var filteredContents = new List<AIContent>();

            foreach (var content in message.Contents)
            {
                if (content is ToolApprovalRequestContent request &&
                    request.ToolCall is FunctionCallContent requestCall &&
                    approvalResponses.TryGetValue(
                        request.RequestId,
                        out var responseCall) &&
                    string.Equals(
                        requestCall.CallId,
                        responseCall.CallId,
                        StringComparison.Ordinal))
                {
                    // AG-UI reconstructs both the approval request and response
                    // when decoding a resume payload.
                    //
                    // With a persisted AgentSession, the original approval
                    // request already exists in session history.
                    //
                    // Keep the incoming response, but remove this duplicate
                    // synthetic request.
                    continue;
                }

                filteredContents.Add(content);
            }

            if (filteredContents.Count == 0)
            {
                continue;
            }

            if (filteredContents.Count == message.Contents.Count)
            {
                filteredMessages.Add(message);
                continue;
            }

            var clonedMessage = message.Clone();
            clonedMessage.Contents = filteredContents;

            filteredMessages.Add(clonedMessage);
        }

        return filteredMessages;
    }
}
