using CloudCommerce.BuildingBlocks.Common;
using System;
using System.Collections.Generic;

namespace Catalog.Domain.Entities
{
    public class Product : Entity, IAggregateRoot
    {
        private readonly List<ProductVariant> _variants;
        private readonly List<ProductImage> _images;
        private readonly List<ProductCategory> _categories;

        protected Product() 
        { 
            _variants = new List<ProductVariant>();
            _images = new List<ProductImage>();
            _categories = new List<ProductCategory>();
        }

        public Product(
            string name, 
            string description, 
            string brand, 
            decimal price,
            string sku) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Brand = brand ?? throw new ArgumentNullException(nameof(brand));
            Price = price;
            SKU = sku ?? throw new ArgumentNullException(nameof(sku));
            CreatedDate = DateTime.UtcNow;
            IsActive = true;
        }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Brand { get; private set; }
        public decimal Price { get; private set; }
        public string SKU { get; private set; }
        public int StockQuantity { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? UpdatedDate { get; private set; }

        public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
        public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
        public IReadOnlyCollection<ProductCategory> Categories => _categories.AsReadOnly();

        // Domain Methods
        public void UpdateStock(int quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative");

            StockQuantity = quantity;
            UpdatedDate = DateTime.UtcNow;

            AddDomainEvent(new ProductStockUpdatedEvent(Id, StockQuantity));
        }

        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice <= 0)
                throw new ArgumentException("Price must be greater than zero");

            var oldPrice = Price;
            Price = newPrice;
            UpdatedDate = DateTime.UtcNow;

            AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
        }

        public void AddVariant(string name, string value, decimal priceAdjustment = 0)
        {
            var variant = new ProductVariant(Id, name, value, priceAdjustment);
            _variants.Add(variant);
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedDate = DateTime.UtcNow;
            AddDomainEvent(new ProductDeactivatedEvent(Id));
        }
    }

    // Value Objects
    public class ProductVariant : ValueObject
    {
        public ProductVariant(Guid productId, string name, string value, decimal priceAdjustment)
        {
            ProductId = productId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            PriceAdjustment = priceAdjustment;
        }

        public Guid ProductId { get; private set; }
        public string Name { get; private set; }
        public string Value { get; private set; }
        public decimal PriceAdjustment { get; private set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }
}
