using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.Models;

public class Order
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    [Required, StringLength(160)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 999999999)]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();

    public Payment? Payment { get; set; }
}
