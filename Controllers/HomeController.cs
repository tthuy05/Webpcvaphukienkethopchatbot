using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        int? brandId,
        decimal? minPrice,
        decimal? maxPrice,
        string sort = "newest",
        int page = 1)
    {
        page = Math.Max(1, page);
        const int pageSize = 12;
        var now = DateTime.UtcNow;
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive && product.Category!.IsActive && product.Brand!.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(product =>
                product.Name.Contains(keyword) ||
                product.Description.Contains(keyword) ||
                product.SuitableNeeds.Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }
        if (brandId.HasValue)
        {
            query = query.Where(product => product.BrandId == brandId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(product => product.Price >= minPrice.Value || product.SalePrice >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= maxPrice.Value || product.SalePrice <= maxPrice.Value);
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(product => product.SalePrice ?? product.Price),
            "price_desc" => query.OrderByDescending(product => product.SalePrice ?? product.Price),
            "popular" => query.OrderByDescending(product => product.SoldQuantity),
            "name" => query.OrderBy(product => product.Name),
            _ => query.OrderByDescending(product => product.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductCardViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                ImageUrl = product.MainImageUrl,
                OriginalPrice = product.Price,
                CurrentPrice = product.SalePrice.HasValue && product.SalePrice < product.Price &&
                               (!product.SaleStartAt.HasValue || product.SaleStartAt <= now) &&
                               (!product.SaleEndAt.HasValue || product.SaleEndAt >= now)
                    ? product.SalePrice.Value
                    : product.Price,
                StockQuantity = product.StockQuantity,
                AverageRating = product.Reviews.Where(review => review.IsVisible)
                    .Select(review => (double?)review.Rating)
                    .Average()
            })
            .ToListAsync();

        var model = new ProductCatalogViewModel
        {
            Search = search,
            CategoryId = categoryId,
            BrandId = brandId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort,
            Page = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            Products = products,
            Categories = await _dbContext.Categories.AsNoTracking().Where(item => item.IsActive)
                .OrderBy(item => item.Name).Select(item => new LookupItemViewModel { Id = item.Id, Name = item.Name }).ToListAsync(),
            Brands = await _dbContext.Brands.AsNoTracking().Where(item => item.IsActive)
                .OrderBy(item => item.Name).Select(item => new LookupItemViewModel { Id = item.Id, Name = item.Name }).ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
