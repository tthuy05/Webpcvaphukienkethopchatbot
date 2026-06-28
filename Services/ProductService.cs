using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public partial class ProductService : IProductService
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public ProductService(ApplicationDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<IReadOnlyList<AdminProductItemViewModel>> GetAdminProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Brand)
            .Include(product => product.Category)
            .OrderByDescending(product => product.CreatedAt)
            .Select(product => new AdminProductItemViewModel
            {
                Id = product.Id,
                Name = product.Name,
                BrandName = product.Brand != null ? product.Brand.Name : string.Empty,
                CategoryName = product.Category != null ? product.Category.Name : string.Empty,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                MainImageUrl = product.MainImageUrl
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductFormViewModel> CreateFormAsync(CancellationToken cancellationToken = default)
    {
        var model = new ProductFormViewModel
        {
            Price = 0,
            StockQuantity = 0,
            IsActive = true
        };
        await PopulateSelectionsAsync(model, cancellationToken);
        return model;
    }

    public async Task<ProductFormViewModel?> EditFormAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Cpu = product.Cpu,
            Ram = product.Ram,
            Ssd = product.Ssd,
            Gpu = product.Gpu,
            Screen = product.Screen,
            Battery = product.Battery,
            Weight = product.Weight,
            OperatingSystem = product.OperatingSystem,
            Description = product.Description,
            SuitableNeeds = product.SuitableNeeds,
            IsActive = product.IsActive,
            ExistingImageUrl = product.MainImageUrl
        };

        await PopulateSelectionsAsync(model, cancellationToken);
        return model;
    }

    public async Task PopulateSelectionsAsync(ProductFormViewModel model, CancellationToken cancellationToken = default)
    {
        model.Categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive || category.Id == model.CategoryId)
            .OrderBy(category => category.Name)
            .Select(category => new SelectListItem(category.Name, category.Id.ToString(), category.Id == model.CategoryId))
            .ToListAsync(cancellationToken);

        model.Brands = await _dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive || brand.Id == model.BrandId)
            .OrderBy(brand => brand.Name)
            .Select(brand => new SelectListItem(brand.Name, brand.Id.ToString(), brand.Id == model.BrandId))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveAsync(ProductFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.Price < 0)
        {
            throw new InvalidOperationException("Gia san pham khong duoc am.");
        }

        if (model.StockQuantity < 0)
        {
            throw new InvalidOperationException("Ton kho khong duoc am.");
        }

        var categoryExists = await _dbContext.Categories.AnyAsync(category => category.Id == model.CategoryId, cancellationToken);
        var brandExists = await _dbContext.Brands.AnyAsync(brand => brand.Id == model.BrandId, cancellationToken);
        if (!categoryExists || !brandExists)
        {
            throw new InvalidOperationException("Danh muc hoac thuong hieu khong hop le.");
        }

        var slug = await CreateUniqueSlugAsync(model.Name, model.Id, cancellationToken);
        var imageUrl = model.ExistingImageUrl;
        if (model.ImageFile is { Length: > 0 })
        {
            imageUrl = await SaveImageAsync(model.ImageFile, cancellationToken);
        }

        var product = model.Id == 0
            ? new Product { CreatedAt = DateTime.UtcNow }
            : await _dbContext.Products.FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException("Khong tim thay san pham.");
        }

        product.Name = model.Name.Trim();
        product.Slug = slug;
        product.CategoryId = model.CategoryId;
        product.BrandId = model.BrandId;
        product.Price = model.Price;
        product.StockQuantity = model.StockQuantity;
        product.Cpu = model.Cpu?.Trim();
        product.Ram = model.Ram?.Trim();
        product.Ssd = model.Ssd?.Trim();
        product.Gpu = model.Gpu?.Trim();
        product.Screen = model.Screen?.Trim();
        product.Battery = model.Battery?.Trim();
        product.Weight = model.Weight?.Trim();
        product.OperatingSystem = model.OperatingSystem?.Trim();
        product.Description = model.Description.Trim();
        product.SuitableNeeds = model.SuitableNeeds.Trim();
        product.IsActive = model.IsActive;
        product.MainImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "/images/products/placeholder.svg" : imageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        if (model.Id == 0)
        {
            _dbContext.Products.Add(product);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsurePrimaryImageAsync(product, cancellationToken);

        return product.Id;
    }

    public async Task HideAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return;
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<Product> ActiveProductsQuery()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Brand)
            .Include(product => product.Category)
            .Where(product => product.IsActive);
    }

    public async Task<ProductFilterViewModel> SearchAsync(ProductFilterViewModel filter, CancellationToken cancellationToken = default)
    {
        var query = ActiveProductsQuery();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(product =>
                product.Name.Contains(keyword) ||
                product.Description.Contains(keyword) ||
                product.SuitableNeeds.Contains(keyword));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == filter.CategoryId.Value);
        }

        if (filter.BrandId.HasValue)
        {
            query = query.Where(product => product.BrandId == filter.BrandId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= filter.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Cpu))
        {
            query = query.Where(product => product.Cpu != null && product.Cpu.Contains(filter.Cpu.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Ram))
        {
            query = query.Where(product => product.Ram != null && product.Ram.Contains(filter.Ram.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Ssd))
        {
            query = query.Where(product => product.Ssd != null && product.Ssd.Contains(filter.Ssd.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Gpu))
        {
            query = query.Where(product => product.Gpu != null && product.Gpu.Contains(filter.Gpu.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.UsageNeed))
        {
            query = query.Where(product => product.SuitableNeeds.Contains(filter.UsageNeed.Trim()));
        }

        query = filter.SortBy switch
        {
            "price-asc" => query.OrderBy(product => product.Price),
            "price-desc" => query.OrderByDescending(product => product.Price),
            "best-selling" => query.OrderByDescending(product => product.SoldQuantity),
            _ => query.OrderByDescending(product => product.CreatedAt)
        };

        filter.Products = await query
            .Select(product => new ProductCardViewModel
            {
                Id = product.Id,
                Name = product.Name,
                BrandName = product.Brand != null ? product.Brand.Name : string.Empty,
                CategoryName = product.Category != null ? product.Category.Name : string.Empty,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                SoldQuantity = product.SoldQuantity,
                MainImageUrl = product.MainImageUrl,
                SuitableNeeds = product.SuitableNeeds
            })
            .ToListAsync(cancellationToken);

        filter.Categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new SelectListItem(category.Name, category.Id.ToString(), category.Id == filter.CategoryId))
            .ToListAsync(cancellationToken);

        filter.Brands = await _dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.Name)
            .Select(brand => new SelectListItem(brand.Name, brand.Id.ToString(), brand.Id == filter.BrandId))
            .ToListAsync(cancellationToken);

        return filter;
    }

    public async Task<ProductDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ActiveProductsQuery()
            .Where(product => product.Id == id)
            .Select(product => new ProductDetailViewModel
            {
                Id = product.Id,
                Name = product.Name,
                BrandName = product.Brand != null ? product.Brand.Name : string.Empty,
                CategoryName = product.Category != null ? product.Category.Name : string.Empty,
                Price = product.Price,
                MainImageUrl = product.MainImageUrl,
                Cpu = product.Cpu,
                Ram = product.Ram,
                Ssd = product.Ssd,
                Gpu = product.Gpu,
                Screen = product.Screen,
                Battery = product.Battery,
                Weight = product.Weight,
                OperatingSystem = product.OperatingSystem,
                StockQuantity = product.StockQuantity,
                Description = product.Description,
                SuitableNeeds = product.SuitableNeeds
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsurePrimaryImageAsync(Product product, CancellationToken cancellationToken)
    {
        var image = await _dbContext.ProductImages
            .FirstOrDefaultAsync(item => item.ProductId == product.Id && item.IsPrimary, cancellationToken);

        if (image is null)
        {
            _dbContext.ProductImages.Add(new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = product.MainImageUrl,
                AltText = product.Name,
                IsPrimary = true,
                SortOrder = 1
            });
        }
        else
        {
            image.ImageUrl = product.MainImageUrl;
            image.AltText = product.Name;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> SaveImageAsync(IFormFile imageFile, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(imageFile.FileName);
        if (!AllowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Dinh dang anh khong hop le.");
        }

        var imagesDirectory = Path.Combine(_environment.WebRootPath, "images", "products");
        Directory.CreateDirectory(imagesDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(imagesDirectory, fileName);

        await using var stream = File.Create(filePath);
        await imageFile.CopyToAsync(stream, cancellationToken);

        return $"/images/products/{fileName}";
    }

    private async Task<string> CreateUniqueSlugAsync(string name, int currentProductId, CancellationToken cancellationToken)
    {
        var baseSlug = CreateSlug(name);
        var slug = baseSlug;
        var index = 2;

        while (await _dbContext.Products.AnyAsync(product => product.Slug == slug && product.Id != currentProductId, cancellationToken))
        {
            slug = $"{baseSlug}-{index}";
            index++;
        }

        return slug;
    }

    private static string CreateSlug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = InvalidSlugCharacters().Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-");
        slug = DuplicateDash().Replace(slug, "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidSlugCharacters();

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateDash();
}
