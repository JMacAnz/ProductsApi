using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class CreateProductRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [MaxLength(50)]
        public string? SKU { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateProductRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class BulkCreateProductsRequest
    {
        [Required]
        [Range(1, 100000, ErrorMessage = "La cantidad debe estar entre 1 y 100,000")]
        public int Count { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public string? BaseProductName { get; set; } = "Producto";

        [Range(1, 10000)]
        public double MinPrice { get; set; } = 10.0;

        [Range(1, 10000)]
        public double MaxPrice { get; set; } = 1000.0;
    }

    public class BulkCreateResponse
    {
        public int CreatedCount { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
