using System.ComponentModel.DataAnnotations;

namespace WhatsAppBot.Shared.Requests;

public sealed class LoginRequestDTO
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(1024)]
    public string Password { get; init; } = string.Empty;
}
