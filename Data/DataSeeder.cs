using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Webpcvaphukienkethopchatbot.Models;
using Webpcvaphukienkethopchatbot.Models.Enums;

namespace Webpcvaphukienkethopchatbot.Data;

public static class DataSeeder
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin@123";
    public const string UserEmail = "user@example.com";
    public const string UserPassword = "User@123";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        var admin = await SeedUserAsync(userManager, AdminEmail, AdminPassword, "Demo Admin", "Admin");
        var user = await SeedUserAsync(userManager, UserEmail, UserPassword, "Demo User", "User");

        await SeedCatalogAsync(dbContext, cancellationToken);
        await SeedPreferencesAsync(dbContext, admin.Id, user.Id, cancellationToken);
        await SeedSampleOrdersAsync(dbContext, user.Id, cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<ApplicationUser> SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                PhoneNumberConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Cannot seed user {email}: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(cancellationToken))
        {
            dbContext.Categories.AddRange(
                new Category { Name = "Laptop", Slug = "laptop", Description = "May tinh xach tay" },
                new Category { Name = "Phu kien", Slug = "phu-kien", Description = "Phu kien may tinh" },
                new Category { Name = "Chuot", Slug = "chuot", Description = "Chuot van phong va gaming" },
                new Category { Name = "Ban phim", Slug = "ban-phim", Description = "Ban phim co va ban phim van phong" },
                new Category { Name = "Tai nghe", Slug = "tai-nghe", Description = "Tai nghe lam viec va giai tri" },
                new Category { Name = "Balo", Slug = "balo", Description = "Balo laptop" },
                new Category { Name = "Man hinh", Slug = "man-hinh", Description = "Man hinh may tinh" });
        }

        if (!await dbContext.Brands.AnyAsync(cancellationToken))
        {
            dbContext.Brands.AddRange(
                new Brand { Name = "Asus", Slug = "asus" },
                new Brand { Name = "Acer", Slug = "acer" },
                new Brand { Name = "Lenovo", Slug = "lenovo" },
                new Brand { Name = "Dell", Slug = "dell" },
                new Brand { Name = "HP", Slug = "hp" },
                new Brand { Name = "MSI", Slug = "msi" },
                new Brand { Name = "Logitech", Slug = "logitech" },
                new Brand { Name = "Razer", Slug = "razer" });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = await dbContext.Categories.ToDictionaryAsync(category => category.Slug, cancellationToken);
        var brands = await dbContext.Brands.ToDictionaryAsync(brand => brand.Slug, cancellationToken);
        var laptopImage = "/images/products/placeholder-laptop.svg";
        var accessoryImage = "/images/products/placeholder-accessory.svg";

        var products = new List<Product>
        {
            Laptop("Acer Aspire 3 A315", "acer-aspire-3-a315", "acer", 8990000, "Intel Core i3-1215U", "8GB", "256GB", "Intel UHD", "15.6 inch FHD", "40Wh", "1.7kg", "Windows 11", 18, "Laptop gia tot cho sinh vien va van phong co ban.", "sinh vien;van phong", laptopImage),
            Laptop("HP 14s Ryzen 3", "hp-14s-ryzen-3", "hp", 9500000, "AMD Ryzen 3 7320U", "8GB", "512GB", "AMD Radeon", "14 inch FHD", "41Wh", "1.46kg", "Windows 11", 16, "May mong nhe, pin tot, phu hop hoc tap.", "sinh vien;mong nhe;van phong", laptopImage),
            Laptop("Lenovo IdeaPad Slim 3", "lenovo-ideapad-slim-3", "lenovo", 10990000, "Intel Core i5-12450H", "8GB", "512GB", "Intel UHD", "15.6 inch FHD", "47Wh", "1.62kg", "Windows 11", 20, "Cau hinh can bang cho hoc tap va lam viec.", "sinh vien;van phong;lap trinh", laptopImage),
            Laptop("Asus Vivobook 15 X1504", "asus-vivobook-15-x1504", "asus", 11990000, "Intel Core i5-1335U", "16GB", "512GB", "Intel Iris Xe", "15.6 inch FHD", "42Wh", "1.7kg", "Windows 11", 12, "RAM 16GB san sang cho lap trinh co ban.", "hoc IT;lap trinh;van phong", laptopImage),
            Laptop("Dell Inspiron 15 3520", "dell-inspiron-15-3520", "dell", 12990000, "Intel Core i5-1235U", "16GB", "512GB", "Intel Iris Xe", "15.6 inch FHD 120Hz", "41Wh", "1.83kg", "Windows 11", 13, "Do ben tot, phu hop hoc IT va van phong.", "hoc IT;lap trinh;van phong", laptopImage),
            Laptop("Acer Swift Go 14", "acer-swift-go-14", "acer", 14990000, "Intel Core i5-13500H", "16GB", "512GB", "Intel Iris Xe", "14 inch 2.8K", "65Wh", "1.32kg", "Windows 11", 10, "Man hinh dep, mong nhe, hieu nang tot.", "mong nhe;lap trinh;sinh vien", laptopImage),
            Laptop("HP Pavilion 15", "hp-pavilion-15", "hp", 15990000, "Intel Core i5-1340P", "16GB", "512GB", "Intel Iris Xe", "15.6 inch FHD", "43Wh", "1.75kg", "Windows 11", 14, "Laptop da dung cho hoc tap va lam viec.", "hoc IT;lap trinh;van phong", laptopImage),
            Laptop("Lenovo ThinkBook 14 G6", "lenovo-thinkbook-14-g6", "lenovo", 16990000, "AMD Ryzen 5 7530U", "16GB", "512GB", "AMD Radeon", "14 inch FHD", "60Wh", "1.38kg", "Windows 11 Pro", 11, "Ban phim tot, hop cho lap trinh vien moi.", "hoc IT;lap trinh;mong nhe", laptopImage),
            Laptop("Asus TUF Gaming F15", "asus-tuf-gaming-f15", "asus", 18990000, "Intel Core i5-12500H", "16GB", "512GB", "NVIDIA RTX 3050", "15.6 inch FHD 144Hz", "56Wh", "2.2kg", "Windows 11", 9, "Gaming pho thong, xu ly do hoa va lap trinh on.", "gaming;do hoa;lap trinh", laptopImage),
            Laptop("MSI Thin 15 B12UC", "msi-thin-15-b12uc", "msi", 19990000, "Intel Core i7-12650H", "16GB", "512GB", "NVIDIA RTX 3050", "15.6 inch FHD 144Hz", "52Wh", "1.86kg", "Windows 11", 8, "Hieu nang manh trong tam gia duoi 20 trieu.", "gaming;do hoa;hoc IT", laptopImage),
            Laptop("Dell Vostro 5630", "dell-vostro-5630", "dell", 20990000, "Intel Core i7-1360P", "16GB", "1TB", "Intel Iris Xe", "16 inch FHD+", "54Wh", "1.9kg", "Windows 11 Pro", 7, "Man hinh lon, SSD 1TB cho cong viec nhieu du lieu.", "van phong;lap trinh;do hoa", laptopImage),
            Laptop("Asus Zenbook 14 OLED", "asus-zenbook-14-oled", "asus", 22990000, "Intel Core Ultra 5 125H", "16GB", "512GB", "Intel Arc", "14 inch OLED 3K", "75Wh", "1.2kg", "Windows 11", 6, "Mong nhe, man OLED dep cho hoc tap va sang tao.", "mong nhe;do hoa;lap trinh", laptopImage),
            Laptop("Lenovo LOQ 15", "lenovo-loq-15", "lenovo", 23990000, "Intel Core i5-13450HX", "16GB", "512GB", "NVIDIA RTX 4050", "15.6 inch FHD 144Hz", "60Wh", "2.4kg", "Windows 11", 6, "Gaming tam trung, GPU RTX 4050 tot cho do hoa.", "gaming;do hoa;lap trinh", laptopImage),
            Laptop("Acer Nitro V 15", "acer-nitro-v-15", "acer", 24990000, "Intel Core i7-13620H", "16GB", "512GB", "NVIDIA RTX 4050", "15.6 inch FHD 144Hz", "57Wh", "2.1kg", "Windows 11", 5, "Hieu nang cao cho game va render co ban.", "gaming;do hoa;hoc IT", laptopImage),
            Laptop("MSI Cyborg 15", "msi-cyborg-15", "msi", 26990000, "Intel Core i7-13620H", "16GB", "1TB", "NVIDIA RTX 4060", "15.6 inch FHD 144Hz", "53.5Wh", "1.98kg", "Windows 11", 4, "RTX 4060 cho gaming va do hoa nang hon.", "gaming;do hoa", laptopImage),
            Laptop("HP Envy x360 14", "hp-envy-x360-14", "hp", 27990000, "Intel Core Ultra 7 155U", "16GB", "1TB", "Intel Graphics", "14 inch OLED touch", "59Wh", "1.39kg", "Windows 11", 4, "May gap xoay linh hoat, hop sinh vien sang tao.", "mong nhe;sinh vien;do hoa", laptopImage),
            Laptop("Dell XPS 13 Plus", "dell-xps-13-plus", "dell", 32990000, "Intel Core i7-1360P", "16GB", "1TB", "Intel Iris Xe", "13.4 inch 3.5K OLED", "55Wh", "1.26kg", "Windows 11 Pro", 3, "Cao cap, mong nhe, man hinh sac net.", "mong nhe;van phong;lap trinh", laptopImage),
            Laptop("Lenovo Legion Slim 5", "lenovo-legion-slim-5", "lenovo", 34990000, "AMD Ryzen 7 7840HS", "32GB", "1TB", "NVIDIA RTX 4060", "16 inch 2.5K 165Hz", "80Wh", "2.3kg", "Windows 11", 3, "RAM 32GB, man hinh 2.5K cho game va do hoa.", "gaming;do hoa;lap trinh", laptopImage),
            Laptop("Asus ROG Zephyrus G14", "asus-rog-zephyrus-g14", "asus", 42990000, "AMD Ryzen 9 8945HS", "32GB", "1TB", "NVIDIA RTX 4070", "14 inch OLED 120Hz", "73Wh", "1.5kg", "Windows 11", 2, "Gaming mong nhe cao cap, man OLED dep.", "gaming;do hoa;mong nhe", laptopImage),
            Laptop("MSI Creator Z16", "msi-creator-z16", "msi", 45990000, "Intel Core i9-13900H", "32GB", "1TB", "NVIDIA RTX 4070", "16 inch QHD+ 165Hz", "90Wh", "2.35kg", "Windows 11 Pro", 2, "May sang tao noi dung, render va thiet ke do hoa.", "do hoa;gaming;lap trinh", laptopImage),
            Accessory("Logitech M331 Silent Plus", "logitech-m331-silent-plus", "logitech", "chuot", 390000, "Chuot khong day em, phu hop van phong.", 50, accessoryImage),
            Accessory("Logitech MX Master 3S", "logitech-mx-master-3s", "logitech", "chuot", 2490000, "Chuot cao cap cho lap trinh va sang tao.", 22, accessoryImage),
            Accessory("Razer DeathAdder V3", "razer-deathadder-v3", "razer", "chuot", 1590000, "Chuot gaming nhe, cam bien chinh xac.", 18, accessoryImage),
            Accessory("Logitech K380", "logitech-k380", "logitech", "ban-phim", 790000, "Ban phim Bluetooth nho gon.", 35, accessoryImage),
            Accessory("Razer BlackWidow V4", "razer-blackwidow-v4", "razer", "ban-phim", 3290000, "Ban phim co gaming day du tinh nang.", 12, accessoryImage),
            Accessory("Logitech H390", "logitech-h390", "logitech", "tai-nghe", 690000, "Tai nghe USB cho hoc online va hop.", 28, accessoryImage),
            Accessory("Razer Kraken V3", "razer-kraken-v3", "razer", "tai-nghe", 1990000, "Tai nghe gaming am thanh manh.", 14, accessoryImage),
            Accessory("Balo Lenovo 15.6", "balo-lenovo-15-6", "lenovo", "balo", 590000, "Balo laptop chong soc co ban.", 40, accessoryImage),
            Accessory("Dell Pro Backpack", "dell-pro-backpack", "dell", "balo", 1190000, "Balo cong tac ben, nhieu ngan.", 20, accessoryImage),
            Accessory("Acer EK241Y 24 inch", "acer-ek241y-24-inch", "acer", "man-hinh", 2790000, "Man hinh 24 inch FHD cho hoc tap va lam viec.", 16, accessoryImage)
        };

        foreach (var product in products)
        {
            dbContext.Products.Add(product);
            dbContext.ProductImages.Add(new ProductImage
            {
                Product = product,
                ImageUrl = product.MainImageUrl,
                AltText = product.Name,
                IsPrimary = true,
                SortOrder = 1
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        Product Laptop(
            string name,
            string slug,
            string brandSlug,
            decimal price,
            string cpu,
            string ram,
            string ssd,
            string gpu,
            string screen,
            string battery,
            string weight,
            string os,
            int stock,
            string description,
            string needs,
            string imageUrl) => new()
            {
                Name = name,
                Slug = slug,
                CategoryId = categories["laptop"].Id,
                BrandId = brands[brandSlug].Id,
                Price = price,
                Cpu = cpu,
                Ram = ram,
                Ssd = ssd,
                Gpu = gpu,
                Screen = screen,
                Battery = battery,
                Weight = weight,
                OperatingSystem = os,
                StockQuantity = stock,
                Description = description,
                SuitableNeeds = needs,
                MainImageUrl = imageUrl
            };

        Product Accessory(
            string name,
            string slug,
            string brandSlug,
            string categorySlug,
            decimal price,
            string description,
            int stock,
            string imageUrl) => new()
            {
                Name = name,
                Slug = slug,
                CategoryId = categories[categorySlug].Id,
                BrandId = brands[brandSlug].Id,
                Price = price,
                StockQuantity = stock,
                Description = description,
                SuitableNeeds = "phu kien",
                MainImageUrl = imageUrl
            };
    }

    private static async Task SeedPreferencesAsync(ApplicationDbContext dbContext, string adminId, string userId, CancellationToken cancellationToken)
    {
        foreach (var userIdValue in new[] { adminId, userId })
        {
            if (!await dbContext.UserPreferences.AnyAsync(preference => preference.ApplicationUserId == userIdValue, cancellationToken))
            {
                dbContext.UserPreferences.Add(new UserPreference
                {
                    ApplicationUserId = userIdValue,
                    PreferredLanguage = "vi-VN",
                    PreferredTheme = "light"
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSampleOrdersAsync(ApplicationDbContext dbContext, string userId, CancellationToken cancellationToken)
    {
        if (await dbContext.Orders.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = await dbContext.Products
            .OrderBy(product => product.Price)
            .Take(4)
            .ToListAsync(cancellationToken);

        if (products.Count < 2)
        {
            return;
        }

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ApplicationUserId = userId,
            ReceiverName = "Demo User",
            PhoneNumber = "0900000000",
            ShippingAddress = "Quan 1, TP Ho Chi Minh",
            Status = OrderStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        foreach (var product in products.Take(2))
        {
            var quantity = product.Price < 1000000 ? 2 : 1;
            order.Details.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity,
                LineTotal = product.Price * quantity
            });
            product.SoldQuantity += quantity;
            product.StockQuantity -= quantity;
        }

        order.TotalAmount = order.Details.Sum(detail => detail.LineTotal);
        order.Payment = new Payment
        {
            Method = PaymentMethod.CashOnDelivery,
            Status = PaymentStatus.Paid,
            Amount = order.TotalAmount,
            PaidAt = DateTime.UtcNow.AddDays(-1)
        };

        var pendingOrder = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow.AddSeconds(1):yyyyMMddHHmmss}",
            ApplicationUserId = userId,
            ReceiverName = "Demo User",
            PhoneNumber = "0900000000",
            ShippingAddress = "Thu Duc, TP Ho Chi Minh",
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var product in products.Skip(2).Take(2))
        {
            pendingOrder.Details.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1,
                LineTotal = product.Price
            });
            product.SoldQuantity += 1;
            product.StockQuantity -= 1;
        }

        pendingOrder.TotalAmount = pendingOrder.Details.Sum(detail => detail.LineTotal);
        pendingOrder.Payment = new Payment
        {
            Method = PaymentMethod.SimulatedBankTransfer,
            Status = PaymentStatus.Simulated,
            Amount = pendingOrder.TotalAmount,
            TransactionCode = "SIM-DEMO-001"
        };

        dbContext.Orders.AddRange(order, pendingOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
