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
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
            entity.ToTable(table => table.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0"));

            entity.HasOne(order => order.ApplicationUser)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);
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
    }
}
