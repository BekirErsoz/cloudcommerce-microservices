using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using CloudCommerce.BuildingBlocks.Common;
using System.Linq.Expressions;

namespace Catalog.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly CatalogContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(
            CatalogContext context, 
            ICacheService cacheService,
            ILogger<ProductRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IUnitOfWork UnitOfWork => _context;

        public async Task<Product> GetByIdAsync(Guid id)
        {
            var cacheKey = $"product:{id}";

            var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogDebug("Product {ProductId} retrieved from cache", id);
                return cachedProduct;
            }

            var product = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Include(p => p.Categories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5));
            }

            return product;
        }

        public async Task<Product> GetBySkuAsync(string sku)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<PaginatedResult<Product>> GetPaginatedAsync(
            int pageIndex, 
            int pageSize,
            Expression<Func<Product, bool>> filter = null,
            Func<IQueryable<Product>, IOrderedQueryable<Product>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<Product> query = _context.Products;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            var totalItems = await query.CountAsync();

            query = orderBy != null ? orderBy(query) : query.OrderBy(p => p.Name);

            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedResult<Product>(pageIndex, pageSize, totalItems, items);
        }

        public async Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate)
        {
            return await _context.Products.Where(predicate).ToListAsync();
        }

        public async Task<Product> AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _cacheService.RemoveByPatternAsync("products:*");
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _cacheService.RemoveAsync($"product:{product.Id}");
            await _cacheService.RemoveByPatternAsync("products:*");
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _cacheService.RemoveAsync($"product:{product.Id}");
            await _cacheService.RemoveByPatternAsync("products:*");
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
        {
            return await _context.Products
                .Include(p => p.Categories)
                .Where(p => p.Categories.Any(c => c.CategoryId == categoryId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByBrandAsync(string brand)
        {
            return await _context.Products
                .Where(p => p.Brand == brand && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            return await _context.Products
                .Where(p => p.IsActive && 
                    (p.Name.ToLower().Contains(searchTerm) ||
                     p.Description.ToLower().Contains(searchTerm) ||
                     p.Brand.ToLower().Contains(searchTerm) ||
                     p.SKU.ToLower().Contains(searchTerm)))
                .OrderBy(p => p.Name)
                .Take(50)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetStockLevelsAsync(IEnumerable<Guid> productIds)
        {
            return await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id.ToString(), p => p.StockQuantity);
        }
    }
}
