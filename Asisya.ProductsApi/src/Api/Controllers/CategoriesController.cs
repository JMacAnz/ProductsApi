using Api.Models;
using Application.DTOs;
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
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ICategoryService categoryService,
            ICategoryRepository categoryRepository,
            ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryDto>>>> GetCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                return Ok(new ApiResponse<IEnumerable<CategoryDto>>
                {
                    Success = true,
                    Message = "Categorías obtenidas exitosamente",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías");
                return StatusCode(500, new ApiResponse<IEnumerable<CategoryDto>>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Category>>> GetCategory(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new ApiResponse<Category>
                    {
                        Success = false,
                        Message = "Categoría no encontrada"
                    });
                }

                return Ok(new ApiResponse<Category>
                {
                    Success = true,
                    Message = "Categoría obtenida exitosamente",
                    Data = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría {Id}", id);
                return StatusCode(500, new ApiResponse<Category>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Category>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                // Verificar si ya existe una categoría con ese nombre
                var existingCategory = await _categoryRepository.GetByNameAsync(request.Name);
                if (existingCategory != null)
                {
                    return BadRequest(new ApiResponse<Category>
                    {
                        Success = false,
                        Message = "Ya existe una categoría con ese nombre"
                    });
                }

                var category = new Category
                {
                    Name = request.Name,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl
                };

                await _categoryRepository.AddAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetCategory),
                    new { id = category.Id },
                    new ApiResponse<Category>
                    {
                        Success = true,
                        Message = "Categoría creada exitosamente",
                        Data = category
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");
                return StatusCode(500, new ApiResponse<Category>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }
    }
}
