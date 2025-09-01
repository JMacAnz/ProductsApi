# ProductsApi

API CRUD para Productos

---

## Guía de comandos Docker para Asisya Products API

### Desarrollo local (solo base de datos)

```bash
# Iniciar solo PostgreSQL y PgAdmin para desarrollo local
docker-compose up postgres pgadmin -d

# Ver logs
docker-compose logs -f postgres

# Parar servicios
docker-compose down

#Producción completa (con API en Docker)
# Build y ejecutar toda la aplicación
docker-compose --profile with-api up --build -d

# Ejecutar en segundo plano
docker-compose up --build -d

# Ver logs de todos los servicios
docker-compose logs -f

# Ver logs solo de la API
docker-compose logs -f api

# Escalar la API (múltiples instancias)
docker-compose up --scale api=3 -d

#Producción con Load Balancer
#Ejecutar con Nginx y múltiples instancias de API
docker-compose --profile production up --scale api=3 -d

# Acceder a la API a través de Nginx
# http://localhost/api/auth/login

#Monitoreo avanzado
# Ejecutar con Prometheus para métricas
docker-compose --profile monitoring up -d

# Accesos:
# API: http://localhost:5000
# PgAdmin: http://localhost:5050  
# Prometheus: http://localhost:9090

#Comandos de mantenimiento
# Ver estado de todos los contenedores
docker-compose ps

# Ver uso de recursos
docker stats

# Limpiar volúmenes (¡CUIDADO! Elimina datos)
docker-compose down -v

# Rebuild forzado (sin cache)
docker-compose build --no-cache api

# Ver logs con timestamps
docker-compose logs -f -t api

# Ejecutar comando dentro del contenedor de API
docker-compose exec api bash

# Ejecutar comando dentro de PostgreSQL
docker-compose exec postgres psql -U postgres -d AsisyaProductsDb

#Diagnóstico de problemas
# Verificar que todos los servicios estén "healthy"
docker-compose ps

# Ver logs de errores
docker-compose logs api | grep -i error

# Verificar conectividad entre contenedores
docker-compose exec api ping postgres

# Verificar configuración de la BD
docker-compose exec postgres psql -U postgres -c "SHOW max_connections;"

# Reiniciar un servicio específico
docker-compose restart api

#Testing en Docker
# Ejecutar pruebas durante el build
docker build --target test -t asisya-api-test .

# Ejecutar pruebas en contenedor separado
docker run --rm asisya-api-test dotnet test --logger:console

# Build que incluye pruebas automáticamente
docker-compose build api  # Las pruebas se ejecutan automáticamente

#Variables de entorno importantes
#Para desarrollo:
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=AsisyaProductsDb;Username=postgres;Password=postgres123

#Para producción:
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=AsisyaProductsDb;Username=postgres;Password=postgres123
JwtSettings__SecretKey=clave-super-segura-para-produccion

#Verificación de funcionamiento
Después de docker-compose up:

Health checks: Todos los servicios deben mostrar "(healthy)"

API: http://localhost:5000/api/auth/test
 debe responder

Swagger: http://localhost:5000/
 debe cargar

PgAdmin: http://localhost:5050
 debe cargar

Base de datos: Debe tener las tablas Categories y Products

#Solución a problemas comunes
#Error de conexión a PostgreSQL:
# Verificar que PostgreSQL esté healthy
docker-compose ps

# Ver logs de PostgreSQL
docker-compose logs postgres

# Reiniciar PostgreSQL
docker-compose restart postgres

#API no inicia:
# Ver logs detallados
docker-compose logs api

# Verificar que las migraciones se apliquen
docker-compose exec api dotnet ef database update

#Performance lenta:
# Verificar uso de recursos
docker stats

# Verificar configuración de pool de conexiones
docker-compose exec postgres psql -U postgres -c "SHOW max_connections;"
