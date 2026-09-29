namespace WhatsAppBot.Api.Helpers;

public static class TraceIdHelper
{
    public static string GetTraceId(HttpContext context) => context.TraceIdentifier;
}
