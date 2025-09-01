using Domain.Repositories;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // DbContextPool -> Mejor manejo de conexiones en alta concurrencia
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30); // timeout de 30s
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Optimizaciones de performance
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        }, poolSize: 128); // tamaño del pool ajustable

        // Cache distribuido (puedes empezar con memoria local)
        services.AddDistributedMemoryCache();

        // 👉 Si quieres Redis en lugar de memoria, cambia esto por:
        // services.AddStackExchangeRedisCache(options =>
        // {
        //     options.Configuration = configuration.GetConnectionString("RedisConnection");
        //     options.InstanceName = "AsisyaCache_";
        // });

        // Repositorios
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        return services;
    }
}
