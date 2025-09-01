using Api.Models;
using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("GlobalPolicy")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductController> _logger;

        // Contador de versión global para invalidar la caché de listas paginadas.
        private static int _productsCacheVersion = 0;
        private static readonly object _cacheLock = new();

        public ProductController(
            IProductService productService,
            ICategoryService categoryService,
            IMemoryCache cache,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<ProductDto>>>> GetProducts(
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
                // La versión global en la clave de caché asegura que una actualización invalide todas las listas.
                var cacheKey = $"products_v{_productsCacheVersion}_{pageNumber}_{pageSize}_{search}_{categoryId}_{minPrice}_{maxPrice}_{isActive}";

                if (_cache.TryGetValue(cacheKey, out PagedResponse<ProductDto>? cachedResponse))
                {
                    return Ok(new ApiResponse<PagedResponse<ProductDto>>
                    {
                        Success = true,
                        Message = "Productos obtenidos desde caché",
                        Data = cachedResponse
                    });
                }

                var filters = new ProductFiltersDto
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = search,
                    CategoryId = categoryId,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    IsActive = isActive
                };

                var result = await _productService.GetProductsAsync(filters);

                var response = new PagedResponse<ProductDto>
                {
                    Items = result.Items,
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
                    Size = result.TotalCount
                };

                _cache.Set(cacheKey, response, cacheOptions);

                return Ok(new ApiResponse<PagedResponse<ProductDto>>
                {
                    Success = true,
                    Message = "Productos obtenidos exitosamente",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos");
                return StatusCode(500, new ApiResponse<PagedResponse<ProductDto>>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            try
            {
                // Clave de caché simple basada solo en el ID.
                var cacheKey = $"product_{id}";

                if (_cache.TryGetValue(cacheKey, out ProductDto? cachedProduct))
                {
                    return Ok(new ApiResponse<ProductDto>
                    {
                        Success = true,
                        Message = "Producto obtenido desde caché",
                        Data = cachedProduct
                    });
                }

                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Producto no encontrado"
                    });
                }

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Size = 1
                };

                // Cachear producto por 5 minutos
                _cache.Set(cacheKey, product, cacheOptions);

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Message = "Producto obtenido exitosamente",
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {Id}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPost]
        [EnableRateLimiting("CreateProductPolicy")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var category = await GetCategoryFromCacheAsync(request.CategoryId);
                if (category == null)
                {
                    return BadRequest(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "La categoría especificada no existe"
                    });
                }

                var productDto = new CreateProductDto
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    SKU = request.SKU,
                    CategoryId = request.CategoryId,
                    IsActive = request.IsActive
                };

                var result = await _productService.CreateProductAsync(productDto);

                // Invalidar la caché de listas, ya que se ha añadido un nuevo producto.
                InvalidateProductCaches();

                _logger.LogDebug("Producto {ProductId} creado por usuario {UserEmail}",
                    result.Id, User.FindFirst(ClaimTypes.Email)?.Value);

                return CreatedAtAction(
                    nameof(GetProduct),
                    new { id = result.Id },
                    new ApiResponse<ProductDto>
                    {
                        Success = true,
                        Message = "Producto creado exitosamente",
                        Data = result
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto: {Message}", ex.Message);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var productDto = new UpdateProductDto
                {
                    Id = id,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    CategoryId = request.CategoryId,
                    IsActive = request.IsActive
                };

                var result = await _productService.UpdateProductAsync(productDto);

                // Invalidar la caché del producto específico y las listas.
                _cache.Remove($"product_{id}");
                InvalidateProductCaches();

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Message = "Producto actualizado exitosamente",
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "El producto fue modificado por otro usuario. Vuelve a cargar los datos antes de intentar de nuevo."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto {Id}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
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
                var deleted = await _productService.DeleteProductAsync(id);
                if (!deleted)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Producto no encontrado"
                    });
                }

                // Invalidar la caché del producto específico y las listas.
                _cache.Remove($"product_{id}");
                InvalidateProductCaches();

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

        private async Task<CategoryDto?> GetCategoryFromCacheAsync(int categoryId)
        {
            var cacheKey = $"category_{categoryId}";

            if (!_cache.TryGetValue(cacheKey, out CategoryDto? category))
            {
                category = await _categoryService.GetCategoryByIdAsync(categoryId);
                if (category != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                        Size = 1
                    };
                    _cache.Set(cacheKey, category, cacheOptions);
                }
            }
            return category;
        }

        private void InvalidateProductCaches()
        {
            lock (_cacheLock)
            {
                _productsCacheVersion++;
                _logger.LogDebug("♻️ Cache de productos invalidada, nueva versión: {Version}", _productsCacheVersion);
            }
        }
    }
}
