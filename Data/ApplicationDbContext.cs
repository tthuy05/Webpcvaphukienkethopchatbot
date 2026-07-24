using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Models;

namespace Webpcvaphukienkethopchatbot.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<ChatbotLog> ChatbotLogs => Set<ChatbotLog>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<EmailOutbox> EmailOutbox => Set<EmailOutbox>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(160);
            entity.Property(user => user.Address).HasMaxLength(500);
        });

        builder.Entity<Category>(entity =>
        {
            entity.HasIndex(category => category.Slug).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Slug).HasMaxLength(140).IsRequired();
        });

        builder.Entity<Brand>(entity =>
        {
            entity.HasIndex(brand => brand.Slug).IsUnique();
            entity.Property(brand => brand.Name).HasMaxLength(120).IsRequired();
            entity.Property(brand => brand.Slug).HasMaxLength(140).IsRequired();
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(product => product.Slug).IsUnique();
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.SalePrice).HasPrecision(18, 2);
            entity.Property(product => product.Name).HasMaxLength(220).IsRequired();
            entity.Property(product => product.Slug).HasMaxLength(240).IsRequired();
            entity.Property(product => product.MainImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(2500);
            entity.Property(product => product.SuitableNeeds).HasMaxLength(500);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
                table.HasCheckConstraint("CK_Products_Stock_NonNegative", "[StockQuantity] >= 0");
                table.HasCheckConstraint("CK_Products_Sold_NonNegative", "[SoldQuantity] >= 0");
                table.HasCheckConstraint("CK_Products_SalePrice_Valid", "[SalePrice] IS NULL OR ([SalePrice] >= 0 AND [SalePrice] < [Price])");
            });

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(product => product.Brand)
                .WithMany(brand => brand.Products)
                .HasForeignKey(product => product.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductImage>(entity =>
        {
            entity.Property(image => image.ImageUrl).HasMaxLength(500).IsRequired();
            entity.HasOne(image => image.Product)
                .WithMany(product => product.Images)
                .HasForeignKey(image => image.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Cart>(entity =>
        {
            entity.HasIndex(cart => cart.ApplicationUserId).IsUnique();
            entity.HasOne(cart => cart.ApplicationUser)
                .WithOne(user => user.Cart)
                .HasForeignKey<Cart>(cart => cart.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_CartItems_Quantity_Positive", "[Quantity] > 0");
                table.HasCheckConstraint("CK_CartItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            });

            entity.HasOne(item => item.Cart)
                .WithMany(cart => cart.Items)
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany(product => product.CartItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(order => order.OrderNumber).IsUnique();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.SubtotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.DiscountAmount).HasPrecision(18, 2);
            entity.Property(order => order.ShippingFee).HasPrecision(18, 2);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
                table.HasCheckConstraint("CK_Orders_SubtotalAmount_NonNegative", "[SubtotalAmount] >= 0");
                table.HasCheckConstraint("CK_Orders_DiscountAmount_NonNegative", "[DiscountAmount] >= 0");
                table.HasCheckConstraint("CK_Orders_ShippingFee_NonNegative", "[ShippingFee] >= 0");
            });

            entity.HasOne(order => order.ApplicationUser)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(order => order.Coupon)
                .WithMany(coupon => coupon.Orders)
                .HasForeignKey(order => order.CouponId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<OrderDetail>(entity =>
        {
            entity.Property(detail => detail.UnitPrice).HasPrecision(18, 2);
            entity.Property(detail => detail.LineTotal).HasPrecision(18, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_OrderDetails_Quantity_Positive", "[Quantity] > 0");
                table.HasCheckConstraint("CK_OrderDetails_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                table.HasCheckConstraint("CK_OrderDetails_LineTotal_NonNegative", "[LineTotal] >= 0");
            });

            entity.HasOne(detail => detail.Order)
                .WithMany(order => order.Details)
                .HasForeignKey(detail => detail.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(detail => detail.Product)
                .WithMany(product => product.OrderDetails)
                .HasForeignKey(detail => detail.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasIndex(payment => payment.OrderId).IsUnique();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(40);
            entity.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30);
            entity.ToTable(table => table.HasCheckConstraint("CK_Payments_Amount_NonNegative", "[Amount] >= 0"));

            entity.HasOne(payment => payment.Order)
                .WithOne(order => order.Payment)
                .HasForeignKey<Payment>(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChatbotLog>(entity =>
        {
            entity.Property(log => log.DetectedBudget).HasPrecision(18, 2);
            entity.HasOne(log => log.ApplicationUser)
                .WithMany(user => user.ChatbotLogs)
                .HasForeignKey(log => log.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UserPreference>(entity =>
        {
            entity.HasIndex(preference => preference.ApplicationUserId).IsUnique();
            entity.Property(preference => preference.PreferredLanguage).HasMaxLength(10).IsRequired();
            entity.Property(preference => preference.PreferredTheme).HasMaxLength(20).IsRequired();

            entity.HasOne(preference => preference.ApplicationUser)
                .WithOne(user => user.Preference)
                .HasForeignKey<UserPreference>(preference => preference.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasIndex(transaction => new { transaction.ProductId, transaction.CreatedAt });
            entity.Property(transaction => transaction.Type).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(transaction => transaction.Product)
                .WithMany(product => product.InventoryTransactions)
                .HasForeignKey(transaction => transaction.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasIndex(history => new { history.OrderId, history.ChangedAt });
            entity.Property(history => history.FromStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(history => history.ToStatus).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(history => history.Order)
                .WithMany(order => order.StatusHistory)
                .HasForeignKey(history => history.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(coupon => coupon.Code).IsUnique();
            entity.Property(coupon => coupon.DiscountType).HasConversion<string>().HasMaxLength(30);
            entity.Property(coupon => coupon.Value).HasPrecision(18, 2);
            entity.Property(coupon => coupon.MinimumOrderAmount).HasPrecision(18, 2);
            entity.Property(coupon => coupon.MaximumDiscountAmount).HasPrecision(18, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Coupons_Value_Positive", "[Value] > 0");
                table.HasCheckConstraint("CK_Coupons_Counts_NonNegative", "[UsedCount] >= 0 AND ([UsageLimit] IS NULL OR [UsageLimit] >= 0)");
                table.HasCheckConstraint("CK_Coupons_Percentage_Valid", "[DiscountType] <> 'Percentage' OR [Value] <= 100");
            });
        });

        builder.Entity<ProductReview>(entity =>
        {
            entity.HasIndex(review => new { review.ApplicationUserId, review.ProductId }).IsUnique();
            entity.HasIndex(review => new { review.ProductId, review.CreatedAt });
            entity.ToTable(table => table.HasCheckConstraint("CK_ProductReviews_Rating", "[Rating] BETWEEN 1 AND 5"));
            entity.HasOne(review => review.Product)
                .WithMany(product => product.Reviews)
                .HasForeignKey(review => review.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(review => review.ApplicationUser)
                .WithMany(user => user.ProductReviews)
                .HasForeignKey(review => review.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WishlistItem>(entity =>
        {
            entity.HasIndex(item => new { item.ApplicationUserId, item.ProductId }).IsUnique();
            entity.HasOne(item => item.Product)
                .WithMany(product => product.WishlistItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.ApplicationUser)
                .WithMany(user => user.WishlistItems)
                .HasForeignKey(item => item.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(log => log.CreatedAt);
            entity.HasIndex(log => new { log.ApplicationUserId, log.CreatedAt });
        });

        builder.Entity<EmailOutbox>(entity =>
        {
            entity.HasIndex(email => new { email.Recipient, email.CreatedAt });
        });
    }
}
