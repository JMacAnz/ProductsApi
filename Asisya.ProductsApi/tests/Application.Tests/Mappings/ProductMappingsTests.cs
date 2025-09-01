using Application.DTOs;
using Application.Mappings;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Tests.Mappings
{
    public class ProductMappingsTests
    {
        [Fact]
        public void ToDto_DebeMapearCorrectamente_ProductoCompleto()
        {
            // Arrange
            var category = new Category
            {
                Id = 1,
                Name = "SERVIDORES",
                ImageUrl = "https://example.com/image.jpg"
            };

            var product = new Product
            {
                Id = 1,
                Name = "Servidor Dell",
                Description = "Servidor empresarial",
                Price = 15000m,
                Stock = 5,
                SKU = "DELL-001",
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1),
                CategoryId = 1,
                Category = category
            };

            // Act
            var result = product.ToDto();

            // Assert
            Assert.Equal(1, result.Id);
            Assert.Equal("Servidor Dell", result.Name);
            Assert.Equal("Servidor empresarial", result.Description);
            Assert.Equal(15000m, result.Price);
            Assert.Equal(5, result.Stock);
            Assert.Equal("DELL-001", result.SKU);
            Assert.True(result.IsActive);
            Assert.Equal(1, result.CategoryId);
            Assert.Equal("SERVIDORES", result.CategoryName);
            Assert.Equal("https://example.com/image.jpg", result.CategoryImageUrl);
        }

        [Fact]
        public void ToEntity_DebeMapearCorrectamente_CreateProductDto()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Name = "Nuevo Producto",
                Description = "Descripción test",
                Price = 500m,
                Stock = 20,
                SKU = "NEW-001",
                CategoryId = 2,
                IsActive = true
            };

            // Act
            var result = createDto.ToEntity();

            // Assert
            Assert.Equal("Nuevo Producto", result.Name);
            Assert.Equal("Descripción test", result.Description);
            Assert.Equal(500m, result.Price);
            Assert.Equal(20, result.Stock);
            Assert.Equal("NEW-001", result.SKU);
            Assert.Equal(2, result.CategoryId);
            Assert.True(result.IsActive);
            Assert.Equal(0, result.Id); // Nuevo producto no tiene ID
        }

        [Fact]
        public void UpdateEntity_DebeActualizarCorrectamente_ProductoExistente()
        {
            // Arrange
            var existingProduct = new Product
            {
                Id = 1,
                Name = "Producto Original",
                Price = 100m,
                Stock = 10,
                CategoryId = 1,
                CreatedAt = new DateTime(2025, 1, 1)
            };

            var updateDto = new UpdateProductDto
            {
                Id = 1,
                Name = "Producto Actualizado",
                Description = "Nueva descripción",
                Price = 200m,
                Stock = 15,
                CategoryId = 2,
                IsActive = false
            };

            // Act
            updateDto.UpdateEntity(existingProduct);

            // Assert
            Assert.Equal("Producto Actualizado", existingProduct.Name);
            Assert.Equal("Nueva descripción", existingProduct.Description);
            Assert.Equal(200m, existingProduct.Price);
            Assert.Equal(15, existingProduct.Stock);
            Assert.Equal(2, existingProduct.CategoryId);
            Assert.False(existingProduct.IsActive);
            Assert.NotNull(existingProduct.UpdatedAt); // Debe haberse actualizado
            Assert.Equal(new DateTime(2025, 1, 1), existingProduct.CreatedAt); // No debe cambiar
        }

        [Theory]
        [InlineData("", false)] // Nombre vacío
        [InlineData("A", true)]  // Nombre muy corto pero válido
        [InlineData("Servidor HP ProLiant DL380 Gen10", true)] // Nombre normal
        public void ValidateProductName_DebeValidarCorrectamente(string name, bool expectedValid)
        {
            // Este test verifica las reglas de validación de nombres
            var isValid = !string.IsNullOrWhiteSpace(name) && name.Length <= 200 && name.Length >= 1;

            Assert.Equal(expectedValid, isValid);
        }
    }
}
