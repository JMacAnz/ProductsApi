using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductFiltersDto filters);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductDto productDto);
        Task<ProductDto> UpdateProductAsync(UpdateProductDto productDto);
        Task<bool> DeleteProductAsync(int id);
        Task<BulkCreateResultDto> CreateBulkProductsAsync(BulkCreateRequestDto request);
        Task<bool> ExistsBySKUAsync(string sku);
    }

    public class BulkCreateRequestDto
    {
        public int Count { get; set; }
        public int CategoryId { get; set; }
        public string BaseProductName { get; set; } = "Producto";
        public decimal MinPrice { get; set; } = 10.0m;
        public decimal MaxPrice { get; set; } = 1000.0m;
        public int BatchSize { get; set; } = 1000; // Para procesamiento en lotes
    }

    public class BulkCreateResultDto
    {
        public int CreatedCount { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan ProcessingTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
