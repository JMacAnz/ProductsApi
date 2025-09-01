using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<ILogger<ProductService>> _mockLogger;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockLogger = new Mock<ILogger<ProductService>>();

            _productService = new ProductService(
                _mockProductRepository.Object,
                _mockCategoryRepository.Object,
                _mockLogger.Object);
        }

        #region GetProductByIdAsync Tests
        [Fact]
        public async Task GetProductByIdAsync_DebeRetornarProducto_CuandoExiste()
        {
            var productId = 1;
            var category = new Category { Id = 1, Name = "SERVIDORES" };
            var product = new Product
            {
                Id = productId,
                Name = "Servidor Test",
                Price = 1000m,
                Stock = 10,
                CategoryId = 1,
                Category = category
            };

            _mockProductRepository
                .Setup(r => r.GetByIdWithCategoryAsync(productId))
                .ReturnsAsync(product);

            var result = await _productService.GetProductByIdAsync(productId);

            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal("Servidor Test", result.Name);
            Assert.Equal("SERVIDORES", result.CategoryName);
            Assert.Equal(1000m, result.Price);

            _mockProductRepository.Verify(r => r.GetByIdWithCategoryAsync(productId), Times.Once);
        }

        [Fact]
        public async Task GetProductByIdAsync_DebeRetornarNull_CuandoNoExiste()
        {
            var productId = 999;
            _mockProductRepository
                .Setup(r => r.GetByIdWithCategoryAsync(productId))
                .ReturnsAsync((Product?)null);

            var result = await _productService.GetProductByIdAsync(productId);

            Assert.Null(result);
        }
        #endregion

        #region CreateProductAsync Tests
        [Fact]
        public async Task CreateProductAsync_DebeCrearProducto_CuandoDatosValidos()
        {
            var category = new Category { Id = 1, Name = "SERVIDORES" };
            var createDto = new CreateProductDto
            {
                Name = "Nuevo Servidor",
                Description = "Servidor de prueba",
                Price = 5000m,
                Stock = 5,
                CategoryId = 1,
                SKU = "TEST-001"
            };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
            _mockProductRepository.Setup(r => r.ExistsBySKUAsync("TEST-001")).ReturnsAsync(false);
            _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>())).ReturnsAsync((Product p) => p);

            var createdProduct = new Product
            {
                Id = 1,
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                Stock = createDto.Stock,
                CategoryId = createDto.CategoryId,
                SKU = createDto.SKU,
                Category = category
            };
            _mockProductRepository.Setup(r => r.GetByIdWithCategoryAsync(It.IsAny<int>())).ReturnsAsync(createdProduct);

            var result = await _productService.CreateProductAsync(createDto);

            Assert.NotNull(result);
            Assert.Equal("Nuevo Servidor", result.Name);
            Assert.Equal(5000m, result.Price);
            Assert.Equal("SERVIDORES", result.CategoryName);

            _mockCategoryRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mockProductRepository.Verify(r => r.ExistsBySKUAsync("TEST-001"), Times.Once);
            _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
            _mockProductRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateProductAsync_DebeLanzarExcepcion_CuandoCategoriaNoExiste()
        {
            var createDto = new CreateProductDto { Name = "Producto Test", CategoryId = 999 };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Category?)null);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _productService.CreateProductAsync(createDto));

            Assert.Equal("La categoría especificada no existe", exception.Message);
        }

        [Fact]
        public async Task CreateProductAsync_DebeLanzarExcepcion_CuandoSKUDuplicado()
        {
            var category = new Category { Id = 1, Name = "SERVIDORES" };
            var createDto = new CreateProductDto { Name = "Producto Test", CategoryId = 1, SKU = "SKU-DUPLICADO" };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
            _mockProductRepository.Setup(r => r.ExistsBySKUAsync("SKU-DUPLICADO")).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _productService.CreateProductAsync(createDto));

            Assert.Equal("Ya existe un producto con ese SKU", exception.Message);
        }
        #endregion

        #region GetProductsAsync Paginación
        [Theory]
        [InlineData(1, 10, 100)]
        [InlineData(2, 5, 100)]
        [InlineData(1, 20, 50)]
        public async Task GetProductsAsync_DebeRetornarPaginacionCorrecta(int pageNumber, int pageSize, int totalCount)
        {
            // Generar todos los productos simulados según totalCount
            var allProducts = GenerateTestProducts(totalCount);

            // Simular la página que corresponde
            var pagedProducts = allProducts.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var filters = new ProductFiltersDto { PageNumber = pageNumber, PageSize = pageSize };

            _mockProductRepository
                .Setup(r => r.GetProductsWithFiltersAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool?>()
                ))
                .ReturnsAsync((pagedProducts, totalCount));

            var result = await _productService.GetProductsAsync(filters);

            Assert.NotNull(result);
            Assert.Equal(pagedProducts.Count, result.Items.Count());
            Assert.Equal(totalCount, result.TotalCount);
            Assert.Equal(pageNumber, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);

            var firstProduct = result.Items.First();
            var firstSourceProduct = pagedProducts.First();
            Assert.Equal(firstSourceProduct.Name, firstProduct.Name);
            Assert.Equal(firstSourceProduct.Price, firstProduct.Price);

            _mockProductRepository.Verify(r => r.GetProductsWithFiltersAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<bool?>()), Times.Once);
        }
        #endregion

        #region Helpers
        private List<Product> GenerateTestProducts(int totalCount)
        {
            var category = new Category { Id = 1, Name = "SERVIDORES" };
            var products = new List<Product>();

            for (int i = 1; i <= totalCount; i++)
            {
                products.Add(new Product
                {
                    Id = i,
                    Name = $"Producto Test {i}",
                    Price = 100m * i,
                    Stock = 10,
                    CategoryId = 1,
                    Category = category,
                    SKU = $"TEST-{i:D3}"
                });
            }

            return products;
        }
        #endregion
    }
}
