using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.ChatDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto dto)
    {
        var result = await _chatService.SendMessageAsync(dto);
        return Ok(result);
    }
}
