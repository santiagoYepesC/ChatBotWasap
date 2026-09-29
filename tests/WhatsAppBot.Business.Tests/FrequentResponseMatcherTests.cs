using WhatsAppBot.Api.Business.Services;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Api.Models.Entities;

namespace WhatsAppBot.Business.Tests;

public sealed class FrequentResponseMatcherTests
{
    private readonly FrequentResponseMatcher _matcher = new();

    [Fact]
    public void Match_NormalizesUnicodeWhitespaceAndCase_ThenUsesLiteralContainment()
    {
        var candidate = Candidate(1, "  precio\tactual ", 10, "La respuesta");

        var result = _matcher.Match("¿Cuál es el PRECIO   actual hoy?", [candidate]);

        Assert.Same(candidate, result);
    }

    [Fact]
    public void Match_IgnoresInactiveRules()
    {
        var inactive = Candidate(1, "horario", 100, "No debe responder", isActive: false);

        var result = _matcher.Match("¿Cuál es el horario?", [inactive]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_SelectsHighestPriority_AndThenLowestStableId()
    {
        var lowerPriority = Candidate(1, "envío", 3, "Baja prioridad");
        var laterId = Candidate(9, "envío", 8, "Id posterior");
        var firstById = Candidate(4, "envío", 8, "Id anterior");

        var result = _matcher.Match("consulta de envío", [lowerPriority, laterId, firstById]);

        Assert.Same(firstById, result);
    }

    [Fact]
    public void Match_DoesNotUseFuzzyOrDescriptionMatching()
    {
        var candidate = Candidate(1, "devolución exacta", 1, "Devuelve el artículo");

        Assert.Null(_matcher.Match("devolucion aproximada", [candidate]));
    }

    private static FrequentResponseRecord Candidate(
        long id, string expression, int priority, string answer, bool isActive = true)
    {
        var response = new FrequentResponse(
            id, Guid.Empty, $"Intent {id}", answer, priority, null, isActive);
        return new FrequentResponseRecord(response, [expression]);
    }
}
