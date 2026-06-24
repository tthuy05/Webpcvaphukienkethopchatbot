using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.ViewModels;

namespace Webpcvaphukienkethopchatbot.Services;

public interface IProductService
{
    Task<IReadOnlyList<AdminProductItemViewModel>> GetAdminProductsAsync(CancellationToken cancellationToken = default);

    Task<ProductFormViewModel> CreateFormAsync(CancellationToken cancellationToken = default);

    Task<ProductFormViewModel?> EditFormAsync(int id, CancellationToken cancellationToken = default);

    Task PopulateSelectionsAsync(ProductFormViewModel model, CancellationToken cancellationToken = default);

    Task<int> SaveAsync(ProductFormViewModel model, CancellationToken cancellationToken = default);

    Task HideAsync(int id, CancellationToken cancellationToken = default);

    IQueryable<Product> ActiveProductsQuery();
}
