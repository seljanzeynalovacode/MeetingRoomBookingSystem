using MRBSystem.Contracts.DTOs.ChatDtos;

namespace MRBSystem.Contracts.Services;

public interface IChatService
{
    Task<ChatResponseDto> SendMessageAsync(ChatRequestDto dto);
}
