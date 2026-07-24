using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed partial class ChatbotService : IChatbotService
{
    private static readonly string[] Needs = ["gaming", "do hoa", "lap trinh", "van phong", "sinh vien", "mong nhe"];
    private readonly ApplicationDbContext _dbContext;

    public ChatbotService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<ChatbotAnswer> AskAsync(string question, string? userId, CancellationToken cancellationToken = default)
    {
        question = question.Trim();
        var normalized = Normalize(question);
        var budget = DetectBudget(normalized);
        var need = Needs.FirstOrDefault(normalized.Contains);
        var now = DateTime.UtcNow;
        var candidates = await _dbContext.Products.AsNoTracking()
            .Where(product => product.IsActive && product.StockQuantity > 0)
            .Select(product => new
            {
                Product = product,
                CurrentPrice = product.SalePrice.HasValue && product.SalePrice < product.Price &&
                               (!product.SaleStartAt.HasValue || product.SaleStartAt <= now) &&
                               (!product.SaleEndAt.HasValue || product.SaleEndAt >= now)
                    ? product.SalePrice.Value
                    : product.Price,
                Rating = product.Reviews.Where(review => review.IsVisible).Select(review => (double?)review.Rating).Average()
            })
            .ToListAsync(cancellationToken);

        var filtered = candidates.AsEnumerable();
        if (budget.HasValue) filtered = filtered.Where(item => item.CurrentPrice <= budget.Value);
        if (need is not null)
        {
            filtered = filtered.OrderByDescending(item => Normalize(item.Product.SuitableNeeds).Contains(need));
        }
        else
        {
            filtered = filtered.OrderByDescending(item => item.Rating ?? 0).ThenByDescending(item => item.Product.SoldQuantity);
        }

        var products = filtered.Take(5).Select(item => new ProductCardViewModel
        {
            Id = item.Product.Id,
            Name = item.Product.Name,
            Slug = item.Product.Slug,
            Description = item.Product.Description,
            ImageUrl = item.Product.MainImageUrl,
            OriginalPrice = item.Product.Price,
            CurrentPrice = item.CurrentPrice,
            StockQuantity = item.Product.StockQuantity,
            AverageRating = item.Rating
        }).ToList();

        var answer = products.Count == 0
            ? "Mình chưa tìm thấy sản phẩm còn hàng phù hợp. Hãy thử tăng ngân sách hoặc mô tả nhu cầu rộng hơn."
            : $"Mình tìm thấy {products.Count} sản phẩm phù hợp" +
              (budget.HasValue ? $" trong ngân sách {budget.Value:N0} ₫" : string.Empty) +
              (need is not null ? $" cho nhu cầu {need}" : string.Empty) + ".";

        _dbContext.ChatbotLogs.Add(new ChatbotLog
        {
            ApplicationUserId = userId,
            Question = question,
            Answer = answer,
            DetectedBudget = budget,
            DetectedUsageNeed = need,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ChatbotAnswer(answer, budget, need, products);
    }

    private static decimal? DetectBudget(string text)
    {
        var match = BudgetPattern().Match(text);
        if (!match.Success) return null;
        if (!decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) return null;
        var unit = match.Groups[2].Value;
        return unit is "trieu" or "tr" or "m" ? value * 1_000_000m : value;
    }

    private static string Normalize(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character == 'đ' ? 'd' : character);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"(\d+(?:[\.,]\d+)?)\s*(trieu|tr|m|vnd|dong)?", RegexOptions.IgnoreCase)]
    private static partial Regex BudgetPattern();
}
