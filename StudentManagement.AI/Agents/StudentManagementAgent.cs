using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace StudentManagement.AI.Agents;

public static class StudentManagementAgent
{
    private const string Instructions = """
        You are the Student Management Copilot for an educational institution.
        You help administrators look up student information, courses, enrollments,
        fee status, and attendance, and can perform actions like enrolling students
        or recording payments when explicitly asked and approved.

        Only state facts you can verify through your available tools. If you do not
        currently have a tool to answer a question, say so plainly instead of guessing
        or inventing student data.
        """;

    public static AIAgent Create(IChatClient chatClient, IList<AITool> tools)
    {
        return new ChatClientAgent(
            chatClient,
            instructions: Instructions,
            name: "StudentManagementCopilot",
            tools: tools);
    }
}