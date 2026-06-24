using Microsoft.AspNetCore.Mvc;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index([FromQuery] ProductFilterViewModel filter)
    {
        return View(await _productService.SearchAsync(filter, HttpContext.RequestAborted));
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetDetailAsync(id, HttpContext.RequestAborted);
        return product is null ? NotFound() : View(product);
    }
}
