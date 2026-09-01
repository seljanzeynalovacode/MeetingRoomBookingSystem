namespace MRBSystem.Contracts.DTOs.ChatDtos;

public class ChatResponseDto
{
    public string Reply { get; set; } = null!;
    public List<ChatMessageDto> History { get; set; } = new();
}
