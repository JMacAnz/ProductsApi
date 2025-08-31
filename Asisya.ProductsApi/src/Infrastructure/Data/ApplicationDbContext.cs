using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSet = equivalente a una tabla en SQL Server
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la entidad Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(255);
                entity.HasIndex(e => e.Name).IsUnique(); // Índice único en Name

                // Una Category puede tener muchos Products
                entity.HasMany(c => c.Products)
                      .WithOne(p => p.Category)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict); // No permitir borrar categoría con productos
            });

            // Configuración de la entidad Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Stock).IsRequired();
                entity.Property(e => e.SKU).HasMaxLength(50);
                entity.HasIndex(e => e.SKU).IsUnique(); // SKU único
                entity.HasIndex(e => e.Name); // Índice para búsquedas rápidas
                entity.HasIndex(e => new { e.CategoryId, e.IsActive }); // Índice compuesto
            });

            // Datos semilla (seed data) - datos iniciales
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Crear categorías iniciales con fechas fijas
            var fixedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var categories = new[]
            {
                new Category
                {
                    Id = 1,
                    Name = "SERVIDORES",
                    Description = "Servidores físicos y virtuales",
                    ImageUrl = "https://example.com/servidores.jpg",
                    CreatedAt = fixedDate
                },
                new Category
                {
                    Id = 2,
                    Name = "CLOUD",
                    Description = "Servicios en la nube",
                    ImageUrl = "https://example.com/cloud.jpg",
                    CreatedAt = fixedDate
                }
            };

            modelBuilder.Entity<Category>().HasData(categories);
        }
    }
}
