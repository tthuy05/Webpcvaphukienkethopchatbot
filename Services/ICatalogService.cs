using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<CatalogItemViewModel>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogItemViewModel>> GetBrandsAsync(CancellationToken cancellationToken = default);

    Task<CatalogFormViewModel?> GetCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task<CatalogFormViewModel?> GetBrandAsync(int id, CancellationToken cancellationToken = default);

    Task SaveCategoryAsync(CatalogFormViewModel model, CancellationToken cancellationToken = default);

    Task SaveBrandAsync(CatalogFormViewModel model, CancellationToken cancellationToken = default);

    Task HideOrDeleteCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task HideOrDeleteBrandAsync(int id, CancellationToken cancellationToken = default);
}
