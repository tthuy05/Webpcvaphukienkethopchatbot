using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BrandsController : Controller
{
    private readonly ICatalogService _catalogService;

    public BrandsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _catalogService.GetBrandsAsync(HttpContext.RequestAborted));
    }

    public IActionResult Create()
    {
        return View("Form", new CatalogFormViewModel());
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _catalogService.GetBrandAsync(id, HttpContext.RequestAborted);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CatalogFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await _catalogService.SaveBrandAsync(model, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Đã lưu thương hiệu.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(model.Name), exception.Message);
            return View("Form", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _catalogService.HideOrDeleteBrandAsync(id, HttpContext.RequestAborted);
        TempData["SuccessMessage"] = "Đã ẩn hoặc xóa thương hiệu.";
        return RedirectToAction(nameof(Index));
    }
}
