using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _productService.GetAdminProductsAsync(HttpContext.RequestAborted));
    }

    public async Task<IActionResult> Create()
    {
        return View("Form", await _productService.CreateFormAsync(HttpContext.RequestAborted));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _productService.EditFormAsync(id, HttpContext.RequestAborted);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Save(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _productService.PopulateSelectionsAsync(model, HttpContext.RequestAborted);
            return View("Form", model);
        }

        try
        {
            await _productService.SaveAsync(model, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Da luu san pham.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await _productService.PopulateSelectionsAsync(model, HttpContext.RequestAborted);
            return View("Form", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id)
    {
        await _productService.HideAsync(id, HttpContext.RequestAborted);
        TempData["SuccessMessage"] = "Da an san pham.";
        return RedirectToAction(nameof(Index));
    }
}
