using System.ComponentModel.DataAnnotations;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class ProfileViewModel
{
    [Required, StringLength(160)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public IReadOnlyList<Order> RecentOrders { get; set; } = new List<Order>();
}
