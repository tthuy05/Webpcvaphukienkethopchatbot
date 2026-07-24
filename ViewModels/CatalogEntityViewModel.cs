using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public sealed class CatalogEntityViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Tên")]
    public string Name { get; set; } = string.Empty;

    [StringLength(140)]
    public string? Slug { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
