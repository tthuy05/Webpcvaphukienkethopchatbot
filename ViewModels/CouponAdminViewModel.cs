using System.ComponentModel.DataAnnotations;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class CouponAdminViewModel
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Code { get; set; } = string.Empty;

    [StringLength(240)]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Range(0.01, 999999999)]
    public decimal Value { get; set; }

    [Range(0, 999999999)]
    public decimal MinimumOrderAmount { get; set; }

    [Range(0, 999999999)]
    public decimal? MaximumDiscountAmount { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
