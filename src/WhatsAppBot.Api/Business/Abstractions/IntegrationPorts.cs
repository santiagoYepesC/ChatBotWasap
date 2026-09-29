namespace WhatsAppBot.Api.Business.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface ISecretReferenceResolver
{
    ValueTask<string?> ResolveAsync(string secretReference, CancellationToken cancellationToken);
}

public interface IMetaWhatsAppClient
{
    Task SendTextAsync(string phoneNumberId, string recipient, string text, CancellationToken cancellationToken);
}

public interface IAiReplyGenerator
{
    Task<string> GenerateReplyAsync(string prompt, CancellationToken cancellationToken);
}

public interface IImageAnalysisService
{
    Task<string> AnalyzeAsync(Stream image, string contentType, CancellationToken cancellationToken);
}

public interface IAudioTranscriptionService
{
    Task<string> TranscribeAsync(Stream audio, string contentType, CancellationToken cancellationToken);
}

public interface IMediaStorage
{
    Task<string> StorePrivateAsync(Stream content, string contentType, CancellationToken cancellationToken);
}
