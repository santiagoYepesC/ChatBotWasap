using System.Text;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Api.Models.Entities;

namespace WhatsAppBot.Api.Business.Services;

public sealed class FrequentResponseMatcher
{
    public FrequentResponseRecord? Match(
        string message, IEnumerable<FrequentResponseRecord> candidates)
    {
        var normalizedMessage = Normalize(message);
        if (normalizedMessage.Length == 0)
        {
            return null;
        }

        return candidates
            .Where(candidate => candidate.Response.IsActive)
            .Where(candidate => candidate.Expressions.Any(expression =>
                Normalize(expression).Length > 0 &&
                normalizedMessage.Contains(Normalize(expression), StringComparison.Ordinal)))
            .OrderByDescending(candidate => candidate.Response.Priority)
            .ThenBy(candidate => candidate.Response.FrequentResponseId)
            .FirstOrDefault();
    }

    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        var pendingSpace = false;

        foreach (var character in normalized.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}
