using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.AI.Services;

namespace StudentManagementApp.WebApi.Controllers;

public record CopilotChatRequest(string Message);
public record CopilotChatResponse(string Response);

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

    public record CopilotChatRequest(string Message, string? SessionId);
    public record CopilotChatResponse(string Response, string SessionId);

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] CopilotChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Message = "Message cannot be empty." });
        }

        var result = await _copilotService.SendMessageAsync(request.Message, request.SessionId, cancellationToken);
        return Ok(new CopilotChatResponse(result.Response, result.SessionId));
    }
}