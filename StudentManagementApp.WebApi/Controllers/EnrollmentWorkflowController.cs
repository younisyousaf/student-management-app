using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.AI.Workflows.Enrollment;
using StudentManagement.AI.Workflows.Enrollment.Models;

namespace StudentManagementApp.WebApi.Controllers;

[ApiController]
[Route("api/enrollment-workflow")]
[Authorize]
public sealed class EnrollmentWorkflowController : ControllerBase
{
    private readonly EnrollmentWorkflowService _workflowService;

    public EnrollmentWorkflowController(
        EnrollmentWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpPost]
    public async Task<IActionResult> Test(
        [FromBody] EnrollmentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.RunAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("approval")]
    public async Task<IActionResult> Approve(
    [FromBody] EnrollmentWorkflowApprovalDecision request,
    CancellationToken cancellationToken)
    {
        var result =
            await _workflowService.ResumeAsync(
                request.RequestId,
                request.Approved,
                cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{requestId}/recover")]
    public async Task<IActionResult> Recover(
    string requestId,
    CancellationToken cancellationToken)
    {
        var result =
            await _workflowService.RecoverAsync(
                requestId,
                cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{requestId}/retry")]
    public async Task<IActionResult> Retry(
    string requestId,
    CancellationToken cancellationToken)
    {
        var result =
            await _workflowService.RetryAsync(
                requestId,
                cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{requestId}/history")]
    public async Task<IActionResult> GetHistory(
    string requestId,
    CancellationToken cancellationToken)
    {
        var history =
            await _workflowService.GetHistoryAsync(
                requestId,
                cancellationToken);

        return Ok(history);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{requestId}/summary")]
    public async Task<IActionResult> GetSummary(
    string requestId,
    CancellationToken cancellationToken)
    {
        var summary =
            await _workflowService.GetSummaryAsync(
                requestId,
                cancellationToken);

        return Ok(summary);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetWorkflows(
    [FromQuery] EnrollmentWorkflowQuery query,
    CancellationToken cancellationToken)
    {
        var workflows =
            await _workflowService.GetWorkflowsAsync(
                query,
                cancellationToken);

        return Ok(workflows);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("recover-stale")]
    public async Task<IActionResult> RecoverStale(
    [FromQuery] int staleMinutes = 30,
    CancellationToken cancellationToken = default)
    {
        if (staleMinutes <= 0)
        {
            throw new ArgumentException(
                "staleMinutes must be greater than zero.",
                nameof(staleMinutes));
        }

        int affectedRows =
            await _workflowService
                .RecoverStaleProcessingAsync(
                    TimeSpan.FromMinutes(staleMinutes),
                    cancellationToken);

        return Ok(new
        {
            affectedRows,
            message =
                $"{affectedRows} stale processing workflow(s) marked as interrupted."
        });
    }
}