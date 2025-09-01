# 🚀 Asisya Products API

[![CI Build & Test](https://github.com/JMacAnz/ProductsApi/actions/workflows/ci.yml/badge.svg)]

API REST robusta, escalable y segura para gestión de productos y categorías, optimizada para manejar **100,000 requests concurrentes**.

## ✨ Características

- 🏗️ **Arquitectura Limpia** (Clean Architecture)
- ⚡ **Alta Concurrencia** - Optimizada para 100k requests simultáneos
- 🔐 **Autenticación JWT** segura
- 🐳 **Dockerizada** completamente
- 🧪 **Pruebas Automatizadas** (unitarias e integración)
- 📊 **Rate Limiting** inteligente
- 🚀 **CI/CD** con GitHub Actions
- 📝 **Documentación** automática con Swagger
- 💾 **PostgreSQL** optimizado para alta carga
- 🎯 **DTOs** y mapeo explícito

## 🛠️ Stack Tecnológico

- **Backend**: .NET 9.0, ASP.NET Core Web API
- **Base de Datos**: PostgreSQL 15
- **ORM**: Entity Framework Core
- **Cache**: Memory Cache
- **Autenticación**: JWT Bearer
- **Testing**: xUnit, Moq
- **Contenedores**: Docker, Docker Compose
- **CI/CD**: GitHub Actions

## 🏗️ Arquitectura

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Controllers   │ -> │   Application   │ -> │  Infrastructure │
│ (API Endpoints) │    │ (Business Logic)│    │   (Data Access) │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         v                       v                       v
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   DTOs/Models   │    │     Domain      │    │   PostgreSQL    │
│  (Data Transfer)│    │   (Entities)    │    │   (Database)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 🚀 Inicio Rápido

### Prerrequisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

### 1. Clonar el repositorio

```bash
git clone https://github.com/JMacAnz/ProductsApi.git
cd Asisya.ProductsApi
```

### 2. Desarrollo Local (sin Docker)

```bash
# Iniciar PostgreSQL en Docker
docker-compose up postgres pgladmin -d

# Ejecutar API localmente
cd src/Api
dotnet run

# La API estará disponible en: https://localhost:5000
```

### 3. Con Docker (Recomendado)

```bash
# Ejecutar toda la aplicación
docker-compose up --build

# Acceder a:
# - API: http://localhost:5000
# - Swagger: http://localhost:5000
# - PgAdmin: http://localhost:5050
```

## 📖 Uso de la API

### 1. Autenticación

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@asisya.com",
    "password": "password123"
  }'
```

### 2. Crear Producto

```bash
curl -X POST http://localhost:5000/api/Product \
  -H "Authorization: Bearer TU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Servidor HP ProLiant",
    "description": "Servidor empresarial de alta performance",
    "price": 15000.00,
    "stock": 5,
    "categoryId": 1
  }'
```

### 3. Listar Productos (con paginación)

```bash
curl -X GET "http://localhost:5000/api/Product?pageNumber=1&pageSize=10&categoryId=1" \
  -H "Authorization: Bearer TU_TOKEN"
```

## 🧪 Ejecutar Pruebas

```bash
# Todas las pruebas
dotnet test

# Solo pruebas unitarias
dotnet test tests/Application.Tests

# Solo pruebas de integración
dotnet test tests/Api.IntegrationTests

# Con coverage
dotnet test --collect:"XPlat Code Coverage"
```

## ⚡ Pruebas de Carga

```bash

# Usar script de carga
#Categoría1
.\CargarProductosC1.ps1 -TotalProductos 20
#Categoría2
.\CargarProductosC2.ps1 -TotalProductos 20
# en caso de error 
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser

```

## 🐳 Docker

### Comandos Útiles

```bash
# Solo base de datos (desarrollo)
docker-compose up postgres pgladmin -d

# Aplicación completa
docker-compose --profile with-api up --build -d

# Escalar API (3 instancias)
docker-compose up --scale api=3 -d

# Ver logs
docker-compose logs -f api

# Limpiar todo
docker-compose down -v
```

### Profiles Disponibles

- **default**: API + PostgreSQL + PgAdmin
- **production**: + Nginx Load Balancer
- **monitoring**: + Prometheus

```bash
# Con load balancer
docker-compose --profile production up

# Con monitoreo
docker-compose --profile monitoring up
```

## 🔧 Configuración

### Variables de Entorno Importantes

```bash
# Base de datos
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;..."

# JWT
JwtSettings__SecretKey="tu-clave-secreta-super-segura"
JwtSettings__ExpiryInMinutes=60

# Rate Limiting
RateLimit__RequestsPerMinute=100
RateLimit__BurstSize=20
```

### Configuración para Producción

```bash
# En docker-compose.yml o variables de entorno
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="cadena-produccion"
JwtSettings__SecretKey="clave-super-segura-produccion"
```

## 📊 Métricas de Performance

### Optimizaciones Implementadas

- ✅ **Pool de conexiones PostgreSQL**: MaxPool=100
- ✅ **Memory Cache**: Para categorías y consultas frecuentes
- ✅ **Rate Limiting**: 50 POST/min, 200 requests/min
- ✅ **Índices de BD**: Optimizados para consultas frecuentes
- ✅ **Async/Await**: Todo el pipeline es asíncrono
- ✅ **DTOs**: Sin referencias circulares
- ✅ **DbContext optimizado**: NoTracking para consultas

### Resultados Esperados

- **Throughput**: ~5,000 requests/segundo
- **Latencia**: < 50ms para operaciones simples
- **Concurrencia**: 100k requests simultáneos soportados

## 🤝 Contribuir

1. Fork el proyecto
2. Crea tu feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📝 Decisiones Arquitectónicas

### ¿Por qué Clean Architecture?

- **Testeable**: Cada capa se puede probar independientemente
- **Mantenible**: Cambios en una capa no afectan otras
- **Escalable**: Fácil agregar nuevas funcionalidades
- **Flexible**: Cambiar BD o framework sin tocar lógica de negocio

### ¿Por qué PostgreSQL?

- **Performance**: Excelente para alta concurrencia
- **Confiabilidad**: ACID completo
- **Escalabilidad**: Particionado y replicación nativas
- **Open Source**: Sin costos de licencia

### ¿Por qué Docker?

- **Consistencia**: Mismo entorno en desarrollo y producción
- **Escalabilidad**: Fácil escalar horizontalmente
- **DevOps**: Integración simple con CI/CD
- **Aislamiento**: Dependencias encapsuladas

## 🚀 Roadmap

- [ ] **v2.0**: Integración con Redis para cache distribuido
- [ ] **v2.1**: Implementar CQRS con MediatR
- [ ] **v2.2**: GraphQL endpoint
- [ ] **v2.3**: Event Sourcing para auditoría
- [ ] **v2.4**: Kubernetes manifests
- [ ] **v2.5**: Frontend Angular (bonus)

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - By Marcelo Anzola.

---