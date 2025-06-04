
# CloudCommerce – Minimal Scaffold

Bu ZIP, verilen proje yapısına uygun **örnek bir iskelet** içerir. 
- .NET 8 ve C# 12 hedeflenmiştir.
- Catalog mikroservisi derlenip çalışacak kadar eksiksizdir; diğer servisler için aynı şablonu çoğaltabilirsiniz.
- `docker-compose up -d` komutu PostgreSQL + RabbitMQ + Catalog.API konteynerini ayağa kaldırır.

## Hızlı Başlangıç

```bash
dotnet restore CloudCommerce.sln
dotnet build CloudCommerce.sln
dotnet run --project src/Services/Catalog/Catalog.API/Catalog.API.csproj
```

Ardından `https://localhost:5001/api/v1/products` uç noktasına POST isteği gönderebilirsiniz.

> Not: Tam üretim mimarisi için kalan servis dosyalarını doldurmanız gerekir.
