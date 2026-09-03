using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.ChatDtos;

public class ChatMessageDto
{
    [Required, RegularExpression("^(user|model)$", ErrorMessage = "Rol 'user' ve ya 'model' olmal?d?r.")]
    public string Role { get; set; } = null!;

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Text { get; set; } = null!;
}
