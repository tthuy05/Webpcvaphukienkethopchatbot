using System.ComponentModel.DataAnnotations;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class CheckoutViewModel
{
    [Required, StringLength(160)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

    public CartViewModel Cart { get; set; } = new();
}
