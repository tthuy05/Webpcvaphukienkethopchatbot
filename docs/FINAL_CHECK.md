# Kết quả kiểm tra cuối

Ngày kiểm tra: 2026-06-24

## Lệnh đã chạy

```bash
dotnet restore
dotnet build
dotnet ef migrations list
dotnet ef database update
dotnet run --no-build --urls http://127.0.0.1:5020
```

## Kết quả

- `dotnet restore`: thành công.
- `dotnet build`: thành công, 0 warning, 0 error.
- `dotnet ef migrations list`: thành công, có migration `20260624123118_InitialCreate`.
- `dotnet ef database update`: thành công, database đã up-to-date.
- `dotnet run`: ứng dụng chạy được trên localhost.

## HTTP checks

- Product search/filter page: pass.
- Theme dark bằng cookie `preferred-theme=dark`: pass.
- Localization English bằng cookie `.AspNetCore.Culture`: pass.
- Chatbot POST câu hỏi `Em co 15tr hoc IT nen mua laptop nao`: pass, trả gợi ý sản phẩm từ database.
- Admin login `admin@example.com`: pass, truy cập `/Admin/Dashboard` status 200.
- User thường `user@example.com` truy cập `/Admin/Dashboard`: pass, bị redirect/deny.
- Add to cart bằng user thường: pass.
- Checkout bằng user thường: pass, tạo đơn hàng thành công.

## Ghi chú lỗi đã gặp

- LocalDB `(localdb)\\mssqllocaldb` không tạo được instance trên máy hiện tại.
- Đã chuyển development connection string sang SQL Server Express `Server=.\\SQLEXPRESS`, migration và database update chạy thành công.
