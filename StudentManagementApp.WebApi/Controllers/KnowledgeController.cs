using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.AI.RAG;

namespace StudentManagementApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class KnowledgeController : ControllerBase
{
    private readonly KnowledgeIngestionService _ingestionService;

    public KnowledgeController(
        KnowledgeIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(
        [FromBody] KnowledgeIngestionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest(new
            {
                Message = "FilePath is required."
            });
        }

        await _ingestionService.IngestDocumentAsync(
            request.FilePath,
            cancellationToken);

        return Ok(new
        {
            Message = "Knowledge document indexed successfully."
        });
    }
}

public record KnowledgeIngestionRequest(
    string FilePath);