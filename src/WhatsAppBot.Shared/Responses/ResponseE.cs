namespace WhatsAppBot.Shared.Responses;

public sealed class ResponseE<T>
{
    public required bool Success { get; init; }
    public T? Data { get; init; }
    public ApiErrorDTO? Error { get; init; }
    public required string TraceId { get; init; }

    public static ResponseE<T> Ok(T data, string traceId) =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ResponseE<T> Fail(ApiErrorDTO error, string traceId) =>
        new() { Success = false, Error = error, TraceId = traceId };
}

public sealed class ApiErrorDTO
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public IReadOnlyList<string>? Details { get; init; }
}
