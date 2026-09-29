using WhatsAppBot.Api.Models.Entities;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Api.Models.Contracts;

public sealed record FrequentResponseCommand(
    string QuestionOrIntent,
    IReadOnlyList<string> Expressions,
    string AnswerText,
    int Priority,
    string? Category,
    bool IsActive);

public sealed record FrequentResponseRecord(
    FrequentResponse Response,
    IReadOnlyList<string> Expressions);

public sealed record FrequentResponsePage(
    IReadOnlyList<FrequentResponseRecord> Items,
    int Page,
    int PageSize,
    int TotalCount);

public enum BotReplyResolutionKind
{
    FrequentResponse,
    NoReply,
    AiFallbackRequired
}

public sealed record BotReplyResolution(
    BotReplyResolutionKind Kind,
    FrequentResponseRecord? FrequentResponse = null,
    bool FaqEvaluationSkipped = false);

public sealed class BusinessValidationException(string message) : Exception(message);

public sealed class BusinessNotFoundException(string resource) : Exception($"{resource} was not found.");

public static class ReplyModeRules
{
    public static bool IsSupported(BotReplyMode mode) => Enum.IsDefined(mode);
}
