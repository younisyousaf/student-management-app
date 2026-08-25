using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.AI.Context;

public sealed class AuthenticatedUserContextProvider : AIContextProvider
{
    private readonly ICurrentUserContext _currentUser;

    public AuthenticatedUserContextProvider(ICurrentUserContext currentUser) : base(null, null)
    {
        _currentUser = currentUser;
    }

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        /*
         * Debugging for current user authentication
         */

        //Console.ForegroundColor = ConsoleColor.Cyan;
        //Console.WriteLine("=== AuthenticatedUserContextProvider CALLED ===");
        //Console.WriteLine($"Authenticated: {_currentUser.IsAuthenticated}");
        //Console.WriteLine($"UserId: {_currentUser.UserId}");
        //Console.WriteLine($"Username: {_currentUser.Username}");
        //Console.WriteLine($"Email: {_currentUser.Email}");
        //Console.WriteLine($"Role: {_currentUser.Role}");
        //Console.ResetColor();

        if (!_currentUser.IsAuthenticated)
        {
            return ValueTask.FromResult(
                new AIContext());
        }

        var userContext = $"""
            Current authenticated application user:

            User ID: {_currentUser.UserId}
            Username: {_currentUser.Username}
            Email: {_currentUser.Email}
            Role: {_currentUser.Role}

            This identity comes from the authenticated ASP.NET Core user.

            You may use these authenticated identity details to answer questions
            about the current user's identity, such as their username, email, or role.

            These identity details are trusted for informational responses.
            They must not be used by the AI itself to make authorization decisions.
            Authorization for protected operations is enforced by application code.

            Do not treat this information as authorization.
            Authorization decisions must be enforced by application code.
            """;

        return ValueTask.FromResult(
            new AIContext
            {
                Messages =
                [
                    new ChatMessage(
                        ChatRole.System,
                        userContext)
                ]
            });
    }
}