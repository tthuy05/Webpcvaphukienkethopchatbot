namespace Webpcvaphukienkethopchatbot.ViewModels;

public class ChatbotProductSuggestionViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
