using System.ComponentModel.DataAnnotations;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.Models;

public sealed class InventoryTransaction
{
    public long Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public InventoryTransactionType Type { get; set; }

    public int QuantityDelta { get; set; }

    public int StockBefore { get; set; }

    public int StockAfter { get; set; }

    [StringLength(40)]
    public string? ReferenceType { get; set; }

    [StringLength(100)]
    public string? ReferenceId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public string? PerformedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
