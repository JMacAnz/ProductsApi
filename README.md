# ProductsApi

Api CRUD Productos



\# Guía de comandos Docker para Asisya Products API



\## Desarrollo local (solo base de datos)

```bash

\# Iniciar solo PostgreSQL y PgAdmin para desarrollo local

docker-compose up postgres pgladmin -d



\# Ver logs

docker-compose logs -f postgres



\# Parar servicios

docker-compose down

```



\## Producción completa (con API en Docker)

```bash

\# Build y ejecutar toda la aplicación

docker-compose up --build



\# Ejecutar en segundo plano

docker-compose up --build -d



\# Ver logs de todos los servicios

docker-compose logs -f



\# Ver logs solo de la API

docker-compose logs -f api



\# Escalar la API (múltiples instancias)

docker-compose up --scale api=3 -d

```



\## Producción con Load Balancer

```bash

\# Ejecutar con Nginx y múltiples instancias de API

docker-compose --profile production up --scale api=3 -d



\# Acceder a la API a través de Nginx

\# http://localhost/api/auth/login

```



\## Monitoreo avanzado

```bash

\# Ejecutar con Prometheus para métricas

docker-compose --profile monitoring up -d



\# Accesos:

\# API: http://localhost:5000

\# PgAdmin: http://localhost:5050  

\# Prometheus: http://localhost:9090

```



\## Comandos de mantenimiento

```bash

\# Ver estado de todos los contenedores

docker-compose ps



\# Ver uso de recursos

docker stats



\# Limpiar volúmenes (¡CUIDADO! Elimina datos)

docker-compose down -v



\# Rebuild forzado (sin cache)

docker-compose build --no-cache api



\# Ver logs con timestamps

docker-compose logs -f -t api



\# Ejecutar comando dentro del contenedor de API

docker-compose exec api bash



\# Ejecutar comando dentro de PostgreSQL

docker-compose exec postgres psql -U postgres -d AsisyaProductsDb

```



\## Diagnóstico de problemas

```bash

\# Verificar que todos los servicios estén "healthy"

docker-compose ps



\# Ver logs de errores

docker-compose logs api | grep -i error



\# Verificar conectividad entre contenedores

docker-compose exec api ping postgres



\# Verificar configuración de la BD

docker-compose exec postgres psql -U postgres -c "SHOW max\_connections;"



\# Reiniciar un servicio específico

docker-compose restart api

```



\## Testing en Docker

```bash

\# Ejecutar pruebas durante el build

docker build --target test -t asisya-api-test .



\# Ejecutar pruebas en contenedor separado

docker run --rm asisya-api-test dotnet test --logger:console



\# Build que incluye pruebas automáticamente

docker-compose build api  # Las pruebas se ejecutan automáticamente

```



\## Variables de entorno importantes



\### Para desarrollo:

```bash

ASPNETCORE\_ENVIRONMENT=Development

ConnectionStrings\_\_DefaultConnection=Host=localhost;Port=5432;...

```



\### Para producción:

```bash

ASPNETCORE\_ENVIRONMENT=Production

ConnectionStrings\_\_DefaultConnection=Host=postgres;Port=5432;...

JwtSettings\_\_SecretKey=clave-super-segura-para-produccion

```



\## Verificación de funcionamiento



Después de `docker-compose up`:



1\. \*\*Health checks\*\*: Todos los servicios deben mostrar "(healthy)"

2\. \*\*API\*\*: http://localhost:5000/api/auth/test debe responder

3\. \*\*Swagger\*\*: http://localhost:5000/ debe cargar

4\. \*\*PgAdmin\*\*: http://localhost:5050 debe cargar

5\. \*\*Base de datos\*\*: Debe tener las tablas Categories y Products



\## Solución a problemas comunes



\### Error de conexión a PostgreSQL:

```bash

\# Verificar que PostgreSQL esté healthy

docker-compose ps



\# Ver logs de PostgreSQL

docker-compose logs postgres



\# Reiniciar PostgreSQL

docker-compose restart postgres

```



\### API no inicia:

```bash

\# Ver logs detallados

docker-compose logs api



\# Verificar que las migraciones se apliquen

docker-compose exec api dotnet ef database update

```



\### Performance lenta:

```bash

\# Verificar uso de recursos

docker stats



\# Verificar configuración de pool de conexiones

docker-compose exec postgres psql -U postgres -c "SHOW max\_connections;"

```

