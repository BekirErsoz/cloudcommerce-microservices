
# CloudCommerce – Minimal Scaffold

 
- .NET 8 ve C# 12 hedeflenmiştir.
- `docker-compose up -d` komutu PostgreSQL + RabbitMQ + Catalog.API konteynerini ayağa kaldırır.

## Hızlı Başlangıç

```bash
dotnet restore CloudCommerce.sln
dotnet build CloudCommerce.sln
dotnet run --project src/Services/Catalog/Catalog.API/Catalog.API.csproj
```


