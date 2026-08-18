using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using StudentManagement.AI.Models;
using StudentManagement.AI.Sessions;
using StudentManagement.Core.Interfaces;
using StudentManagement.AI.Reliability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentManagement.AI.Configuration;
using System.ClientModel;
using System.Text.Json;
using System.Diagnostics;

namespace StudentManagement.AI.Services;

public class CopilotService : ICopilotService
{
    private readonly AIAgent _agent;
    private readonly ISessionStore _sessionStore;
    private readonly IStudentService _studentService;
    private readonly IAttendanceService _attendanceService;
    private readonly IFeeService _feeService;
    private readonly ILogger<CopilotService> _logger;
    private readonly OpenRouterOptions _openRouterOptions;

    public CopilotService(
    AIAgent agent,
    ISessionStore sessionStore,
    IStudentService studentService,
    IAttendanceService attendanceService,
    IFeeService feeService,
    ILogger<CopilotService> logger,
    IOptions<OpenRouterOptions> openRouterOptions)
    {
        _agent = agent;
        _sessionStore = sessionStore;
        _studentService = studentService;
        _attendanceService = attendanceService;
        _feeService = feeService;
        _logger = logger;
        _openRouterOptions = openRouterOptions.Value;
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

        AgentResponse result;
        var agentStopwatch = Stopwatch.StartNew();

        using var timeoutCts =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    _openRouterOptions.TimeoutSeconds));

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

        try
        {
            result = await _agent.RunAsync(
                message,
                session,
                cancellationToken: linkedCts.Token);
        }
        catch (ClientResultException ex)
            when (AIProviderFailureClassifier.IsTemporaryFailure(ex))
        {
            throw new AIProviderUnavailableException(
                "The AI provider is temporarily unavailable.",
                ex);
        }
        catch (OperationCanceledException ex)
            when (
                timeoutCts.IsCancellationRequested &&
                !cancellationToken.IsCancellationRequested)
        {
            throw new AIProviderUnavailableException(
                "The AI provider request timed out.",
                new TimeoutException(
                    "The AI provider did not respond within the configured timeout.",
                    ex));
        }
        finally
        {
            agentStopwatch.Stop();

            _logger.LogInformation(
                "Agent execution finished after {ElapsedMilliseconds} ms.",
                agentStopwatch.ElapsedMilliseconds);
        }

        LogAgentExecution(result);

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

    public async Task<StudentAttendanceAssessment> GetAttendanceAssessmentAsync(
    int studentId,
    CancellationToken cancellationToken = default)
    {
        // Authoritative application data
        var student = _studentService.GetStudentById(studentId);

        if (student is null)
        {
            throw new KeyNotFoundException(
                $"Student with ID {studentId} was not found.");
        }

        var attendance =
            _attendanceService.GetAttendanceSummary(studentId);

        string studentName =
            $"{student.FirstName} {student.LastName}";

        string dataStatus =
            attendance.TotalRecords == 0
                ? "No Attendance Data"
                : "Available";

        // AI is responsible only for the natural-language summary.
        string summary;
        string observation;

        try
        {
            AgentResponse<AttendanceSummaryOutput> aiResult =
                await _agent.RunAsync<AttendanceSummaryOutput>(
                    $"""
            Analyze the verified attendance data below.

            Student Name: {studentName}
            Total Records: {attendance.TotalRecords}
            Present Count: {attendance.PresentCount}
            Absent Count: {attendance.AbsentCount}
            Late Count: {attendance.LateCount}
            Excused Count: {attendance.ExcusedCount}
            Attendance Percentage: {attendance.AttendancePercentage}
            Data Status: {dataStatus}

            Return:
            - Summary: A short factual summary of the attendance data.
            - Observation: A short neutral observation about the attendance pattern.

            Rules:
            - Use only the supplied verified data.
            - Do not change or recalculate any values.
            - Do not invent institutional attendance policies.
            - Do not classify the student as At Risk, Good, Critical,
              Failing, or similar unless such a rule is explicitly provided.
            """,
                    cancellationToken: cancellationToken);

            summary = aiResult.Result.Summary;
            observation = aiResult.Result.Observation;
        }
        catch (Exception ex)
        {
            _logger.LogError(
            ex,
            "Structured attendance assessment generation failed for StudentId {StudentId}.",
            studentId);

            summary =
                $"{studentName} has {attendance.TotalRecords} attendance records.";

            observation =
                "AI-generated attendance analysis is currently unavailable.";
        }

        // Final API object is controlled by C#.
        return new StudentAttendanceAssessment(
            StudentId: student.Id,
            StudentName: studentName,
            TotalRecords: attendance.TotalRecords,
            PresentCount: attendance.PresentCount,
            AbsentCount: attendance.AbsentCount,
            LateCount: attendance.LateCount,
            ExcusedCount: attendance.ExcusedCount,
            AttendancePercentage: attendance.AttendancePercentage,
            DataStatus: dataStatus,
            Summary: summary,
            Observation: observation);
    }

    public async Task<StudentFeeAssessment> GetFeeAssessmentAsync(
    int studentId,
    int courseId,
    CancellationToken cancellationToken = default)
    {
        // Authoritative student data
        var student = _studentService.GetStudentById(studentId);

        if (student is null)
        {
            throw new KeyNotFoundException(
                $"Student with ID {studentId} was not found.");
        }

        // Authoritative fee data
        var fee = _feeService.GetFeeStatement(studentId, courseId);

        if (fee is null)
        {
            throw new KeyNotFoundException(
                $"No fee statement exists for student ID {studentId} " +
                $"and course ID {courseId}.");
        }

        string studentName =
            $"{student.FirstName} {student.LastName}";

        // AI owns only explanatory fields.
        string summary;
        string observation;

        try
        {
            AgentResponse<FeeSummaryOutput> aiResult =
                await _agent.RunAsync<FeeSummaryOutput>(
                        $"""
            Analyze the verified fee data below.

            Student Name: {studentName}
            Student ID: {student.Id}
            Course ID: {courseId}
            Amount Due: {fee.AmountDue}
            Amount Paid: {fee.AmountPaid}
            Payment Status: {fee.Status}

            Return:
            - Summary: A short factual summary of the student's fee statement.
            - Observation: A short neutral observation about the payment state.

            Rules:
            - Use only the supplied verified data.
            - Do not change or recalculate any monetary values.
            - Do not change the payment status.
            - Do not invent payment deadlines, penalties, discounts,
              scholarships, or institutional policies.
            - Do not recommend making a payment.
            """,
                cancellationToken: cancellationToken);
            summary = aiResult.Result.Summary;
            observation = aiResult.Result.Observation;
        }
        catch (Exception ex)
        {
            _logger.LogError(
            ex,
            "Structured fee assessment generation failed for StudentId {StudentId}, CourseId {CourseId}.",
            studentId,
            courseId);

            summary =
                $"{studentName} has a {fee.Status} fee status " +
                $"for course ID {courseId}.";

            observation =
                "AI-generated fee analysis is currently unavailable.";
        }

        return new StudentFeeAssessment(
            StudentId: student.Id,
            CourseId: courseId,
            AmountDue: fee.AmountDue,
            AmountPaid: fee.AmountPaid,
            PaymentStatus: fee.Status,
            Summary : summary,
            Observation : observation);
    }

    private void LogAgentExecution(AgentResponse result)
    {
        foreach (var message in result.Messages)
        {
            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case FunctionCallContent functionCall:
                        _logger.LogDebug(
                            "Agent requested tool {ToolName}.",
                            functionCall.Name);

                        if (string.Equals(
                            functionCall.Name,
                            "load_skill",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation(
                                "Agent requested skill loading. Arguments: {@Arguments}",
                                functionCall.Arguments);
                        }
                        break;

                    case FunctionResultContent functionResult:
                        _logger.LogDebug(
                            "Agent received a result for tool call {CallId}.",
                            functionResult.CallId);
                        break;

                    case ToolApprovalRequestContent approvalRequest:
                        _logger.LogDebug(
                            "Agent requested human approval. RequestId: {RequestId}",
                            approvalRequest.RequestId);
                        break;
                }
            }
        }
    }
}