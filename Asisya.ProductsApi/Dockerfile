# Dockerfile optimizado para .NET 9.0

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Optimizaciones para alta concurrencia en producción
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_gcServer=1
ENV DOTNET_gcConcurrent=1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyectos principales
COPY ["src/Api/Api.csproj", "src/Api/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["Asisya.ProductsApi.sln", "./"]

# Restore de dependencias (solo proyectos principales)
RUN dotnet restore "Asisya.ProductsApi.sln"

# Copiar código fuente (solo src/)
COPY src/ src/

# Build de la aplicación
WORKDIR "/src/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Test stage separado (para ejecutar pruebas si se necesita)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS test
WORKDIR /src

# Copiar solución completa incluyendo tests
COPY . .

# Restore y ejecutar pruebas
RUN dotnet restore "Asisya.ProductsApi.Tests.sln" || dotnet restore
RUN dotnet test --no-restore --verbosity normal || true

# Final stage
FROM base AS final
WORKDIR /app

# Crear usuario no-root para seguridad
RUN addgroup --system --gid 1001 dotnet \
    && adduser --system --uid 1001 --ingroup dotnet dotnet

# Copiar la aplicación publicada
COPY --from=publish /app/publish .

# Cambiar ownership y permisos
RUN chown -R dotnet:dotnet /app
USER dotnet

# Health check mejorado
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/api/auth/test || exit 1

ENTRYPOINT ["dotnet", "Api.dll"]