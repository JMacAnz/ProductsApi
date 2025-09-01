// src/Api/Program.cs
using Api.Services;
using Application;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

#region Kestrel config (alta concurrencia)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
    options.ListenAnyIP(8080); // Puerto interno 8080
});
#endregion

#region Controllers & JSON
builder.Services.AddControllers(options =>
{
    // Control de colecciones grandes
    options.MaxModelBindingCollectionSize = 100000;
})
.AddJsonOptions(options =>
{
    // Evitar referencias circulares
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

// Configuración adicional para performance
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.WriteIndented = false; // Menos overhead
});
#endregion

#region Capas Application & Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
#endregion

#region Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    //// Límite para creación de productos
    //options.AddFixedWindowLimiter("CreateProductPolicy", opt =>
    //{
    //    opt.PermitLimit = 20_000; // requests por minuto
    //    opt.Window = TimeSpan.FromSeconds(30);
    //    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    //    opt.QueueLimit = 50_000; // cuantos pueden esperar en cola
    //});

    //// Límite global
    //options.AddFixedWindowLimiter("GlobalPolicy", opt =>
    //{
    //    opt.PermitLimit = 50_000; // máximo global por minuto
    //    opt.Window = TimeSpan.FromSeconds(30);
    //    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    //    opt.QueueLimit = 50_000; // máximo en cola
    //});
    // Límite por IP para creación de productos
    options.AddPolicy("CreateProductPolicy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20_000,           // max requests por ventana
            Window = TimeSpan.FromSeconds(30),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 50_000             // max requests en cola
        });
    });

    // Límite global, pero aún partitioned por IP
    options.AddPolicy("GlobalPolicy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 50_000,
            Window = TimeSpan.FromSeconds(30),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 50_000
        });
    });

});
#endregion

#region Cache en memoria
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000;
});
#endregion

#region Autenticación JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
#endregion

#region Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Asisya Products API",
        Version = "v1",
        Description = "API optimizada para gestión de productos con alta concurrencia"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
#endregion

#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
#endregion

var app = builder.Build();

#region Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Asisya Products API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
#endregion

#region Migraciones automáticas en dev
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.Migrate();
        Console.WriteLine("✅ Base de datos migrada correctamente");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al migrar la base de datos: {ex.Message}");
    }
}
#endregion

Console.WriteLine("🚀 Asisya Products API iniciada");
Console.WriteLine("📖 Swagger: /");

app.Run();
public partial class Program { }
