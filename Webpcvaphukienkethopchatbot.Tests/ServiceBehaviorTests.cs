using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Webpcvaphukienkethopchatbot.Data;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Services;
using Webpcvaphukienkethopchatbot.ViewModels;
using Xunit;

namespace Webpcvaphukienkethopchatbot.Tests;

public class ServiceBehaviorTests
{
    [Theory]
    [InlineData("Tôi có 15.5 triệu học IT, nên mua laptop nào?", 15_500_000)]
    [InlineData("Tôi có 15000000 học IT, nên mua laptop nào?", 15_000_000)]
    public async Task Chatbot_detects_common_budget_formats(string question, decimal expectedBudget)
    {
        await using var dbContext = CreateDbContext();
        SeedLaptopCatalog(dbContext);

        var service = new ChatbotService(dbContext);
        var response = await service.AskAsync(new(), question);

        Assert.Equal(expectedBudget, response.DetectedBudget);
        Assert.Contains(response.Suggestions, item => item.ProductName == "Laptop học IT");
    }

    [Fact]
    public async Task Product_service_can_hide_and_reactivate_product()
    {
        await using var dbContext = CreateDbContext();
        var category = new Category { Name = "Laptop", Slug = "laptop" };
        var brand = new Brand { Name = "Asus", Slug = "asus" };
        var product = new Product
        {
            Name = "Test laptop",
            Slug = "test-laptop",
            Category = category,
            Brand = brand,
            Price = 10_000_000m,
            StockQuantity = 3,
            Description = "Laptop test",
            SuitableNeeds = "hoc IT",
            IsActive = true
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext, new TestWebHostEnvironment());

        await service.SetActiveAsync(product.Id, false);
        Assert.False((await dbContext.Products.FindAsync(product.Id))!.IsActive);

        await service.SetActiveAsync(product.Id, true);
        Assert.True((await dbContext.Products.FindAsync(product.Id))!.IsActive);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SeedLaptopCatalog(ApplicationDbContext dbContext)
    {
        var category = new Category { Name = "Laptop", Slug = "laptop" };
        var brand = new Brand { Name = "Lenovo", Slug = "lenovo" };

        dbContext.Products.AddRange(
            new Product
            {
                Name = "Laptop học IT",
                Slug = "laptop-hoc-it",
                Category = category,
                Brand = brand,
                Price = 14_900_000m,
                StockQuantity = 5,
                Cpu = "Intel Core i5",
                Ram = "16GB",
                Ssd = "512GB",
                Description = "Phù hợp học IT và lập trình",
                SuitableNeeds = "hoc IT, lap trinh",
                IsActive = true
            },
            new Product
            {
                Name = "Laptop đồ họa",
                Slug = "laptop-do-hoa",
                Category = category,
                Brand = brand,
                Price = 22_000_000m,
                StockQuantity = 4,
                Cpu = "Intel Core i7",
                Ram = "16GB",
                Ssd = "1TB",
                Gpu = "RTX 4050",
                Description = "Phù hợp thiết kế đồ họa",
                SuitableNeeds = "do hoa",
                IsActive = true
            });

        dbContext.SaveChanges();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ApplicationName { get; set; } = "Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = "Development";
    }
}
