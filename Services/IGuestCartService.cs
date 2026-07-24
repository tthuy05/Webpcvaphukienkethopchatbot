namespace Webpcvaphukienkethopchatbot.Services;

public interface IGuestCartService
{
    IReadOnlyList<GuestCartItem> GetItems();

    void Add(int productId, int quantity, int maximumQuantity);

    void Update(int productId, int quantity, int maximumQuantity);

    void Remove(int productId);

    void Clear();

    Task MergeIntoUserCartAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed record GuestCartItem(int ProductId, int Quantity);
