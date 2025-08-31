using Application.DTOs;
using Application.Mappings;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductFiltersDto filters)
        {
            var (items, totalCount) = await _productRepository.GetProductsWithFiltersAsync(
                filters.PageNumber,
                filters.PageSize,
                filters.SearchTerm,
                filters.CategoryId,
                filters.MinPrice,
                filters.MaxPrice,
                filters.IsActive);

            var productDtos = items.Select(p => p.ToDto()).ToList();

            return new PagedResultDto<ProductDto>
            {
                Items = productDtos,
                TotalCount = totalCount,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize
            };
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdWithCategoryAsync(id);
            return product?.ToDto();
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto productDto)
        {
            // Validaciones de negocio
            var category = await _categoryRepository.GetByIdAsync(productDto.CategoryId);
            if (category == null)
                throw new ArgumentException("La categoría especificada no existe");

            if (!string.IsNullOrWhiteSpace(productDto.SKU))
            {
                var skuExists = await _productRepository.ExistsBySKUAsync(productDto.SKU);
                if (skuExists)
                    throw new ArgumentException("Ya existe un producto con ese SKU");
            }

            var product = productDto.ToEntity();

            // Generar SKU automáticamente si no se proporciona
            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                product.SKU = await GenerateUniqueSKUAsync(category.Name);
            }

            await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();

            var createdProduct = await _productRepository.GetByIdWithCategoryAsync(product.Id);
            return createdProduct!.ToDto();
        }

        public async Task<ProductDto> UpdateProductAsync(UpdateProductDto productDto)
        {
            var product = await _productRepository.GetByIdAsync(productDto.Id);
            if (product == null)
                throw new ArgumentException("Producto no encontrado");

            // Validar categoría si cambió
            if (productDto.CategoryId != product.CategoryId)
            {
                var category = await _categoryRepository.GetByIdAsync(productDto.CategoryId);
                if (category == null)
                    throw new ArgumentException("La categoría especificada no existe");
            }

            productDto.UpdateEntity(product);
            await _productRepository.UpdateAsync(product);
            await _productRepository.SaveChangesAsync();

            var updatedProduct = await _productRepository.GetByIdWithCategoryAsync(product.Id);
            return updatedProduct!.ToDto();
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return false;

            await _productRepository.DeleteAsync(product);
            await _productRepository.SaveChangesAsync();
            return true;
        }

        public async Task<BulkCreateResultDto> CreateBulkProductsAsync(BulkCreateRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validar categoría
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
                if (category == null)
                    throw new ArgumentException("La categoría especificada no existe");

                _logger.LogInformation("Iniciando creación masiva de {Count} productos para categoría {CategoryName}",
                    request.Count, category.Name);

                var totalCreated = 0;
                var batchSize = request.BatchSize;
                var random = new Random();

                // Procesar en lotes para mejor performance
                for (int batch = 0; batch < Math.Ceiling((double)request.Count / batchSize); batch++)
                {
                    var remainingProducts = request.Count - totalCreated;
                    var currentBatchSize = Math.Min(batchSize, remainingProducts);

                    var products = new List<Product>();

                    for (int i = 0; i < currentBatchSize; i++)
                    {
                        var productNumber = totalCreated + i + 1;
                        var product = new Product
                        {
                            Name = $"{request.BaseProductName} {productNumber:D6}",
                            Description = $"Producto generado automáticamente #{productNumber} para {category.Name}",
                            Price = Math.Round((decimal)(random.NextDouble() * (double)(request.MaxPrice - request.MinPrice) + (double)request.MinPrice), 2),
                            Stock = random.Next(0, 1000),
                            SKU = $"SKU-{category.Name.Replace(" ", "")}-{DateTime.UtcNow.Ticks}-{productNumber:D6}",
                            CategoryId = request.CategoryId,
                            IsActive = true
                        };
                        products.Add(product);
                    }

                    // Insertar lote
                    await _productRepository.BulkInsertAsync(products);
                    await _productRepository.SaveChangesAsync();

                    totalCreated += currentBatchSize;

                    // Log de progreso cada 10 lotes
                    if (batch % 10 == 0 || totalCreated == request.Count)
                    {
                        _logger.LogInformation("Progreso: {Created}/{Total} productos creados",
                            totalCreated, request.Count);
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation("Creación masiva completada: {Count} productos en {ElapsedMs}ms",
                    totalCreated, stopwatch.ElapsedMilliseconds);

                return new BulkCreateResultDto
                {
                    CreatedCount = totalCreated,
                    CategoryName = category.Name,
                    ProcessingTime = stopwatch.Elapsed,
                    Message = $"Se crearon {totalCreated} productos exitosamente en {stopwatch.ElapsedMilliseconds}ms"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error durante la creación masiva de productos");
                throw;
            }
        }
        public async Task<bool> ExistsBySKUAsync(string sku)
        {
            return await _productRepository.ExistsBySKUAsync(sku);
        }

        private async Task<string> GenerateUniqueSKUAsync(string categoryName)
        {
            string sku;
            do
            {
                sku = $"SKU-{categoryName.Replace(" ", "")}-{DateTime.UtcNow.Ticks}";
            }
            while (await _productRepository.ExistsBySKUAsync(sku));

            return sku;
        }
    }
}
