using Api.Models;
using Application.Services;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ILogger<ProductsController> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<Product>>>> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? isActive = true)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (items, totalCount) = await _productRepository.GetProductsWithFiltersAsync(
                    pageNumber, pageSize, search, categoryId, minPrice, maxPrice, isActive);

                var pagedResponse = new PagedResponse<Product>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(new ApiResponse<PagedResponse<Product>>
                {
                    Success = true,
                    Message = "Productos obtenidos exitosamente",
                    Data = pagedResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos");
                return StatusCode(500, new ApiResponse<PagedResponse<Product>>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Product>>> GetProduct(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdWithCategoryAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<Product>
                    {
                        Success = false,
                        Message = "Producto no encontrado"
                    });
                }

                return Ok(new ApiResponse<Product>
                {
                    Success = true,
                    Message = "Producto obtenido exitosamente",
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {Id}", id);
                return StatusCode(500, new ApiResponse<Product>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Product>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                // Verificar que la categoría existe
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
                if (category == null)
                {
                    return BadRequest(new ApiResponse<Product>
                    {
                        Success = false,
                        Message = "La categoría especificada no existe"
                    });
                }

                // Verificar SKU único si se proporciona
                if (!string.IsNullOrWhiteSpace(request.SKU))
                {
                    var skuExists = await _productRepository.ExistsBySKUAsync(request.SKU);
                    if (skuExists)
                    {
                        return BadRequest(new ApiResponse<Product>
                        {
                            Success = false,
                            Message = "Ya existe un producto con ese SKU"
                        });
                    }
                }

                var product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    SKU = request.SKU,
                    CategoryId = request.CategoryId,
                    IsActive = request.IsActive
                };

                await _productRepository.AddAsync(product);
                await _productRepository.SaveChangesAsync();

                // Recargar con categoría para la respuesta
                var createdProduct = await _productRepository.GetByIdWithCategoryAsync(product.Id);

                return CreatedAtAction(
                    nameof(GetProduct),
                    new { id = product.Id },
                    new ApiResponse<Product>
                    {
                        Success = true,
                        Message = "Producto creado exitosamente",
                        Data = createdProduct
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");
                return StatusCode(500, new ApiResponse<Product>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<ApiResponse<BulkCreateResponse>>> CreateBulkProducts([FromBody] BulkCreateProductsRequest request)
        {
            try
            {
                if (request.Count <= 0 || request.Count > 100000)
                {
                    return BadRequest(new ApiResponse<BulkCreateResponse>
                    {
                        Success = false,
                        Message = "La cantidad debe estar entre 1 y 100,000"
                    });
                }

                // Verificar que la categoría existe
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
                if (category == null)
                {
                    return BadRequest(new ApiResponse<BulkCreateResponse>
                    {
                        Success = false,
                        Message = "La categoría especificada no existe"
                    });
                }

                var products = new List<Product>();
                var random = new Random();

                for (int i = 0; i < request.Count; i++)
                {
                    var product = new Product
                    {
                        Name = $"{request.BaseProductName ?? "Producto"} {i + 1:D6}",
                        Description = $"Descripción del producto generado automáticamente #{i + 1}",
                        Price = Math.Round((decimal)(random.NextDouble() * (request.MaxPrice - request.MinPrice) + request.MinPrice), 2),
                        Stock = random.Next(0, 1000),
                        SKU = $"SKU-{category.Name}-{DateTime.UtcNow.Ticks}-{i:D6}",
                        CategoryId = request.CategoryId,
                        IsActive = true
                    };
                    products.Add(product);
                }

                await _productRepository.BulkInsertAsync(products);
                await _productRepository.SaveChangesAsync();

                _logger.LogInformation("Creados {Count} productos masivamente para categoría {CategoryId}",
                    request.Count, request.CategoryId);

                return Ok(new ApiResponse<BulkCreateResponse>
                {
                    Success = true,
                    Message = $"Se crearon {request.Count} productos exitosamente",
                    Data = new BulkCreateResponse
                    {
                        CreatedCount = request.Count,
                        CategoryName = category.Name
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear productos masivamente");
                return StatusCode(500, new ApiResponse<BulkCreateResponse>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<Product>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<Product>
                    {
                        Success = false,
                        Message = "Producto no encontrado"
                    });
                }

                // Verificar que la categoría existe si se cambió
                if (request.CategoryId != product.CategoryId)
                {
                    var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
                    if (category == null)
                    {
                        return BadRequest(new ApiResponse<Product>
                        {
                            Success = false,
                            Message = "La categoría especificada no existe"
                        });
                    }
                }

                // Actualizar propiedades
                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Stock = request.Stock;
                product.CategoryId = request.CategoryId;
                product.IsActive = request.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _productRepository.UpdateAsync(product);
                await _productRepository.SaveChangesAsync();

                var updatedProduct = await _productRepository.GetByIdWithCategoryAsync(product.Id);

                return Ok(new ApiResponse<Product>
                {
                    Success = true,
                    Message = "Producto actualizado exitosamente",
                    Data = updatedProduct
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto {Id}", id);
                return StatusCode(500, new ApiResponse<Product>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Producto no encontrado"
                    });
                }

                await _productRepository.DeleteAsync(product);
                await _productRepository.SaveChangesAsync();

                _logger.LogInformation("Producto {Id} eliminado exitosamente", id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Producto eliminado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }
    }
}
