using System.ComponentModel.DataAnnotations;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class ProductDetailsViewModel
{
    public Product Product { get; init; } = new();

    public decimal CurrentPrice { get; init; }

    public double? AverageRating { get; init; }

    public IReadOnlyList<ProductReview> Reviews { get; init; } = [];

    public bool CanReview { get; init; }

    public bool IsWishlisted { get; init; }

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Comment { get; set; } = string.Empty;
}
