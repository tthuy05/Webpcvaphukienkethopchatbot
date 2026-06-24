using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ICatalogService _catalogService;

    public CategoriesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _catalogService.GetCategoriesAsync(HttpContext.RequestAborted));
    }

    public IActionResult Create()
    {
        return View("Form", new CatalogFormViewModel());
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _catalogService.GetCategoryAsync(id, HttpContext.RequestAborted);
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
            await _catalogService.SaveCategoryAsync(model, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Da luu danh muc.";
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
        await _catalogService.HideOrDeleteCategoryAsync(id, HttpContext.RequestAborted);
        TempData["SuccessMessage"] = "Da an hoac xoa danh muc.";
        return RedirectToAction(nameof(Index));
    }
}
