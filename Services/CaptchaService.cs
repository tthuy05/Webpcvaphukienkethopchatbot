using System.Security.Cryptography;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed class CaptchaService : ICaptchaService
{
    private const string Prefix = "Captcha:";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CaptchaService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CreateQuestion(string purpose)
    {
        var session = GetSession();
        var left = RandomNumberGenerator.GetInt32(2, 10);
        var right = RandomNumberGenerator.GetInt32(1, 10);
        session.SetInt32(Prefix + purpose, left + right);
        return $"{left} + {right} = ?";
    }

    public bool Validate(string purpose, int? answer)
    {
        var session = GetSession();
        var key = Prefix + purpose;
        var expected = session.GetInt32(key);
        session.Remove(key);
        return answer.HasValue && expected.HasValue && answer.Value == expected.Value;
    }

    private ISession GetSession() => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Session is not available for captcha validation.");
}
