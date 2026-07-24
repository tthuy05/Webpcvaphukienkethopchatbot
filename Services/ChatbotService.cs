using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed partial class ChatbotService : IChatbotService
{
    private static readonly string[] Needs = ["gaming", "do hoa", "lap trinh", "hoc it", "van phong", "sinh vien", "mong nhe", "hoc tap"];
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatbotService(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

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
        var isExactBudgetMatch = true;

        if (budget.HasValue)
        {
            var budgetFiltered = filtered.Where(item => item.CurrentPrice <= budget.Value).ToList();
            if (budgetFiltered.Count > 0)
            {
                filtered = budgetFiltered;
            }
            else
            {
                isExactBudgetMatch = false;
                filtered = candidates.OrderBy(item => item.CurrentPrice);
            }
        }

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

        string answer;
        var apiKey = _configuration["Gemini:ApiKey"];

        if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "YOUR_GEMINI_API_KEY_HERE")
        {
            var geminiAnswer = await AskGeminiAsync(question, products, isExactBudgetMatch, apiKey, cancellationToken);
            answer = !string.IsNullOrWhiteSpace(geminiAnswer) ? geminiAnswer : SmartFallbackAnswer(question, products, budget, need, isExactBudgetMatch);
        }
        else
        {
            answer = SmartFallbackAnswer(question, products, budget, need, isExactBudgetMatch);
        }

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

    private async Task<string?> AskGeminiAsync(string question, List<ProductCardViewModel> products, bool isExactBudgetMatch, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            var model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var sbPrompt = new StringBuilder();
            sbPrompt.AppendLine("Bạn là chuyên gia tư vấn máy tính bán hàng xuất sắc của cửa hàng PC Store.");
            sbPrompt.AppendLine("Nhiệm vụ của bạn là tư vấn thân thiện, thông minh, đúng trọng tâm và tự nhiên bằng Tiếng Việt.");
            sbPrompt.AppendLine();
            sbPrompt.AppendLine("QUY TẮC TƯ VẤN:");
            sbPrompt.AppendLine("1. NẾU CÂU HỎI QUÁ CHUNG CHUNG (ví dụ: 'tìm laptop đi học', 'cần tư vấn laptop'): Hãy lịch sự đặt câu hỏi ngược lại để làm rõ nhu cầu: hỏi khách học ngành gì (Lập trình, Đồ họa hay Văn phòng) và ngân sách dự kiến bao nhiêu triệu.");
            sbPrompt.AppendLine("2. NẾU KHÁCH ĐÃ NÓI RÕ NHU CẦU HOẶC GIÁ: Phân tích nhanh tại sao các sản phẩm dưới đây phù hợp nhất cho họ.");
            sbPrompt.AppendLine("3. NẾU KHÔNG CÓ SẢN PHẨM KHỚP 100% HOẶC VƯỢT NGÂN SÁCH: Giải thích lịch sự và chủ động đề xuất sản phẩm thay thế tốt nhất có sẵn trong kho bên dưới.");
            sbPrompt.AppendLine();
            sbPrompt.AppendLine("Danh sách sản phẩm có sẵn trong cửa hàng:");

            foreach (var p in products)
            {
                sbPrompt.AppendLine($"- {p.Name} | Giá: {p.CurrentPrice:N0} VNĐ | Mô tả: {p.Description}");
            }

            sbPrompt.AppendLine();
            sbPrompt.AppendLine($"Câu hỏi của khách hàng: \"{question}\"");

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = sbPrompt.ToString() }
                        }
                    }
                }
            };

            var client = _httpClientFactory.CreateClient();
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    return text?.Trim();
                }
            }
        }
        catch
        {
            // Fallback gracefully on exception
        }

        return null;
    }

    private static string SmartFallbackAnswer(string question, List<ProductCardViewModel> products, decimal? budget, string? need, bool isExactBudgetMatch)
    {
        var norm = Normalize(question);

        if (!budget.HasValue && (norm == "lap trinh" || norm.Contains("di hoc") || norm.Contains("hoc tap") || norm.Contains("tu van") || norm.Contains("tim laptop")))
        {
            return "Dạ chào bạn! Để PC Store tư vấn chuẩn nhất cho bạn, bạn có thể chia sẻ thêm: Bạn cần mua laptop cho ngành học gì (Công nghệ thông tin/Lập trình, Thiết kế đồ họa hay Kinh tế/Văn phòng) và ngân sách dự kiến khoảng bao nhiêu triệu không ạ?";
        }

        if (!isExactBudgetMatch && budget.HasValue)
        {
            return $"Hiện tại shop chưa có mẫu laptop trong tầm giá dưới {budget.Value:N0} ₫ đáp ứng hoàn toàn nhu cầu này. Tuy nhiên, shop đề xuất một số mẫu laptop thay thế có cấu hình cực tốt ở mức giá gần nhất cho bạn tham khảo nhé!";
        }

        return products.Count == 0
            ? "Hiện tại cửa hàng tạm hết sản phẩm đúng theo tiêu chí này. Bạn có thể cho shop biết thêm ngân sách hoặc nhu cầu khác để shop gợi ý mẫu thay thế tốt nhất nhé!"
            : $"PC Store xin gợi ý {products.Count} sản phẩm phù hợp nhất" +
              (need is not null ? $" cho nhu cầu {need}" : string.Empty) +
              (budget.HasValue ? $" trong tầm giá {budget.Value:N0} ₫" : string.Empty) + " cho bạn tham khảo bên dưới:";
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
