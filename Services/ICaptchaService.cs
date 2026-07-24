namespace Webpcvaphukienkethopchatbot.Services;

public interface ICaptchaService
{
    string CreateQuestion(string purpose);

    bool Validate(string purpose, int? answer);
}
