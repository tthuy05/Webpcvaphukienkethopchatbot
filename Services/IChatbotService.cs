using System.Security.Claims;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IChatbotService
{
    Task<ChatbotResponseViewModel> AskAsync(ClaimsPrincipal user, string question, CancellationToken cancellationToken = default);
}
