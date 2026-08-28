using Microsoft.Extensions.AI;

namespace StudentManagementApp.WebApi.AGUI;

public static class CopilotSessionMarkers
{
    private const string StoppedMessageIdPrefix = "smartcampus-stopped:";

    public const string StoppedMessageText =
        "The user's previous request was stopped before completion. " +
        "Treat that request as closed. Do not continue, retry, resume, or execute it unless the user explicitly asks you to do so. " +
        "You may still answer questions about what the stopped request was.";

    public static string CreateStoppedMessageId(string userMessageId)
    {
        return $"{StoppedMessageIdPrefix}{userMessageId}";
    }

    public static bool IsStoppedMessage(ChatMessage message)
    {
        return message.MessageId?.StartsWith(
            StoppedMessageIdPrefix,
            StringComparison.Ordinal) == true;
    }
}