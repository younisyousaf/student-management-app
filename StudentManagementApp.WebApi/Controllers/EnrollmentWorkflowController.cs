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

    [HttpPost("approval")]
    public async Task<IActionResult> Approve(
    [FromBody] EnrollmentWorkflowApprovalDecision request,
    CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _workflowService.ResumeAsync(
                    request.RequestId,
                    request.Approved,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
    }

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

    [HttpGet]
    public async Task<IActionResult> GetWorkflows(
    CancellationToken cancellationToken)
    {
        var workflows =
            await _workflowService.GetWorkflowsAsync(
                cancellationToken);

        return Ok(workflows);
    }
}