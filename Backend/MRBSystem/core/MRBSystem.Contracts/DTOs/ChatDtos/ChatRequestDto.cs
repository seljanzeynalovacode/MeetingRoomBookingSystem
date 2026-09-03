using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.ChatDtos;

public class ChatRequestDto
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Message { get; set; } = null!;

    [MaxLength(40)]
    public List<ChatMessageDto> History { get; set; } = new();
}
