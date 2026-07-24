using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed class GuestCartService : IGuestCartService
{
    private const string SessionKey = "GuestCart";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _dbContext;

    public GuestCartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public IReadOnlyList<GuestCartItem> GetItems() => Load();

    public void Add(int productId, int quantity, int maximumQuantity)
    {
        var items = Load();
        var existing = items.Find(item => item.ProductId == productId);
        var newQuantity = Math.Min(maximumQuantity, (existing?.Quantity ?? 0) + quantity);
        if (existing is null) items.Add(new GuestCartItem(productId, newQuantity));
        else items[items.IndexOf(existing)] = existing with { Quantity = newQuantity };
        Save(items);
    }

    public void Update(int productId, int quantity, int maximumQuantity)
    {
        var items = Load();
        var existing = items.Find(item => item.ProductId == productId);
        if (existing is null) return;
        if (quantity <= 0) items.Remove(existing);
        else items[items.IndexOf(existing)] = existing with { Quantity = Math.Min(quantity, maximumQuantity) };
        Save(items);
    }

    public void Remove(int productId)
    {
        var items = Load();
        items.RemoveAll(item => item.ProductId == productId);
        Save(items);
    }

    public void Clear() => Session.Remove(SessionKey);

    public async Task MergeIntoUserCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        var guestItems = Load();
        if (guestItems.Count == 0) return;

        var productIds = guestItems.Select(item => item.ProductId).ToList();
        var products = await _dbContext.Products
            .Where(product => productIds.Contains(product.Id) && product.IsActive)
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        var cart = await _dbContext.Carts.Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.ApplicationUserId == userId, cancellationToken);
        if (cart is null)
        {
            cart = new Cart { ApplicationUserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _dbContext.Carts.Add(cart);
        }

        foreach (var guestItem in guestItems)
        {
            if (!products.TryGetValue(guestItem.ProductId, out var product) || product.StockQuantity <= 0) continue;
            var current = cart.Items.SingleOrDefault(item => item.ProductId == guestItem.ProductId);
            var quantity = Math.Min(product.StockQuantity, guestItem.Quantity + (current?.Quantity ?? 0));
            if (current is null)
            {
                cart.Items.Add(new CartItem { ProductId = product.Id, Quantity = quantity, UnitPrice = product.GetCurrentPrice(DateTime.UtcNow) });
            }
            else
            {
                current.Quantity = quantity;
                current.UnitPrice = product.GetCurrentPrice(DateTime.UtcNow);
            }
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        Clear();
    }

    private List<GuestCartItem> Load()
    {
        var json = Session.GetString(SessionKey);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<GuestCartItem>>(json) ?? [];
    }

    private void Save(List<GuestCartItem> items) =>
        Session.SetString(SessionKey, JsonSerializer.Serialize(items));

    private ISession Session => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Session is not available for guest cart.");
}
