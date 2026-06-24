using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public partial class CatalogService : ICatalogService
{
    private readonly ApplicationDbContext _dbContext;

    public CatalogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CatalogItemViewModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Select(category => new CatalogItemViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                IsActive = category.IsActive,
                ProductCount = category.Products.Count
            })
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogItemViewModel>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Brands
            .Select(brand => new CatalogItemViewModel
            {
                Id = brand.Id,
                Name = brand.Name,
                Slug = brand.Slug,
                Description = brand.Description,
                IsActive = brand.IsActive,
                ProductCount = brand.Products.Count
            })
            .OrderBy(brand => brand.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogFormViewModel?> GetCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Where(category => category.Id == id)
            .Select(category => new CatalogFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CatalogFormViewModel?> GetBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Brands
            .Where(brand => brand.Id == id)
            .Select(brand => new CatalogFormViewModel
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description,
                IsActive = brand.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveCategoryAsync(CatalogFormViewModel model, CancellationToken cancellationToken = default)
    {
        var slug = CreateSlug(model.Name);
        if (await _dbContext.Categories.AnyAsync(category => category.Slug == slug && category.Id != model.Id, cancellationToken))
        {
            throw new InvalidOperationException("Danh muc da ton tai.");
        }

        var category = model.Id == 0
            ? new Category { CreatedAt = DateTime.UtcNow }
            : await _dbContext.Categories.FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken);

        if (category is null)
        {
            throw new KeyNotFoundException("Khong tim thay danh muc.");
        }

        category.Name = model.Name.Trim();
        category.Slug = slug;
        category.Description = model.Description?.Trim();
        category.IsActive = model.IsActive;

        if (model.Id == 0)
        {
            _dbContext.Categories.Add(category);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveBrandAsync(CatalogFormViewModel model, CancellationToken cancellationToken = default)
    {
        var slug = CreateSlug(model.Name);
        if (await _dbContext.Brands.AnyAsync(brand => brand.Slug == slug && brand.Id != model.Id, cancellationToken))
        {
            throw new InvalidOperationException("Thuong hieu da ton tai.");
        }

        var brand = model.Id == 0
            ? new Brand { CreatedAt = DateTime.UtcNow }
            : await _dbContext.Brands.FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken);

        if (brand is null)
        {
            throw new KeyNotFoundException("Khong tim thay thuong hieu.");
        }

        brand.Name = model.Name.Trim();
        brand.Slug = slug;
        brand.Description = model.Description?.Trim();
        brand.IsActive = model.IsActive;

        if (model.Id == 0)
        {
            _dbContext.Brands.Add(brand);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HideOrDeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return;
        }

        if (category.Products.Any())
        {
            category.IsActive = false;
        }
        else
        {
            _dbContext.Categories.Remove(category);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HideOrDeleteBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _dbContext.Brands
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (brand is null)
        {
            return;
        }

        if (brand.Products.Any())
        {
            brand.IsActive = false;
        }
        else
        {
            _dbContext.Brands.Remove(brand);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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
