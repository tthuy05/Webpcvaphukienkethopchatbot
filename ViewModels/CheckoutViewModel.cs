using System.ComponentModel.DataAnnotations;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [StringLength(160, MinimumLength = 2)]
    [Display(Name = "Người nhận")]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
    [StringLength(500, MinimumLength = 5)]
    [Display(Name = "Địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
    [Display(Name = "Phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

    [StringLength(40)]
    [Display(Name = "Mã giảm giá")]
    public string? CouponCode { get; set; }

    public CartViewModel Cart { get; set; } = new();
}
