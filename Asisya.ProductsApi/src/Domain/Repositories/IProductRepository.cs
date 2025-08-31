using Domain.Common;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        // Métodos específicos para productos
        Task<Product?> GetByIdWithCategoryAsync(int id);
        Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsWithFiltersAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isActive = null);
        Task<bool> ExistsBySKUAsync(string sku);
        Task BulkInsertAsync(IEnumerable<Product> products); // Para los 100k productos
    }
}
