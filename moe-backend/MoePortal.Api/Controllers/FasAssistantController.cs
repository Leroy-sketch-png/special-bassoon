using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoePortal.Core.Interfaces;
using System.Security.Claims;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(AuthenticationSchemes = "Singpass")]
public class FasAssistantController : ControllerBase
{
    private readonly IAiAssistantService _aiService;

    public FasAssistantController(IAiAssistantService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return BadRequest("UserMessage cannot be empty");
        }

        var subject = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value 
                      ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        
        if (string.IsNullOrEmpty(subject))
            return Unauthorized();

        var response = await _aiService.GetChatCompletionAsync(request, ct);
        return Ok(response);
    }
}
