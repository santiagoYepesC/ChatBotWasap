namespace WhatsAppBot.Shared.Responses;

public sealed class AuthResponseDTO
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAtUtc { get; init; }
}
