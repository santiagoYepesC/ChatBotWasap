namespace WhatsAppBot.Api.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string ConnectionStringName = "WhatsAppBot";
    public string WhatsAppBot { get; init; } = string.Empty;
}
