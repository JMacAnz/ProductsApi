-- Este archivo se ejecuta automáticamente al crear el contenedor PostgreSQL

-- Crear base de datos de desarrollo
CREATE DATABASE "AsisyaProductsDb_Dev";

-- Configurar permisos
GRANT ALL PRIVILEGES ON DATABASE "AsisyaProductsDb" TO postgres;
GRANT ALL PRIVILEGES ON DATABASE "AsisyaProductsDb_Dev" TO postgres;

-- Configuraciones de rendimiento específicas para la base de datos
ALTER SYSTEM SET max_connections = 200;
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET effective_cache_size = '1GB';
ALTER SYSTEM SET maintenance_work_mem = '64MB';
ALTER SYSTEM SET checkpoint_completion_target = 0.9;
ALTER SYSTEM SET wal_buffers = '16MB';
ALTER SYSTEM SET default_statistics_target = 100;
ALTER SYSTEM SET random_page_cost = 1.1;
ALTER SYSTEM SET effective_io_concurrency = 200;
ALTER SYSTEM SET work_mem = '4MB';

-- Recargar configuración
SELECT pg_reload_conf();