# Website bán laptop và phụ kiện tích hợp chatbot

Backend ASP.NET Core MVC cho đồ án tốt nghiệp: bán laptop, phụ kiện máy tính và chatbot rule-based hỗ trợ tư vấn sản phẩm theo ngân sách, nhu cầu.

## Công nghệ

- ASP.NET Core MVC `net8.0`
- Entity Framework Core
- SQL Server / SQL Server Express / Azure SQL
- ASP.NET Core Identity
- Razor View + Bootstrap 5
- Localization `vi-VN`, `en-US`
- Theme sáng/tối bằng cookie và UserPreferences

## Cấu trúc thư mục

- `Controllers`: controller phía user.
- `Areas/Admin`: controller và view quản trị.
- `Data`: `ApplicationDbContext`, migration, seeder.
- `Models`: entity và enum.
- `Services`: nghiệp vụ sản phẩm, giỏ hàng, đơn hàng, dashboard, chatbot.
- `ViewModels`: model cho form, filter, view.
- `Resources`: resource đa ngôn ngữ.
- `Views`: Razor View test chức năng.
- `wwwroot`: CSS, JS, ảnh placeholder.

## Cấu hình connection string

Development đang dùng SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=WebPcVaPhuKienDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Không commit connection string thật, API key hoặc password thật. Khi deploy Azure App Service, đặt biến môi trường:

```text
ConnectionStrings__DefaultConnection=<Azure SQL connection string>
SeedData__Enabled=false
```

## Chạy local

```bash
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

Nếu cần tạo lại migration đầu tiên trong môi trường mới:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Ứng dụng tự chạy migration và seed data khi `SeedData:Enabled=true`.

## Seed data

Seeder tạo:

- 7 danh mục: Laptop, Phụ kiện, Chuột, Bàn phím, Tai nghe, Balo, Màn hình.
- 8 thương hiệu: Asus, Acer, Lenovo, Dell, HP, MSI, Logitech, Razer.
- 20 laptop nhiều phân khúc giá.
- 10 phụ kiện.
- 2 đơn hàng mẫu.
- Ảnh placeholder local trong `wwwroot/images/products`.

## Tài khoản mẫu

- Admin: `admin@example.com` / `Admin@123`
- User: `user@example.com` / `User@123`

## Test chức năng chính

1. Đăng nhập admin và vào `/Admin/Dashboard`.
2. Quản lý danh mục, thương hiệu, sản phẩm trong `/Admin`.
3. Tìm kiếm/lọc/sắp xếp sản phẩm tại `/Products`.
4. Đăng nhập user, thêm sản phẩm vào giỏ hàng.
5. Checkout tại `/Orders/Checkout`.
6. Xem lịch sử đơn hàng tại `/Orders`.
7. Đổi ngôn ngữ bằng language switcher trên navbar.
8. Đổi theme sáng/tối bằng nút theme trên navbar.
9. Test chatbot tại `/Chatbot` với câu hỏi như: `Em có 15tr học IT nên mua laptop nào?`

## Localization

Resource files:

- `Resources/SharedResource.vi-VN.resx`
- `Resources/SharedResource.en-US.resx`

Mặc định là `vi-VN`. Người dùng chưa đăng nhập lưu lựa chọn bằng cookie. Người dùng đã đăng nhập lưu vào bảng `UserPreferences`.

## Theme sáng/tối

Layout gắn class:

- `body class="theme-light"`
- `body class="theme-dark"`

CSS variables chính:

- `--bg-color`
- `--text-color`
- `--card-bg`
- `--border-color`
- `--primary-color`

## Chatbot rule-based

Chatbot chưa dùng AI API. Hệ thống nhận diện ngân sách như `10tr`, `15 triệu`, `dưới 20 triệu`, nhận diện nhu cầu như học IT, lập trình, văn phòng, gaming, đồ họa, sinh viên, mỏng nhẹ; sau đó query bảng `Products`, gợi ý tối đa 3 laptop và lưu log vào `ChatbotLogs`.

## Deploy Azure

1. Tạo Azure SQL Database.
2. Tạo Azure App Service chạy .NET 8.
3. Cấu hình App Settings:
   - `ConnectionStrings__DefaultConnection`
   - `SeedData__Enabled=false`
4. Publish từ Visual Studio 2022 hoặc `dotnet publish`.
5. Chạy migration bằng pipeline/console:

```bash
dotnet ef database update
```

## Lệnh thường dùng

```bash
dotnet restore
dotnet build
dotnet ef migrations list
dotnet ef database update
dotnet run
```
