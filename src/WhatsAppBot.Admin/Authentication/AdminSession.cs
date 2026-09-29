namespace WhatsAppBot.Admin.Authentication;

public interface IAdminSession
{
    void SetApiAccessToken(string accessToken);
    string? GetApiAccessToken();
    void Clear();
}

public sealed class AdminSession(IHttpContextAccessor contextAccessor) : IAdminSession
{
    private const string AccessTokenKey = "ApiAccessToken";

    public void SetApiAccessToken(string accessToken) =>
        GetSession().SetString(AccessTokenKey, accessToken);

    public string? GetApiAccessToken() => GetSession().GetString(AccessTokenKey);

    public void Clear() => GetSession().Clear();

    private ISession GetSession() =>
        contextAccessor.HttpContext?.Session ??
        throw new InvalidOperationException("An active HTTP session is required.");
}
