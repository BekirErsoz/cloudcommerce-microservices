using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using System.Net;
using CloudCommerce.BuildingBlocks.Common;
using Catalog.Application.Commands;
using Catalog.Application.Queries;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Catalog.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get products with pagination and filtering
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedResult<ProductDto>), (int)HttpStatusCode.OK)]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts(
            [FromQuery] GetProductsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get product by id
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            var query = new GetProductByIdQuery { Id = id };
            var product = await _mediator.Send(query);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        /// <summary>
        /// Create new product
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ProductManager")]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ProductDto>> CreateProduct(
            [FromBody] CreateProductCommand command)
        {
            _logger.LogInformation("Creating product: {@Command}", command);

            var product = await _mediator.Send(command);

            return CreatedAtAction(
                nameof(GetProduct), 
                new { id = product.Id }, 
                product);
        }

        /// <summary>
        /// Update product
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,ProductManager")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateProduct(
            Guid id, 
            [FromBody] UpdateProductCommand command)
        {
            if (id != command.Id)
                return BadRequest("Product ID mismatch");

            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Update product stock
        /// </summary>
        [HttpPatch("{id:guid}/stock")]
        [Authorize(Roles = "Admin,ProductManager,WarehouseManager")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateStock(
            Guid id, 
            [FromBody] UpdateStockCommand command)
        {
            command.ProductId = id;
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Delete product (soft delete)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var command = new DeleteProductCommand { Id = id };
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Bulk import products
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "Admin,ProductManager")]
        [ProducesResponseType(typeof(BulkImportResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<BulkImportResult>> BulkImport(
            [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var command = new BulkImportProductsCommand
            {
                File = file,
                FileName = file.FileName
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
