using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;
using Webpcvaphukienkethopchatbot.Services;
using Xunit;

namespace Webpcvaphukienkethopchatbot.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CheckoutAndCancel_PersistAndRestoreInventoryExactly()
    {
        await using var fixture = await TestFixture.CreateAsync(stockQuantity: 5, cartQuantity: 2);
        var service = fixture.CreateService();

        var checkout = await service.CheckoutAsync(
            fixture.UserId,
            "Test User",
            "0901234567",
            "Quan 1, TP HCM",
            PaymentMethod.CashOnDelivery);

        Assert.True(checkout.Succeeded);
        Assert.NotNull(checkout.OrderId);

        fixture.Context.ChangeTracker.Clear();
        var productAfterCheckout = await fixture.Context.Products.SingleAsync();
        var orderAfterCheckout = await fixture.Context.Orders
            .Include(order => order.Details)
            .Include(order => order.Payment)
            .SingleAsync();

        Assert.Equal(3, productAfterCheckout.StockQuantity);
        Assert.Equal(2, productAfterCheckout.SoldQuantity);
        Assert.Equal(OrderStatus.Pending, orderAfterCheckout.Status);
        Assert.Equal(2, orderAfterCheckout.Details.Single().Quantity);
        Assert.Equal(PaymentStatus.Unpaid, orderAfterCheckout.Payment!.Status);
        Assert.Empty(await fixture.Context.CartItems.ToListAsync());
        Assert.Single(await fixture.Context.InventoryTransactions.ToListAsync());
        Assert.Single(await fixture.Context.OrderStatusHistories.ToListAsync());

        var cancellation = await service.CancelAsync(checkout.OrderId!.Value, fixture.UserId);

        Assert.True(cancellation.Succeeded);
        fixture.Context.ChangeTracker.Clear();

        var productAfterCancellation = await fixture.Context.Products.SingleAsync();
        var orderAfterCancellation = await fixture.Context.Orders
            .Include(order => order.Payment)
            .SingleAsync();

        Assert.Equal(5, productAfterCancellation.StockQuantity);
        Assert.Equal(0, productAfterCancellation.SoldQuantity);
        Assert.Equal(OrderStatus.Cancelled, orderAfterCancellation.Status);
        Assert.Equal(PaymentStatus.Cancelled, orderAfterCancellation.Payment!.Status);
        Assert.Equal(2, await fixture.Context.InventoryTransactions.CountAsync());
        Assert.Equal(2, await fixture.Context.OrderStatusHistories.CountAsync());
    }

    [Fact]
    public async Task Checkout_WhenStockIsInsufficient_DoesNotChangeDatabase()
    {
        await using var fixture = await TestFixture.CreateAsync(stockQuantity: 2, cartQuantity: 3);
        var service = fixture.CreateService();

        var result = await service.CheckoutAsync(
            fixture.UserId,
            "Test User",
            "0901234567",
            "Quan 1, TP HCM",
            PaymentMethod.CashOnDelivery);

        Assert.False(result.Succeeded);
        fixture.Context.ChangeTracker.Clear();

        var product = await fixture.Context.Products.SingleAsync();
        Assert.Equal(2, product.StockQuantity);
        Assert.Equal(0, product.SoldQuantity);
        Assert.Empty(await fixture.Context.Orders.ToListAsync());
        Assert.Single(await fixture.Context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task Cancel_WhenCalledTwice_DoesNotRestoreInventoryTwice()
    {
        await using var fixture = await TestFixture.CreateAsync(stockQuantity: 4, cartQuantity: 1);
        var service = fixture.CreateService();
        var checkout = await service.CheckoutAsync(
            fixture.UserId,
            "Test User",
            "0901234567",
            "Quan 1, TP HCM",
            PaymentMethod.CashOnDelivery);

        var firstCancellation = await service.CancelAsync(checkout.OrderId!.Value, fixture.UserId);
        var secondCancellation = await service.CancelAsync(checkout.OrderId.Value, fixture.UserId);

        Assert.True(firstCancellation.Succeeded);
        Assert.False(secondCancellation.Succeeded);
        fixture.Context.ChangeTracker.Clear();

        var product = await fixture.Context.Products.SingleAsync();
        Assert.Equal(4, product.StockQuantity);
        Assert.Equal(0, product.SoldQuantity);
    }

    [Fact]
    public async Task Checkout_WithCoupon_PersistsDiscountAndRestoresUsageOnCancellation()
    {
        await using var fixture = await TestFixture.CreateAsync(stockQuantity: 4, cartQuantity: 1);
        fixture.Context.Coupons.Add(new Coupon
        {
            Code = "TEST10",
            DiscountType = DiscountType.Percentage,
            Value = 10,
            MinimumOrderAmount = 1,
            UsageLimit = 1,
            IsActive = true
        });
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();
        var service = fixture.CreateService();

        var checkout = await service.CheckoutAsync(
            fixture.UserId,
            "Test User",
            "0901234567",
            "Quan 1, TP HCM",
            PaymentMethod.CashOnDelivery,
            "test10");

        Assert.True(checkout.Succeeded);
        fixture.Context.ChangeTracker.Clear();
        var order = await fixture.Context.Orders.SingleAsync();
        var coupon = await fixture.Context.Coupons.SingleAsync();
        Assert.Equal(1_000_000m, order.DiscountAmount);
        Assert.Equal(9_030_000m, order.TotalAmount);
        Assert.Equal(1, coupon.UsedCount);

        Assert.True((await service.CancelAsync(order.Id, fixture.UserId)).Succeeded);
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(0, (await fixture.Context.Coupons.SingleAsync()).UsedCount);
    }

    [Fact]
    public async Task AdminStatusLifecycle_CompletesOrderAndMarksPaymentPaid()
    {
        await using var fixture = await TestFixture.CreateAsync(stockQuantity: 3, cartQuantity: 1);
        var service = fixture.CreateService();
        var checkout = await service.CheckoutAsync(
            fixture.UserId,
            "Test User",
            "0901234567",
            "Quan 1, TP HCM",
            PaymentMethod.CashOnDelivery);

        Assert.True((await service.ChangeStatusAsync(checkout.OrderId!.Value, OrderStatus.Confirmed, "admin")).Succeeded);
        Assert.True((await service.ChangeStatusAsync(checkout.OrderId.Value, OrderStatus.Shipping, "admin")).Succeeded);
        Assert.True((await service.ChangeStatusAsync(checkout.OrderId.Value, OrderStatus.Completed, "admin")).Succeeded);
        Assert.False((await service.ChangeStatusAsync(checkout.OrderId.Value, OrderStatus.Pending, "admin")).Succeeded);

        fixture.Context.ChangeTracker.Clear();
        var order = await fixture.Context.Orders.Include(item => item.Payment).SingleAsync();
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.Payment!.Status);
        Assert.NotNull(order.TrackingCode);
        Assert.Equal(4, await fixture.Context.OrderStatusHistories.CountAsync());
    }

    [Fact]
    public async Task ExpiredPendingOrder_IsCancelledAndRestocked()
    {
        await using var fixture = await TestFixture.CreateAsync(stockQuantity: 3, cartQuantity: 2);
        var service = fixture.CreateService();
        var checkout = await service.CheckoutAsync(
            fixture.UserId,
            "Test User",
            "0901234567",
            "Quan 1, TP HCM",
            PaymentMethod.CashOnDelivery);
        await fixture.Context.Orders.Where(order => order.Id == checkout.OrderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.CreatedAt, DateTime.UtcNow.AddDays(-2)));

        var cancelled = await service.CancelExpiredOrdersAsync(DateTime.UtcNow.AddDays(-1));

        Assert.Equal(1, cancelled);
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(OrderStatus.Cancelled, (await fixture.Context.Orders.SingleAsync()).Status);
        var product = await fixture.Context.Products.SingleAsync();
        Assert.Equal(3, product.StockQuantity);
        Assert.Equal(0, product.SoldQuantity);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestFixture(SqliteConnection connection, ApplicationDbContext context, string userId)
        {
            _connection = connection;
            Context = context;
            UserId = userId;
        }

        public ApplicationDbContext Context { get; }

        public string UserId { get; }

        public static async Task<TestFixture> CreateAsync(int stockQuantity, int cartQuantity)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "test@example.test",
                NormalizedUserName = "TEST@EXAMPLE.TEST",
                Email = "test@example.test",
                NormalizedEmail = "TEST@EXAMPLE.TEST",
                FullName = "Test User",
                IsActive = true
            };
            var category = new Category { Name = "Laptop", Slug = "laptop" };
            var brand = new Brand { Name = "Test Brand", Slug = "test-brand" };
            var product = new Product
            {
                Name = "Test Laptop",
                Slug = "test-laptop",
                Category = category,
                Brand = brand,
                Price = 10_000_000,
                StockQuantity = stockQuantity,
                SoldQuantity = 0,
                MainImageUrl = "/images/products/placeholder.svg",
                IsActive = true
            };
            var cart = new Cart
            {
                ApplicationUser = user,
                Items =
                {
                    new CartItem
                    {
                        Product = product,
                        Quantity = cartQuantity,
                        UnitPrice = product.Price
                    }
                }
            };

            context.Carts.Add(cart);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            return new TestFixture(connection, context, user.Id);
        }

        public OrderService CreateService() => new(
            Context,
            Options.Create(new CommerceOptions()),
            NullLogger<OrderService>.Instance);

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
