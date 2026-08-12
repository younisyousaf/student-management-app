using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.AI.Services;

namespace StudentManagementApp.WebApi.Controllers;

public record CopilotChatRequest(string Message, string? SessionId);
public record CopilotChatResponse(
    string? Response,
    string SessionId,
    bool RequiresApproval,
    CopilotApprovalRequest? Approval);

public record CopilotApprovalDecisionRequest(
    string SessionId,
    string RequestId,
    bool Approved,
    string? Reason);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CopilotController : ControllerBase
{
    private readonly ICopilotService _copilotService;

    public CopilotController(ICopilotService copilotService)
    {
        _copilotService = copilotService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] CopilotChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Message = "Message cannot be empty." });
        }

        var result = await _copilotService.SendMessageAsync(
      request.Message,
      request.SessionId,
      cancellationToken);

        return Ok(
            new CopilotChatResponse(
                result.Response,
                result.SessionId,
                result.RequiresApproval,
                result.Approval));
    }

    [HttpPost("approval")]
    public async Task<IActionResult> RespondToApproval(
    [FromBody] CopilotApprovalDecisionRequest request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(new
            {
                Message = "SessionId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return BadRequest(new
            {
                Message = "RequestId is required."
            });
        }

        var result =
            await _copilotService.RespondToApprovalAsync(
                request.SessionId,
                request.RequestId,
                request.Approved,
                request.Reason,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("students/{studentId:int}/attendance-assessment")]
    public async Task<IActionResult> GetAttendanceAssessment(
    int studentId,
    CancellationToken cancellationToken)
    {
        var result =
            await _copilotService.GetAttendanceAssessmentAsync(
                studentId,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("students/{studentId:int}/courses/{courseId:int}/fee-assessment")]
    public async Task<IActionResult> GetFeeAssessment(
    int studentId,
    int courseId,
    CancellationToken cancellationToken)
    {
        var result =
            await _copilotService.GetFeeAssessmentAsync(
                studentId,
                courseId,
                cancellationToken);

        return Ok(result);
    }
}