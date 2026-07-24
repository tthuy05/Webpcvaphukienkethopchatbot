using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.Models;

public sealed class Coupon
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Code { get; set; } = string.Empty;

    [StringLength(240)]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999999)]
    public decimal Value { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 999999999)]
    public decimal MinimumOrderAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 999999999)]
    public decimal? MaximumDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
