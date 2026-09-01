namespace MRBSystem.Contracts.DTOs.ChatDtos;

public class ChatRequestDto
{
    public string Message { get; set; } = null!;
    public List<ChatMessageDto> History { get; set; } = new();
}
