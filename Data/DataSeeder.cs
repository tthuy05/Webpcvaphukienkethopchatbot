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
        await SeedCouponsAsync(dbContext, cancellationToken);
        await SeedPreferencesAsync(dbContext, admin.Id, user.Id, cancellationToken);
        await SeedSampleOrdersAsync(dbContext, user.Id, cancellationToken);
    }

    private static async Task SeedCouponsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Coupons.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Coupons.Add(new Coupon
        {
            Code = "DEMO10",
            Description = "Giảm 10% cho đơn demo từ 5 triệu, tối đa 2 triệu",
            DiscountType = DiscountType.Percentage,
            Value = 10,
            MinimumOrderAmount = 5_000_000,
            MaximumDiscountAmount = 2_000_000,
            UsageLimit = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedRolesAsync(roleManager);
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
                new Category { Name = "Laptop", Slug = "laptop", Description = "Máy tính xách tay" },
                new Category { Name = "Phụ kiện", Slug = "phu-kien", Description = "Phụ kiện máy tính" },
                new Category { Name = "Chuột", Slug = "chuot", Description = "Chuột văn phòng và gaming" },
                new Category { Name = "Bàn phím", Slug = "ban-phim", Description = "Bàn phím cơ và bàn phím văn phòng" },
                new Category { Name = "Tai nghe", Slug = "tai-nghe", Description = "Tai nghe làm việc và giải trí" },
                new Category { Name = "Balo", Slug = "balo", Description = "Balo laptop chống sốc" },
                new Category { Name = "Màn hình", Slug = "man-hinh", Description = "Màn hình máy tính" });
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

        var categories = await dbContext.Categories.ToDictionaryAsync(category => category.Slug, cancellationToken);
        var brands = await dbContext.Brands.ToDictionaryAsync(brand => brand.Slug, cancellationToken);

        var sampleProducts = new List<Product>
        {
            Laptop("Acer Aspire 3 A315", "acer-aspire-3-a315", "acer", 8990000, "Intel Core i3-1215U", "8GB", "256GB", "Intel UHD", "15.6 inch FHD", "40Wh", "1.7kg", "Windows 11", 18, "Laptop học tập - văn phòng phổ thông với thiết kế thanh lịch, trang bị vi xử lý Intel Core i3 Thế hệ 12 giải quyết mượt mà các tác vụ Word, Excel, lướt web và học trực tuyến.", "sinh viên;văn phòng;học tập", "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80"),
            Laptop("HP 14s Ryzen 3", "hp-14s-ryzen-3", "hp", 9500000, "AMD Ryzen 3 7320U", "8GB", "512GB", "AMD Radeon", "14 inch FHD", "41Wh", "1.46kg", "Windows 11", 16, "Thiết kế mỏng nhẹ hiện đại chỉ 1.46kg, sở hữu chip AMD Ryzen 3 7320U tiết kiệm điện năng cho thời lượng pin ấn tượng. Phù hợp cho học sinh, sinh viên di chuyển nhiều.", "sinh viên;mỏng nhẹ;văn phòng", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&auto=format&fit=crop&q=80"),
            Laptop("Lenovo IdeaPad Slim 3", "lenovo-ideapad-slim-3", "lenovo", 10990000, "Intel Core i5-12450H", "8GB", "512GB", "Intel UHD", "15.6 inch FHD", "47Wh", "1.62kg", "Windows 11", 20, "Cấu hình mạnh mẽ với Intel Core i5-12450H chuỗi H hiệu năng cao, bàn phím gõ êm đặc trưng của Lenovo. Hỗ trợ tốt cho công việc văn phòng và chạy tốt các công cụ lập trình cơ bản.", "sinh viên;văn phòng;lập trình", "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=800&auto=format&fit=crop&q=80"),
            Laptop("Asus Vivobook 15 X1504", "asus-vivobook-15-x1504", "asus", 11990000, "Intel Core i5-1335U", "16GB", "512GB", "Intel Iris Xe", "15.6 inch FHD", "42Wh", "1.7kg", "Windows 11", 12, "Trang bị sẵn 16GB RAM đa nhiệm mượt mà không lo giật lag, bản lề mở 180 độ linh hoạt cùng đồ họa Intel Iris Xe mạnh mẽ cho các ứng dụng chỉnh sửa ảnh nhẹ và học IT.", "học IT;lập trình;văn phòng", "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800&auto=format&fit=crop&q=80"),
            Laptop("Dell Inspiron 15 3520", "dell-inspiron-15-3520", "dell", 12990000, "Intel Core i5-1235U", "16GB", "512GB", "Intel Iris Xe", "15.6 inch FHD 120Hz", "41Wh", "1.83kg", "Windows 11", 13, "Độ bền chuẩn thương hiệu Dell với vỏ gia cố chắc chắn, màn hình 120Hz mượt mà vượt trội trong tầm giá. Đáp ứng tốt nhu cầu học lập trình, văn phòng và làm việc lâu dài.", "học IT;lập trình;văn phòng", "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&auto=format&fit=crop&q=80"),
            Laptop("Acer Swift Go 14", "acer-swift-go-14", "acer", 14990000, "Intel Core i5-13500H", "16GB", "512GB", "Intel Iris Xe", "14 inch 2.8K", "65Wh", "1.32kg", "Windows 11", 10, "Dòng ultrabook cao cấp với màn hình siêu nét 2.8K, trọng lượng cực nhẹ 1.32kg cùng vi xử lý Intel Core i5-13500H 12 nhân 16 luồng xử lý dữ liệu cực nhanh.", "mỏng nhẹ;lập trình;sinh viên", "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&auto=format&fit=crop&q=80"),
            Laptop("HP Pavilion 15", "hp-pavilion-15", "hp", 15990000, "Intel Core i5-1340P", "16GB", "512GB", "Intel Iris Xe", "15.6 inch FHD", "43Wh", "1.75kg", "Windows 11", 14, "Thiết kế vỏ nhôm sang trọng, âm thanh B&O đỉnh cao, bàn phím full-size có phím số tiện lợi cho dân kế toán, văn phòng và sinh viên ngành kinh tế, kỹ thuật.", "học IT;lập trình;văn phòng", "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80"),
            Laptop("Lenovo ThinkBook 14 G6", "lenovo-thinkbook-14-g6", "lenovo", 16990000, "AMD Ryzen 5 7530U", "16GB", "512GB", "AMD Radeon", "14 inch FHD", "60Wh", "1.38kg", "Windows 11 Pro", 11, "Bàn phím hành trình sâu trứ danh mang lại trải nghiệm gõ code vô cùng thoải mái. Trang bị bảo mật vân tay, ThinkShutter che webcam và viên pin 60Wh bền bỉ.", "học IT;lập trình;mỏng nhẹ", "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80"),
            Laptop("Asus TUF Gaming F15", "asus-tuf-gaming-f15", "asus", 18990000, "Intel Core i5-12500H", "16GB", "512GB", "NVIDIA RTX 3050", "15.6 inch FHD 144Hz", "56Wh", "2.2kg", "Windows 11", 9, "Laptop Gaming đạt độ bền chuẩn quân đội MIL-STD-810H, trang bị cạc đồ họa rời NVIDIA GeForce RTX 3050 và màn hình 144Hz. Cân tốt mọi tựa game Esport và đồ họa 2D/3D.", "gaming;đồ họa;lập trình", "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&auto=format&fit=crop&q=80"),
            Laptop("MSI Thin 15 B12UC", "msi-thin-15-b12uc", "msi", 19990000, "Intel Core i7-12650H", "16GB", "512GB", "NVIDIA RTX 3050", "15.6 inch FHD 144Hz", "52Wh", "1.86kg", "Windows 11", 8, "Chiến binh Gaming mỏng nhẹ độc đáo với trọng lượng chỉ 1.86kg, vi xử lý Intel Core i7-12650H 10 nhân cực mạnh. Lựa chọn hàng đầu cho sinh viên công nghệ & game thủ.", "gaming;đồ họa;học IT", "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&auto=format&fit=crop&q=80"),
            Laptop("Dell Vostro 5630", "dell-vostro-5630", "dell", 20990000, "Intel Core i7-1360P", "16GB", "1TB", "Intel Iris Xe", "16 inch FHD+", "54Wh", "1.9kg", "Windows 11 Pro", 7, "Màn hình 16 inch tỷ lệ 16:10 rộng rãi hiển thị nhiều dòng code hơn. Ổ cứng SSD 1TB thoải mái lưu trữ dữ liệu dự án, đồ họa và tài liệu doanh nghiệp.", "văn phòng;lập trình;đồ họa", "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80"),
            Laptop("Asus Zenbook 14 OLED", "asus-zenbook-14-oled", "asus", 22990000, "Intel Core Ultra 5 125H", "16GB", "512GB", "Intel Arc", "14 inch OLED 3K", "75Wh", "1.2kg", "Windows 11", 6, "Tuyệt phẩm Ultrabook với màn hình OLED 3K 120Hz rực rỡ, chuẩn màu 100% DCI-P3. Chip Intel Core Ultra 5 tích hợp NPU xử lý AI mượt mà, thời lượng pin 75Wh vượt trội.", "mỏng nhẹ;đồ họa;lập trình", "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800&auto=format&fit=crop&q=80"),
            Laptop("Lenovo LOQ 15", "lenovo-loq-15", "lenovo", 23990000, "Intel Core i5-13450HX", "16GB", "512GB", "NVIDIA RTX 4050", "15.6 inch FHD 144Hz", "60Wh", "2.4kg", "Windows 11", 6, "Laptop Gaming tầm trung thế hệ mới kế thừa hệ thống tản nhiệt đỉnh cao từ Legion. Cạc đồ họa RTX 4050 6GB GDDR6 chơi mượt game AAA và dựng phim chuyên nghiệp.", "gaming;đồ họa;lập trình", "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&auto=format&fit=crop&q=80"),
            Laptop("Acer Nitro V 15", "acer-nitro-v-15", "acer", 24990000, "Intel Core i7-13620H", "16GB", "512GB", "NVIDIA RTX 4050", "15.6 inch FHD 144Hz", "57Wh", "2.1kg", "Windows 11", 5, "Dòng Nitro thế hệ mới với phong cách thiết kế hiện đại, chip i7-13620H kết hợp RTX 4050 hỗ trợ công nghệ DLSS 3 tái tạo hình ảnh siêu chân thực trong các trò chơi.", "gaming;đồ họa;học IT", "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&auto=format&fit=crop&q=80"),
            Laptop("MSI Cyborg 15", "msi-cyborg-15", "msi", 26990000, "Intel Core i7-13620H", "16GB", "1TB", "NVIDIA RTX 4060", "15.6 inch FHD 144Hz", "53.5Wh", "1.98kg", "Windows 11", 4, "Thiết kế xuyên thấu phong cách Cyberpunk độc lạ, sở hữu GPU NVIDIA GeForce RTX 4060 cân trọn các tác vụ đồ họa nặng, mô phỏng 3D và gaming ở thiết lập cao.", "gaming;đồ họa", "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&auto=format&fit=crop&q=80"),
            Laptop("HP Envy x360 14", "hp-envy-x360-14", "hp", 27990000, "Intel Core Ultra 7 155U", "16GB", "1TB", "Intel Graphics", "14 inch OLED touch", "59Wh", "1.39kg", "Windows 11", 4, "Laptop xoay gập 360 độ cao cấp kèm màn hình cảm ứng OLED rực rỡ. Linh hoạt sử dụng như máy tính bảng, lý tưởng cho công việc sáng tạo nội dung và thuyết trình.", "mỏng nhẹ;sinh viên;đồ họa", "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=800&auto=format&fit=crop&q=80"),
            Laptop("Dell XPS 13 Plus", "dell-xps-13-plus", "dell", 32990000, "Intel Core i7-1360P", "16GB", "1TB", "Intel Iris Xe", "13.4 inch 3.5K OLED", "55Wh", "1.26kg", "Windows 11 Pro", 3, "Bản kiệt tác công nghệ với thanh phím chức năng cảm ứng, touchpad vô hình tinh tế và màn hình 3.5K OLED siêu nét. Đỉnh cao sang trọng cho doanh nhân và chuyên gia.", "mỏng nhẹ;văn phòng;lập trình", "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&auto=format&fit=crop&q=80"),
            Laptop("Lenovo Legion Slim 5", "lenovo-legion-slim-5", "lenovo", 34990000, "AMD Ryzen 7 7840HS", "32GB", "1TB", "NVIDIA RTX 4060", "16 inch 2.5K 165Hz", "80Wh", "2.3kg", "Windows 11", 3, "Cấu hình khủng với 32GB RAM DDR5, chip Ryzen 7 7840HS và màn hình 16 inch 2.5K 165Hz chuẩn màu. Đáp ứng hoàn hảo cho lập trình viên, streamer và nhà thiết kế.", "gaming;đồ họa;lập trình", "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&auto=format&fit=crop&q=80"),
            Laptop("Asus ROG Zephyrus G14", "asus-rog-zephyrus-g14", "asus", 42990000, "AMD Ryzen 9 8945HS", "32GB", "1TB", "NVIDIA RTX 4070", "14 inch OLED 120Hz", "73Wh", "1.5kg", "Windows 11", 2, "Đỉnh cao laptop gaming siêu gọn nhẹ 1.5kg, trang bị chip Ryzen 9 8945HS tích hợp AI cùng card đồ họa RTX 4070. Màn hình ROG Nebula OLED 120Hz siêu sống động.", "gaming;đồ họa;mỏng nhẹ", "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800&auto=format&fit=crop&q=80"),
            Laptop("MSI Creator Z16", "msi-creator-z16", "msi", 45990000, "Intel Core i9-13900H", "32GB", "1TB", "NVIDIA RTX 4070", "16 inch QHD+ 165Hz", "90Wh", "2.35kg", "Windows 11 Pro", 2, "Trạm làm việc di động dành riêng cho nhà sáng tạo nội dung chuyên nghiệp. Vỏ nhôm nguyên khối CNC, màn hình QHD+ 165Hz cảm ứng chuẩn màu True Pixel.", "đồ họa;gaming;lập trình", "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80"),
            Accessory("Logitech M331 Silent Plus", "logitech-m331-silent-plus", "logitech", "chuot", 390000, "Chuột không dây công nghệ SilentTouch giảm 90% tiếng nhấp chuột, kết nối ổn định 10m, thời lượng pin lên tới 24 tháng. Lựa chọn tuyệt vời cho môi trường văn phòng & thư viện.", 50, "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=800&auto=format&fit=crop&q=80"),
            Accessory("Logitech MX Master 3S", "logitech-mx-master-3s", "logitech", "chuot", 2490000, "Chuột ergonomics đỉnh cao cho lập trình viên và designer. Cảm biến 8000 DPI lướt trên mọi bề mặt kể cả kính, con cuộn MagSpeed siêu nhanh 1000 dòng/giây.", 22, "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=800&auto=format&fit=crop&q=80"),
            Accessory("Razer DeathAdder V3", "razer-deathadder-v3", "razer", "chuot", 1590000, "Chuột gaming siêu nhẹ chỉ 59g, thiết kế báng cầm chuẩn Esport, trang bị cảm biến quang học Focus Pro 30K DPI cực kỳ chính xác cho các tựa game bắn súng FPS.", 18, "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=800&auto=format&fit=crop&q=80"),
            Accessory("Logitech K380", "logitech-k380", "logitech", "ban-phim", 790000, "Bàn phím Bluetooth đa thiết bị phím tròn thời trang, kết nối cùng lúc 3 thiết bị (Laptop, iPad, Điện thoại) chuyển đổi nhanh bằng phím Easy-Switch.", 35, "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&auto=format&fit=crop&q=80"),
            Accessory("Razer BlackWidow V4", "razer-blackwidow-v4", "razer", "ban-phim", 3290000, "Bàn phím cơ Gaming cao cấp trang bị Switch cơ học Razer, đèn LED RGB Razer Chroma 16.8 triệu màu từng phím, phím đa phương tiện lăn cơ học và kê tay êm ái.", 12, "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&auto=format&fit=crop&q=80"),
            Accessory("Logitech H390", "logitech-h390", "logitech", "tai-nghe", 690000, "Tai nghe chụp tai cổng USB plug-and-play có tích hợp micrô khử tiếng ồn chủ động. Phù hợp cho công việc họp trực tuyến Zoom, Google Meet và học ngoại ngữ.", 28, "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80"),
            Accessory("Razer Kraken V3", "razer-kraken-v3", "razer", "tai-nghe", 1990000, "Tai nghe Gaming âm thanh vòm THX Spatial Audio định vị chuẩn xác bước chân kẻ địch. Màng loa TriForce Titanium 50mm tái tạo âm trầm mạnh mẽ và dải âm trong trẻo.", 14, "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80"),
            Accessory("Balo Lenovo 15.6", "balo-lenovo-15-6", "lenovo", "balo", 590000, "Balo máy tính vải Polyester kháng nước cao cấp, ngăn chống sốc riêng cho laptop 15.6 inch, thiết kế đệm lưng thoáng khí giúp đeo thoải mái cả ngày.", 40, "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&auto=format&fit=crop&q=80"),
            Accessory("Dell Pro Backpack", "dell-pro-backpack", "dell", "balo", 1190000, "Balo công sở cao cấp chất liệu chống thấm nước EcoLoop thân thiện môi trường, chi tiết phản quang an toàn ban đêm, nhiều ngăn tiện lợi cho laptop 15.6 inch và phụ kiện.", 20, "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&auto=format&fit=crop&q=80"),
            Accessory("Acer EK241Y 24 inch", "acer-ek241y-24-inch", "acer", "man-hinh", 2790000, "Màn hình viền siêu mỏng 24 inch Full HD IPS góc nhìn rộng 178 độ, tần số quét 100Hz mượt mà cùng công nghệ bảo vệ mắt Acer VisionCare chống nhấp nháy và giảm ánh sáng xanh.", 16, "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&auto=format&fit=crop&q=80")
        };

        var existingProducts = await dbContext.Products.ToDictionaryAsync(product => product.Slug, cancellationToken);

        foreach (var sample in sampleProducts)
        {
            if (existingProducts.TryGetValue(sample.Slug, out var existing))
            {
                existing.Description = sample.Description;
                existing.SuitableNeeds = sample.SuitableNeeds;
                existing.MainImageUrl = sample.MainImageUrl;

                var existingImages = await dbContext.ProductImages.Where(img => img.ProductId == existing.Id).ToListAsync(cancellationToken);
                foreach (var img in existingImages)
                {
                    img.ImageUrl = sample.MainImageUrl;
                }
                if (!existingImages.Any())
                {
                    dbContext.ProductImages.Add(new ProductImage
                    {
                        ProductId = existing.Id,
                        ImageUrl = sample.MainImageUrl,
                        AltText = sample.Name,
                        IsPrimary = true,
                        SortOrder = 1
                    });
                }
            }
            else
            {
                dbContext.Products.Add(sample);
                dbContext.ProductImages.Add(new ProductImage
                {
                    Product = sample,
                    ImageUrl = sample.MainImageUrl,
                    AltText = sample.Name,
                    IsPrimary = true,
                    SortOrder = 1
                });
            }
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
                SuitableNeeds = "phụ kiện",
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
        order.SubtotalAmount = order.TotalAmount;
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
        pendingOrder.SubtotalAmount = pendingOrder.TotalAmount;
        pendingOrder.Payment = new Payment
        {
            Method = PaymentMethod.SimulatedBankTransfer,
            Status = PaymentStatus.Simulated,
            Amount = pendingOrder.TotalAmount,
            TransactionCode = "SIM-DEMO-001"
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            ToStatus = OrderStatus.Completed,
            Note = "Đơn dữ liệu mẫu đã hoàn thành.",
            ChangedByUserId = userId,
            ChangedAt = order.CreatedAt
        });
        pendingOrder.StatusHistory.Add(new OrderStatusHistory
        {
            ToStatus = OrderStatus.Pending,
            Note = "Đơn dữ liệu mẫu đang chờ xử lý.",
            ChangedByUserId = userId,
            ChangedAt = pendingOrder.CreatedAt
        });

        dbContext.Orders.AddRange(order, pendingOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
