namespace WhatsAppBot.Api.Models.Responses;

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAtUtc);
