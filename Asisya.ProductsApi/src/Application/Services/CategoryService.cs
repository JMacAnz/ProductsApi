using Application.DTOs;
using Application.Mappings;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Select(c => c.ToDto());
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _categoryRepository.GetWithProductsAsync(id);
            return category?.ToDto();
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto)
        {
            // Validación de negocio
            var existingCategory = await _categoryRepository.GetByNameAsync(categoryDto.Name);
            if (existingCategory != null)
                throw new ArgumentException("Ya existe una categoría con ese nombre");

            var category = new Category
            {
                Name = categoryDto.Name,
                Description = categoryDto.Description,
                ImageUrl = categoryDto.ImageUrl
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            _logger.LogInformation("Categoría {Name} creada con ID {Id}", category.Name, category.Id);

            return category.ToDto();
        }

        public async Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryDto categoryDto)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryDto.Id);
            if (category == null)
                throw new ArgumentException("Categoría no encontrada");

            // Validar nombre único si cambió
            if (category.Name != categoryDto.Name)
            {
                var existingCategory = await _categoryRepository.GetByNameAsync(categoryDto.Name);
                if (existingCategory != null)
                    throw new ArgumentException("Ya existe una categoría con ese nombre");
            }

            category.Name = categoryDto.Name;
            category.Description = categoryDto.Description;
            category.ImageUrl = categoryDto.ImageUrl;

            await _categoryRepository.UpdateAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return category.ToDto();
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _categoryRepository.GetWithProductsAsync(id);
            if (category == null)
                return false;

            // Regla de negocio: No permitir eliminar categorías con productos
            if (category.Products.Any())
                throw new InvalidOperationException("No se puede eliminar una categoría que tiene productos asociados");

            await _categoryRepository.DeleteAsync(category);
            await _categoryRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            var category = await _categoryRepository.GetByNameAsync(name);
            return category != null;
        }
    }
}
