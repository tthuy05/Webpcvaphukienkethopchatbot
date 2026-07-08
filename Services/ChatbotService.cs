using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public partial class ChatbotService : IChatbotService
{
    private readonly ApplicationDbContext _dbContext;

    public ChatbotService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChatbotResponseViewModel> AskAsync(ClaimsPrincipal user, string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException("Vui lòng nhập câu hỏi.");
        }

        var normalizedQuestion = Normalize(question);
        var budget = DetectBudget(normalizedQuestion);
        var usage = DetectUsage(normalizedQuestion);

        var query = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Brand)
            .Where(product => product.IsActive && product.Category != null && product.Category.Slug == "laptop");

        if (budget.HasValue)
        {
            query = query.Where(product => product.Price <= budget.Value);
        }

        if (!string.IsNullOrWhiteSpace(usage))
        {
            query = query.Where(product => product.SuitableNeeds.Contains(usage) || product.Description.Contains(usage));
        }

        var candidates = await query.ToListAsync(cancellationToken);
        var suggestions = candidates
            .Select(product => new
            {
                Product = product,
                Score = ScoreProduct(product, usage, budget)
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Product.Ram != null && item.Product.Ram.Contains("16"))
            .ThenBy(item => item.Product.Price)
            .Take(3)
            .Select(item => new ChatbotProductSuggestionViewModel
            {
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                Price = item.Product.Price,
                ImageUrl = item.Product.MainImageUrl,
                Reason = BuildReason(item.Product, usage, budget)
            })
            .ToList();

        var answer = BuildAnswer(suggestions, usage, budget);
        var response = new ChatbotResponseViewModel
        {
            Question = question.Trim(),
            Answer = answer,
            DetectedBudget = budget,
            DetectedUsageNeed = usage,
            Suggestions = suggestions
        };

        _dbContext.ChatbotLogs.Add(new ChatbotLog
        {
            ApplicationUserId = GetUserId(user),
            Question = response.Question,
            Answer = response.Answer,
            DetectedBudget = response.DetectedBudget,
            DetectedUsageNeed = response.DetectedUsageNeed
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static decimal? DetectBudget(string question)
    {
        var millionMatch = MillionBudgetRegex().Match(question);
        if (millionMatch.Success && TryParseBudgetValue(millionMatch.Groups["value"].Value, out var millionValue))
        {
            return millionValue * 1_000_000;
        }

        var directMatch = DirectBudgetRegex().Match(question);
        if (directMatch.Success && TryParseBudgetValue(directMatch.Groups["value"].Value, out var directValue))
        {
            return directValue;
        }

        return null;
    }

    private static bool TryParseBudgetValue(string value, out decimal budget)
    {
        var normalized = value.Replace(" ", string.Empty).Replace(",", ".", StringComparison.Ordinal);
        if (normalized.Count(character => character == '.') > 1)
        {
            normalized = normalized.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out budget);
    }

    private static string? DetectUsage(string question)
    {
        var usages = new Dictionary<string, string>
        {
            ["hoc it"] = "hoc IT",
            ["cong nghe thong tin"] = "hoc IT",
            ["lap trinh"] = "lap trinh",
            ["code"] = "lap trinh",
            ["van phong"] = "van phong",
            ["gaming"] = "gaming",
            ["choi game"] = "gaming",
            ["do hoa"] = "do hoa",
            ["thiet ke"] = "do hoa",
            ["sinh vien"] = "sinh vien",
            ["mong nhe"] = "mong nhe"
        };

        foreach (var usage in usages)
        {
            if (question.Contains(usage.Key, StringComparison.OrdinalIgnoreCase))
            {
                return usage.Value;
            }
        }

        return null;
    }

    private static int ScoreProduct(Product product, string? usage, decimal? budget)
    {
        var score = 0;

        if (budget.HasValue && product.Price <= budget.Value)
        {
            score += 3;
            if (budget.Value - product.Price <= 2_000_000)
            {
                score += 1;
            }
        }

        if (!string.IsNullOrWhiteSpace(usage) && product.SuitableNeeds.Contains(usage, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        if (product.Ram?.Contains("16") == true || product.Ram?.Contains("32") == true)
        {
            score += 2;
        }

        if (product.Ssd?.Contains("512") == true || product.Ssd?.Contains("1TB") == true)
        {
            score += 2;
        }

        if (product.Cpu?.Contains("i5", StringComparison.OrdinalIgnoreCase) == true ||
            product.Cpu?.Contains("i7", StringComparison.OrdinalIgnoreCase) == true ||
            product.Cpu?.Contains("Ryzen 5", StringComparison.OrdinalIgnoreCase) == true ||
            product.Cpu?.Contains("Ryzen 7", StringComparison.OrdinalIgnoreCase) == true)
        {
            score += 2;
        }

        if (usage is "gaming" or "do hoa" && product.Gpu?.Contains("RTX", StringComparison.OrdinalIgnoreCase) == true)
        {
            score += 3;
        }

        return score;
    }

    private static string BuildReason(Product product, string? usage, decimal? budget)
    {
        var parts = new List<string>();

        if (budget.HasValue)
        {
            parts.Add(product.Price <= budget.Value ? "nằm trong ngân sách" : "gần ngân sách");
        }

        if (!string.IsNullOrWhiteSpace(usage) && product.SuitableNeeds.Contains(usage, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"phù hợp nhu cầu {usage}");
        }

        if (!string.IsNullOrWhiteSpace(product.Cpu))
        {
            parts.Add($"CPU {product.Cpu}");
        }

        if (!string.IsNullOrWhiteSpace(product.Ram))
        {
            parts.Add($"RAM {product.Ram}");
        }

        if (!string.IsNullOrWhiteSpace(product.Ssd))
        {
            parts.Add($"SSD {product.Ssd}");
        }

        return string.Join(", ", parts);
    }

    private static string BuildAnswer(IReadOnlyList<ChatbotProductSuggestionViewModel> suggestions, string? usage, decimal? budget)
    {
        if (!suggestions.Any())
        {
            return "Chưa tìm thấy laptop phù hợp. Bạn có thể tăng ngân sách hoặc nói rõ hơn về nhu cầu sử dụng.";
        }

        var builder = new StringBuilder();
        builder.Append("Mình gợi ý các mẫu laptop phù hợp");
        if (budget.HasValue)
        {
            builder.Append($" trong tầm {budget.Value:N0} VND");
        }
        if (!string.IsNullOrWhiteSpace(usage))
        {
            builder.Append($" cho nhu cầu {usage}");
        }
        builder.Append('.');

        return builder.ToString();
    }

    private static string Normalize(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string? GetUserId(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    }

    [GeneratedRegex(@"\b(?<value>\d+(?:[\.,]\d+)?)\s*(trieu|tr|million)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MillionBudgetRegex();

    [GeneratedRegex(@"\b(?<value>(?:\d{1,3}(?:[\.,\s]\d{3}){2,3})|\d{7,9})\s*(vnd|dong|d)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex DirectBudgetRegex();
}
