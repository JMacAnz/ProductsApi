using Application.Services;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Tests.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<ILogger<CategoryService>> _mockLogger;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockLogger = new Mock<ILogger<CategoryService>>();
            _categoryService = new CategoryService(_mockCategoryRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_DebeRetornarTodasLasCategorias()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "SERVIDORES", Description = "Servidores físicos" },
                new Category { Id = 2, Name = "CLOUD", Description = "Servicios cloud" }
            };

            _mockCategoryRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "SERVIDORES");
            Assert.Contains(result, c => c.Name == "CLOUD");
        }

        [Fact]
        public async Task CreateCategoryAsync_DebeCrearCategoria_CuandoDatosValidos()
        {
            // Arrange
            var createDto = new CreateCategoryDto
            {
                Name = "NUEVA_CATEGORIA",
                Description = "Categoría de prueba"
            };

            _mockCategoryRepository
                .Setup(r => r.GetByNameAsync("NUEVA_CATEGORIA"))
                .ReturnsAsync((Category?)null); // No existe

            var createdCategory = new Category
            {
                Id = 3,
                Name = createDto.Name,
                Description = createDto.Description
            };

            _mockCategoryRepository
                .Setup(r => r.AddAsync(It.IsAny<Category>()))
                .ReturnsAsync(createdCategory);

            // Act
            var result = await _categoryService.CreateCategoryAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NUEVA_CATEGORIA", result.Name);
            Assert.Equal("Categoría de prueba", result.Description);

            _mockCategoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
            _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateCategoryAsync_DebeLanzarExcepcion_CuandoNombreDuplicado()
        {
            // Arrange
            var existingCategory = new Category { Id = 1, Name = "SERVIDORES" };
            var createDto = new CreateCategoryDto { Name = "SERVIDORES" };

            _mockCategoryRepository
                .Setup(r => r.GetByNameAsync("SERVIDORES"))
                .ReturnsAsync(existingCategory); // Ya existe

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _categoryService.CreateCategoryAsync(createDto));

            Assert.Equal("Ya existe una categoría con ese nombre", exception.Message);
        }
    }
}
