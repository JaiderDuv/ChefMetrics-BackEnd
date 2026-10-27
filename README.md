# ChefMetrics Backend

API y contratos compartidos de ChefMetrics. El frontend Blazor está en `../PWGAPG-Frontend`.

## Requisitos

- .NET SDK 9
- SQL Server con la base `ChefMetrics`
- `dotnet-ef` para migraciones

## Comandos útiles

```powershell
# Compilar
dotnet build ChefMetrics-BackEnd.csproj

# Pruebas
dotnet test ChefMetrics-BackEnd.Tests\ChefMetrics-BackEnd.Tests.csproj

# Aplicar migraciones (revisar la cadena de conexión antes)
dotnet ef database update

# Crear una migración
dotnet ef migrations add NombreDeLaMigracion
```

La cadena de conexión se configura en `appsettings.json`.

## Cambios recientes

- Conversión de unidades ampliada y centralizada en `ChefMetrics.Contracts/Costeo/ConversorUnidades.cs`.
- Densidad opcional por insumo (`DensidadGPorMl`) para convertir entre masa y volumen.
- Onboarding reducido a dos pasos y con persistencia de datos personales/negocio.
- Categorías de negocio `Panadería` y `Pastelería` disponibles en el frontend.
- Pruebas de conversión, merma, escalado, costeo de producto, evaluación de menú y precio sugerido.

## Documentación

- [Matriz de trazabilidad](docs/trazabilidad.md)

## Nota sobre archivos no rastreados

No agregar a los commits `wwwroot/uploads/recetas/e1e50b5566ef42869bb3dc3431314c44.png`.
