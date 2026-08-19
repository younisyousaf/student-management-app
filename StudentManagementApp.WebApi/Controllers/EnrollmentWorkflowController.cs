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
}