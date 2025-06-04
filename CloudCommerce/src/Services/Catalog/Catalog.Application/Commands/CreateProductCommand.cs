using MediatR;
using FluentValidation;
using AutoMapper;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using CloudCommerce.BuildingBlocks.EventBus;
using CloudCommerce.BuildingBlocks.Common;
using System;
using System.Collections.Generic;

namespace Catalog.Application.Commands
{
    public class CreateProductCommand : IRequest<ProductDto>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public string SKU { get; set; }
        public int InitialStock { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<ProductImageDto> Images { get; set; } = new();
    }

    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200);

            RuleFor(x => x.Brand)
                .NotEmpty().MaximumLength(100);

            RuleFor(x => x.Price)
                .GreaterThan(0);

            RuleFor(x => x.SKU)
                .NotEmpty()
                .Matches(@"^[A-Z0-9\-]+$");

            RuleFor(x => x.InitialStock)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Categories)
                .NotEmpty();
        }
    }

    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateProductCommandHandler> _logger;
        private readonly IEventBus _eventBus;

        public CreateProductCommandHandler(
            IProductRepository productRepository,
            IMapper mapper,
            ILogger<CreateProductCommandHandler> logger,
            IEventBus eventBus)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _logger = logger;
            _eventBus = eventBus;
        }

        public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new product: {ProductName}", request.Name);

            var existingProduct = await _productRepository.GetBySkuAsync(request.SKU);
            if (existingProduct != null)
            {
                throw new DomainException($"Product with SKU {request.SKU} already exists");
            }

            var product = new Product(
                request.Name,
                request.Description,
                request.Brand,
                request.Price,
                request.SKU);

            product.UpdateStock(request.InitialStock);

            foreach (var categoryId in request.Categories)
            {
                product.AddCategory(Guid.Parse(categoryId));
            }

            foreach (var image in request.Images ?? new List<ProductImageDto>())
            {
                product.AddImage(image.Url, image.Alt, image.IsMain);
            }

            await _productRepository.AddAsync(product);
            await _productRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            var productCreatedEvent = new ProductCreatedIntegrationEvent(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity);

            await _eventBus.PublishAsync(productCreatedEvent, cancellationToken);

            _logger.LogInformation("Product created successfully: {ProductId}", product.Id);

            return _mapper.Map<ProductDto>(product);
        }
    }
}
