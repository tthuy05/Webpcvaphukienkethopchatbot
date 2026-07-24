using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IChatbotService
{
    Task<ChatbotAnswer> AskAsync(string question, string? userId, CancellationToken cancellationToken = default);
}

public sealed record ChatbotAnswer(string Answer, decimal? DetectedBudget, string? DetectedNeed, IReadOnlyList<ProductCardViewModel> Products);
