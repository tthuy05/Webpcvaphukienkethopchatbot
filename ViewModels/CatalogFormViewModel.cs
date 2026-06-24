using System.ComponentModel.DataAnnotations;

namespace Webpcvaphukienkethopchatbot.ViewModels;

public class CatalogFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
