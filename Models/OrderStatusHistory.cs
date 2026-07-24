using System.ComponentModel.DataAnnotations;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.Models;

public sealed class OrderStatusHistory
{
    public long Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public string? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
